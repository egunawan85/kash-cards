#!/usr/bin/env bash
# deploy/deploy.sh -- operator-side modular deploy for kash-cards.
#
# Runs from your devbox (the control machine that has `az` auth and the filled,
# gitignored deploy/config + deploy/secrets) -- NOT on the target VM. The fast
# lane that sits next to provision-and-bootstrap.sh: that orchestrator is the
# full ~30-45 min "build the world" pipeline (provision infra + Key Vault +
# bootstrap + schema + all 12 sites + Cloudflare); this script is for the common
# case AFTER the box exists -- redeploy app code, all tiers or just one, without
# re-provisioning, without re-running the perimeter, and (by default) without
# touching the DB schema.
#
# The VM is NSG-dark (no inbound), so -- exactly like the orchestrator -- every
# on-box step runs via 'az vm run-command invoke'. This script just sequences the
# same VM-side scripts with per-tier targeting (-Service) layered on.
#
# Modelled on the sibling runegate deploy-iis.ps1 command surface
# (update/build/restart/start/stop/status/logs), re-shaped for kash's remote
# (NSG-dark) execution model: there the manager runs ON the box and `git pull`s;
# here the devbox drives it over run-command and source arrives as a zipball.
#
# Usage:
#   ENV=dev ./deploy/deploy.sh update [svc] [--with-schema]
#   ENV=dev ./deploy/deploy.sh build  [svc]
#   ENV=dev ./deploy/deploy.sh sync   <svc> [files...] [--recycle]
#   ENV=dev ./deploy/deploy.sh restart [svc]
#   ENV=dev ./deploy/deploy.sh start  [svc]
#   ENV=dev ./deploy/deploy.sh stop   [svc]
#   ENV=dev ./deploy/deploy.sh status
#   ENV=dev ./deploy/deploy.sh logs   [svc]
#   ENV=dev ./deploy/deploy.sh schema           # re-publish the dacpac only
#
# Commands:
#   update [svc]   App-only redeploy: fetch main -> rewrite config -> build+publish
#                  -> inject secrets -> recycle -> start-guarantee. All tiers, or
#                  one (svc). Add --with-schema to also re-publish the dacpac
#                  BEFORE the app deploy (use when a change needs a migration; the
#                  dacpac publish is an incremental diff, so it is safe to run).
#   build [svc]    Build + publish from the source already ON the box (no fetch,
#                  no secret inject). Fast iterate; assumes a prior `update`
#                  populated config + secrets on the box.
#   sync <svc>     Front-end fast lane: copy CHANGED static assets + ASP.NET
#                  markup (.aspx/.ascx/.master/.css/.js/images...) from your LOCAL
#                  working tree straight into the live site root on the box. No
#                  GitHub push, no fetch, no NuGet restore, no MSBuild, no secret
#                  inject -- seconds, not minutes. Changed files are auto-detected
#                  (git diff vs origin/<branch> + untracked) under the tier's
#                  project, or list them explicitly. REFUSES to sync anything that
#                  needs a compile (.cs/.csproj/.resx/packages.config) or patched
#                  config (*.config) -- use `build`/`update` for those. Add
#                  --recycle to also recycle the pool (rarely needed).
#   restart [svc]  Recycle running pool(s); start any that are stopped (never
#                  no-ops a down tier). re-reads the existing per-pool env.
#   start [svc]    Start pool(s) and VERIFY each reaches Started (the guarantee).
#   stop [svc]     Stop pool(s) + site(s).
#   status         Show pool/site state + port for every tier.
#   logs [svc]     Tail the latest IIS log per tier.
#   schema         Re-publish the schema-only dacpac (incremental). Opt-in; not
#                  part of the default `update`.
#
# svc (service alias) = the app pool minus the 'kash-' prefix. Valid aliases:
#   api  api-public  api-admin  api-callback  api-scheduler  apidocs
#   dashboard  dashboard-admin  int  int-callback  int-scheduler  scrapper
# (The on-box scripts validate the alias and list the valid set on a miss.)
set -euo pipefail

