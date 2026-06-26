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
ENV="${ENV:-dev}"
CFG="$SCRIPT_DIR/config/.env.provision.${ENV}"

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

# -- Parse args --------------------------------------------------------------
CMD="${1:-}"; [[ -n "$CMD" ]] || usage 2
shift || true

SVC=""
WITH_SCHEMA=0
for arg in "$@"; do
  case "$arg" in
    --with-schema) WITH_SCHEMA=1 ;;
    -h|--help)     usage 0 ;;
    -*)            die "unknown option: $arg" ;;
    *)             [[ -z "$SVC" ]] || die "unexpected extra argument: $arg"; SVC="$arg" ;;
  esac
done

[[ "$WITH_SCHEMA" -eq 1 && "$CMD" != "update" ]] && die "--with-schema is only valid with 'update'"

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
