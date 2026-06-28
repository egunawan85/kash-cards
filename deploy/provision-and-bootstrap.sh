#!/usr/bin/env bash
# deploy/provision-and-bootstrap.sh
# Orchestrates the disposable dev shakeout end-to-end, mirroring the sister
# runegate-infra "provision-and-bootstrap" entrypoint. Local az phases run here;
# on-VM phases are invoked via `az vm run-command invoke`.
#
# Usage:
#   ENV=dev ./deploy/provision-and-bootstrap.sh            # provision + bootstrap only
#   ENV=dev ./deploy/provision-and-bootstrap.sh --with-deploy   # full pipeline incl. app deploy
#
# Idempotent: every underlying script is create-if-missing / reconcile, so a re-run
# is safe. Nothing here is destructive to anything outside the dev resource group.
set -euo pipefail

# Windows/git-bash: stop MSYS from rewriting args that start with '/' (Azure resource
# IDs / scopes like /subscriptions/...). Exported so all child scripts inherit it.
# Harmless on macOS/Linux.
export MSYS_NO_PATHCONV=1

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
ENV="${ENV:-dev}"
CFG="$SCRIPT_DIR/config/.env.provision.${ENV}"
WITH_DEPLOY=0
[[ "${1:-}" == "--with-deploy" ]] && WITH_DEPLOY=1

log()  { printf '\n=== %s ===\n' "$*"; }
die()  { printf '[xx] %s\n' "$*" >&2; exit 1; }

[[ -f "$CFG" ]] || die "missing $CFG (copy config/.env.provision.${ENV}.example and fill it)"
# shellcheck disable=SC1090
. "$CFG"
: "${VM_NAME:?}" : "${COMPUTE_RG:?}" : "${KEYVAULT_NAME:?}"
# --with-deploy additionally needs the source location and DB name.
if [[ "${1:-}" == "--with-deploy" ]]; then : "${REPO_URL:?set in $CFG}" : "${DB_NAME:?set in $CFG}"; fi
REPO_BRANCH="${REPO_BRANCH:-main}"

# Windows/git-bash: az needs a Windows path for @file args (cygpath). Passthrough on Linux.
winpath() { if command -v cygpath >/dev/null 2>&1; then cygpath -w "$1"; else printf '%s' "$1"; fi; }

# Helper: run a PowerShell script ON the VM and stream its output.
run_on_vm() {
  local script="$1"; shift
  local params="${1:-}"
  log "VM: $(basename "$script") ${params}"
  # `az vm run-command invoke` returns success even when the inner PowerShell fails, so
  # capture its combined output and fail the orchestrator if the script printed a fatal
  # marker ([xx], used by every VM-side script's Stop-*/die). Otherwise a broken phase
  # would silently cascade.
  local out
  out=$(az vm run-command invoke -g "$COMPUTE_RG" -n "$VM_NAME" \
     --command-id RunPowerShellScript --scripts "@$(winpath "$script")" ${params:+--parameters $params} \
     --query "value[0].message" -o tsv)
  printf '%s\n' "$out"
  if printf '%s' "$out" | grep -q '\[xx\]'; then
    die "VM phase failed: $(basename "$script") -- see [xx] above"
  fi
}

# ── Phase 1: Azure resources (RG, network, VM, Key Vault, Log Analytics) ──────
log "Phase 1: provision Azure resources"
ENV="$ENV" "$SCRIPT_DIR/scripts/provision/azure-vm-provision.sh"

# ── Phase 2: seed the rotated/dev secrets into Key Vault ─────────────────────
log "Phase 2: seed Key Vault from deploy/secrets"
ENV="$ENV" "$SCRIPT_DIR/scripts/secrets/seed-kv-secrets.sh"

# ── Phase 3: bootstrap the VM (IIS, build tools, SQL Express, app login) ─────
run_on_vm "$SCRIPT_DIR/scripts/provision/vm-bootstrap.ps1" "KvName=$KEYVAULT_NAME Env=$ENV DbName=${DB_NAME:-qrypto-card} DbAppLogin=${DB_APP_LOGIN:-kash_app}"

if [[ "$WITH_DEPLOY" -eq 0 ]]; then
  log "DONE (provision + bootstrap). Re-run with --with-deploy for the app pipeline."
  exit 0
fi

# ── Phase 4: app + DB + perimeter (build-on-box) ─────────────────────────────
run_on_vm "$SCRIPT_DIR/scripts/deploy/vm-fetch-source.ps1" "RepoUrl=$REPO_URL Branch=$REPO_BRANCH KvName=$KEYVAULT_NAME"
# The filled infra config is gitignored, so it isn't in the clone -- push it to the VM
# (base64 to survive run-command parameter transport) so the deploy scripts can source it.
CFG_B64=$(base64 -w0 "$CFG")
run_on_vm "$SCRIPT_DIR/scripts/deploy/vm-write-config.ps1" "ConfigB64=$CFG_B64 Env=$ENV"
run_on_vm "$SCRIPT_DIR/scripts/deploy/vm-install-sqlpackage.ps1"
run_on_vm "$SCRIPT_DIR/scripts/deploy/vm-migrate.ps1" "Env=$ENV" # baseline (38 tables) + migrations -> SQL Express
run_on_vm "$SCRIPT_DIR/scripts/deploy/deploy-iis.ps1"   "Env=$ENV" # 12 IIS sites + WCF/connstr rewrites
run_on_vm "$SCRIPT_DIR/scripts/deploy/inject-secrets.ps1" "Env=$ENV" # KV -> per-pool env
run_on_vm "$SCRIPT_DIR/scripts/deploy/vm-seed.ps1" "KvName=$KEYVAULT_NAME DbName=$DB_NAME Env=$ENV"
# Cloudflare perimeter: create the tunnel + store its connector token in Key Vault FIRST
# (runs locally; talks to the Cloudflare API), THEN install the connector on the VM, which
# pulls that token from KV. Installing the connector before the tunnel exists hangs waiting
# for a token that isn't there yet.
log "Phase 5: Cloudflare perimeter (tunnel + token)"
ENV="$ENV" "$SCRIPT_DIR/scripts/perimeter/cloudflare-setup.sh"
run_on_vm "$SCRIPT_DIR/scripts/perimeter/vm-install-cloudflared.ps1" "KvName=$KEYVAULT_NAME Env=$ENV"

# ── Phase 6: start-guarantee ──────────────────────────────────────────────────
# Belt-and-braces: deploy-iis starts each pool and inject-secrets recycles them,
# but a Start-WebAppPool that no-ops or errors under -ErrorAction SilentlyContinue
# can leave a tier silently Stopped (the WCF money tiers were observed Stopped at
# the end of a full run). This phase explicitly starts every pool and VERIFIES it
# reaches Started, failing the orchestrator ([xx]) if any does not -- so the
# pipeline can never finish with a money tier down.
run_on_vm "$SCRIPT_DIR/scripts/deploy/vm-iis-ops.ps1" "Action=start Env=$ENV"

# ── Phase 7: verify ──────────────────────────────────────────────────────────
run_on_vm "$SCRIPT_DIR/scripts/verify/vm-verify.ps1" "Env=$ENV"

log "DONE (full dev shakeout). Load deploy/secrets/.smoke.env and run: dotnet test QryptoCard.Tests.Smoke"