# Windows/git-bash: stop MSYS from rewriting '/'-leading args (Azure resource IDs).
export MSYS_NO_PATHCONV=1

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
DEPLOY_SCRIPTS="$SCRIPT_DIR/scripts/deploy"
REPO_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"   # repo root (deploy/ lives one level down)
SITES_JSON="$SCRIPT_DIR/config/sites.json"
ENV="${ENV:-dev}"
CFG="$SCRIPT_DIR/config/.env.provision.${ENV}"

# Max base64 payload for `sync` (run-command caps the inline request). A handful
# of edited static/markup files gzip to a few KB; this guards against someone
# syncing a 100 MB Content/ tree. Override with SYNC_MAX_B64 if ever needed.
SYNC_MAX_B64="${SYNC_MAX_B64:-200000}"   # ~200 KB of base64

log()   { printf '\n=== %s ===\n' "$*"; }
die()   { printf '[xx] %s\n' "$*" >&2; exit 1; }
usage() {
  cat >&2 <<'EOF'
Usage: ENV=dev ./deploy/deploy.sh <command> [svc] [--with-schema]

  update [svc]   App-only redeploy: fetch main -> rewrite config -> build+publish
                 -> inject secrets -> recycle -> start-guarantee. All tiers or one.
                 Add --with-schema to re-publish the dacpac first (for migrations).
  build [svc]    Build + publish from the source already on the box (no fetch, no
                 secret inject). Fast iterate; assumes a prior `update`.
  sync <svc>     Front-end fast lane: copy CHANGED static assets + ASP.NET markup
                 from your LOCAL tree into the live site root -- no push, no build,
                 seconds not minutes. Auto-detects changed files (or list them).
                 Refuses compile-triggers (.cs/.csproj/.config). --recycle optional.
  restart [svc]  Recycle running pool(s); start any that are stopped.
  start [svc]    Start pool(s) and verify each reaches Started (the guarantee).
  stop [svc]     Stop pool(s) + site(s).
  status         Show pool/site state + port for every tier.
  logs [svc]     Tail the latest IIS log per tier.
  schema         Re-publish the schema-only dacpac (incremental; opt-in).

  svc (service alias) = app pool minus the 'kash-' prefix:
    api  api-public  api-admin  api-callback  api-scheduler  apidocs
    dashboard  dashboard-admin  int  int-callback  int-scheduler  scrapper
EOF
  exit "${1:-2}"
}

[[ -f "$CFG" ]] || die "missing $CFG (copy config/.env.provision.${ENV}.example and fill it)"
# shellcheck disable=SC1090
. "$CFG"
: "${VM_NAME:?set in $CFG}" : "${COMPUTE_RG:?set in $CFG}" : "${KEYVAULT_NAME:?set in $CFG}"
REPO_BRANCH="${REPO_BRANCH:-main}"

# Windows/git-bash: az needs a Windows path for @file args (cygpath). Passthrough on Linux.
winpath() { if command -v cygpath >/dev/null 2>&1; then cygpath -w "$1"; else printf '%s' "$1"; fi; }

# Run a PowerShell script ON the VM and stream its output. `az vm run-command
# invoke` exits 0 even when the inner script fails, so we scan the captured
# output for the fatal marker ([xx], emitted by every VM-side Stop-*/die) and
# fail this wrapper if we see it -- otherwise a broken phase cascades silently.
run_on_vm() {
  local script="$1"; shift
  local params="${1:-}"
  log "VM: $(basename "$script") ${params}"
  local out
  out=$(az vm run-command invoke -g "$COMPUTE_RG" -n "$VM_NAME" \
     --command-id RunPowerShellScript --scripts "@$(winpath "$script")" ${params:+--parameters $params} \
     --query "value[0].message" -o tsv)
  printf '%s\n' "$out"
  if printf '%s' "$out" | grep -q '\[xx\]'; then
    die "VM phase failed: $(basename "$script") -- see [xx] above"
  fi
}

# Push the gitignored infra config onto the box. vm-fetch-source REPLACES the
# whole source tree on each run, which wipes the previously-written config, so
# this MUST run after every fetch (the orchestrator does the same).
push_config() {
  local cfg_b64; cfg_b64=$(base64 -w0 "$CFG")
  run_on_vm "$DEPLOY_SCRIPTS/vm-write-config.ps1" "ConfigB64=$cfg_b64 Env=$ENV"
}

# -- sync helpers ------------------------------------------------------------
# Resolve a tier selector (alias / pool / project) to its project name. Each site
# in sites.json is a single JSON line, so a line-grep + sed extract is enough --
# avoids a jq dependency on the devbox (matches the no-jq style of this script).
resolve_project() {
  local tier="$1" line
  [[ -f "$SITES_JSON" ]] || die "missing $SITES_JSON"
  line=$(grep -E "\"appPool\"[[:space:]]*:[[:space:]]*\"kash-${tier}\"" "$SITES_JSON" | head -1 || true)
  [[ -z "$line" ]] && line=$(grep -E "\"appPool\"[[:space:]]*:[[:space:]]*\"${tier}\"" "$SITES_JSON" | head -1 || true)
  [[ -z "$line" ]] && line=$(grep -E "\"project\"[[:space:]]*:[[:space:]]*\"${tier}\"" "$SITES_JSON" | head -1 || true)
  [[ -z "$line" ]] && return 1
  printf '%s' "$line" | sed -n 's/.*"project"[[:space:]]*:[[:space:]]*"\([^"]*\)".*/\1/p'
}

valid_aliases() {
  printf 'valid tiers: '
  grep -oE '"appPool"[[:space:]]*:[[:space:]]*"kash-[^"]+"' "$SITES_JSON" \
    | sed -E 's/.*"kash-([^"]+)"/\1/' | sort | paste -sd' ' - || true
}

# Run git against the repo root. We can't use `git -C "$REPO_ROOT"`: this script
# exports MSYS_NO_PATHCONV=1 (so az gets un-rewritten resource IDs), which also
# stops MSYS from converting the '/c/...'-form REPO_ROOT into the 'C:\...' that
# native git.exe needs -- `git -C /c/...` then fails with "cannot change to".
# `cd` is a shell builtin and handles the MSYS path fine, so subshell-cd instead.
git_repo() { ( cd "$REPO_ROOT" && git "$@" ); }

# Base ref for `sync` change detection: prefer the remote tracking branch (so
# committed-but-unpushed edits show too), else fall back to local HEAD.
sync_base_ref() {
  if git_repo rev-parse --verify --quiet "origin/${REPO_BRANCH}" >/dev/null 2>&1; then
    printf 'origin/%s' "$REPO_BRANCH"
  else
    printf 'HEAD'
  fi
}

# -- Parse args --------------------------------------------------------------
CMD="${1:-}"; [[ -n "$CMD" ]] || usage 2
shift || true

SVC=""
WITH_SCHEMA=0
RECYCLE=0
SYNC_FILES=()
for arg in "$@"; do
  case "$arg" in
    --with-schema) WITH_SCHEMA=1 ;;
    --recycle)     RECYCLE=1 ;;
    -h|--help)     usage 0 ;;
    -*)            die "unknown option: $arg" ;;
    *)
      if [[ -z "$SVC" ]]; then
        SVC="$arg"               # first positional is always the tier
      elif [[ "$CMD" == "sync" ]]; then
        SYNC_FILES+=("$arg")     # sync alone takes an explicit file list after the tier
      else
        die "unexpected extra argument: $arg"
      fi
      ;;
  esac
done

[[ "$WITH_SCHEMA" -eq 1 && "$CMD" != "update" ]] && die "--with-schema is only valid with 'update'"
[[ "$RECYCLE" -eq 1 && "$CMD" != "sync" ]] && die "--recycle is only valid with 'sync'"

# -Service param fragment for the run-command parameter list (empty when no svc).
svc_param() { [[ -n "$SVC" ]] && printf 'Service=%s' "$SVC"; }

case "$CMD" in

  update)
    # App-only redeploy (optionally schema-first). For a private repo the
    # zipball needs REPO-TOKEN in Key Vault -- vm-fetch-source pulls it via KvName.
    : "${REPO_URL:?set in $CFG}"
    log "modular update${SVC:+ (tier: $SVC)}${WITH_SCHEMA:+ +schema}"
    run_on_vm "$DEPLOY_SCRIPTS/vm-fetch-source.ps1" "RepoUrl=$REPO_URL Branch=$REPO_BRANCH KvName=$KEYVAULT_NAME"
    push_config
    if [[ "$WITH_SCHEMA" -eq 1 ]]; then
      run_on_vm "$DEPLOY_SCRIPTS/vm-publish-schema.ps1" "Env=$ENV"
    fi
    run_on_vm "$DEPLOY_SCRIPTS/deploy-iis.ps1"     "$(svc_param)"
    run_on_vm "$DEPLOY_SCRIPTS/inject-secrets.ps1" "$(svc_param)"
    run_on_vm "$DEPLOY_SCRIPTS/vm-iis-ops.ps1"     "Action=start $(svc_param)"
    log "update complete${SVC:+ (tier: $SVC)}"
    ;;

  build)
    # Build + publish from the source already on the box. No fetch, no secrets.
    run_on_vm "$DEPLOY_SCRIPTS/deploy-iis.ps1" "$(svc_param)"
    ;;

  sync)
    # Front-end fast lane: ship CHANGED static/markup files from the LOCAL tree
    # straight into the live site root -- no GitHub push, no fetch, no restore,
    # no MSBuild, no secret inject. Seconds, not minutes.
    [[ -n "$SVC" ]] || die "sync requires a tier, e.g. 'sync dashboard' ($(valid_aliases))"
    proj="$(resolve_project "$SVC")" || true
    [[ -n "$proj" ]] || die "unknown tier '$SVC' -- $(valid_aliases)"
    command -v tar >/dev/null 2>&1 || die "tar not found on this devbox -- needed to pack the sync payload"

    # File set: explicit args win; otherwise auto-detect changed files under the
    # tier's project (working-tree diff vs the remote branch + untracked).
    files=()
    if [[ "${#SYNC_FILES[@]}" -gt 0 ]]; then
      for f in "${SYNC_FILES[@]}"; do
        if   [[ -e "$REPO_ROOT/$f" ]];        then files+=("$f")
        elif [[ -e "$REPO_ROOT/$proj/$f" ]];  then files+=("$proj/$f")
        else die "no such file: $f (looked under $REPO_ROOT and $REPO_ROOT/$proj)"
        fi
      done
    else
      base="$(sync_base_ref)"
      log "sync: detecting changed files in $proj/ vs $base"
      # core.quotePath=false so unicode-named assets come back as real paths
      # (else git C-quotes them and the on-disk existence check would miss them).
      mapfile -t files < <(
        { git_repo -c core.quotePath=false diff --name-only "$base" -- "$proj/";
          git_repo -c core.quotePath=false ls-files --others --exclude-standard -- "$proj/"; } \
        | sort -u
      )
    fi

    # Partition: compile-triggers BLOCK the whole sync (the fast lane must never
    # ship a stale binary or clobber a patched config); recognized static/markup
    # is sent; deletions and unknown types are reported and skipped.
    block=(); send=(); gone=(); skip=()
    for f in "${files[@]}"; do
      [[ -n "$f" ]] || continue
      case "$f" in "$proj"/*) : ;; *) skip+=("$f [outside $proj/]"); continue ;; esac
      [[ "/$f/" == *"/../"* ]] && { skip+=("$f [path traversal]"); continue; }
      fl="${f,,}"   # match extensions case-insensitively (e.g. Site.Master, .CS)
      if [[ "$fl" =~ \.(cs|csproj|resx|config)$ || "$fl" =~ (^|/)packages\.config$ || "$fl" =~ (^|/)(bin|obj)/ ]]; then
        block+=("$f")
      elif [[ "$fl" =~ \.(aspx|ascx|master|cshtml|html?|css|js|map|less|scss|json|png|jpe?g|gif|svg|ico|webp|avif|woff2?|ttf|eot|mp4)$ ]]; then
        if [[ -e "$REPO_ROOT/$f" ]]; then send+=("$f"); else gone+=("$f"); fi
      else
        skip+=("$f [not a syncable static/markup type]")
      fi
    done

    [[ "${#skip[@]}" -gt 0 ]] && for s in "${skip[@]}"; do printf '  [skip] %s\n' "$s"; done
    if [[ "${#gone[@]}" -gt 0 ]]; then
      printf '[!!] %s file(s) were DELETED locally; sync only copies, it cannot remove them on the box:\n' "${#gone[@]}"
      for g in "${gone[@]}"; do printf '       %s\n' "$g"; done
      printf '[!!] run "build %s" / "update %s" if a deletion must take effect.\n' "$SVC" "$SVC"
    fi
    if [[ "${#block[@]}" -gt 0 ]]; then
      printf '[xx] sync refused: %s changed file(s) need a COMPILE (or are patched config):\n' "${#block[@]}" >&2
      for b in "${block[@]}"; do printf '       %s\n' "$b" >&2; done
      die "use 'build $SVC' or 'update $SVC' -- the fast lane only ships static assets + markup"
    fi
    [[ "${#send[@]}" -gt 0 ]] || { log "sync: nothing to send for $proj (no changed static/markup files)"; exit 0; }

    # Pack relative to the project dir so entries land directly under the site
    # root (proj/Content/x.css -> Content/x.css), gzip + base64, size-check.
    rels=(); for f in "${send[@]}"; do rels+=("${f#$proj/}"); done
    printf '\n=== sync %s -> %s (%s file(s)) ===\n' "$SVC" "$proj" "${#send[@]}"
    for f in "${send[@]}"; do printf '  %s\n' "$f"; done
    # --force-local: never treat a 'C:'-style path as a remote host (git-bash tar).
    # --dereference: pack a symlink's target CONTENT as a regular file, so the
    # payload can never carry a symlink that would resolve outside the site root
    # when extracted on the box.
    b64=$(tar --force-local --dereference -czf - -C "$REPO_ROOT/$proj" "${rels[@]}" | base64 -w0)
    n=${#b64}
    if (( n > SYNC_MAX_B64 )); then
      die "sync payload too large (${n} B base64 > ${SYNC_MAX_B64} B). Too many/large files for the fast lane -- run 'build $SVC' / 'update $SVC', or raise SYNC_MAX_B64."
    fi

    run_on_vm "$DEPLOY_SCRIPTS/vm-sync-content.ps1" \
      "PayloadB64=$b64 Service=$SVC Project=$proj$([[ "$RECYCLE" -eq 1 ]] && printf ' Recycle=true')"
    log "sync complete: ${#send[@]} file(s) -> $proj$([[ "$RECYCLE" -eq 1 ]] && printf ' (recycled)')"
    ;;

  restart|start|stop)
    run_on_vm "$DEPLOY_SCRIPTS/vm-iis-ops.ps1" "Action=$CMD $(svc_param)"
    ;;

  status)
    [[ -z "$SVC" ]] || die "status takes no service argument"
    run_on_vm "$DEPLOY_SCRIPTS/vm-iis-ops.ps1" "Action=status"
    ;;

  logs)
    run_on_vm "$DEPLOY_SCRIPTS/vm-iis-ops.ps1" "Action=logs $(svc_param)"
    ;;

  schema)
    # Re-publish the dacpac only (incremental). Config must be on the box; push it
    # first so a standalone schema run after a fresh fetch still has DB_NAME etc.
    [[ -z "$SVC" ]] || die "schema takes no service argument"
    push_config
    run_on_vm "$DEPLOY_SCRIPTS/vm-publish-schema.ps1" "Env=$ENV"
    ;;

  -h|--help|help) usage 0 ;;
  *)              die "unknown command: $CMD (try: update build restart start stop status logs schema)" ;;
esac
