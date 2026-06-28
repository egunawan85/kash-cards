#!/usr/bin/env bash
# seed-kv-secrets.sh -- upload the operator-authored secret/config values into
# Azure Key Vault so the VM can pull them via managed identity at deploy time
# (inject-secrets.ps1 reads them back and writes per-app-pool env vars).
#
# Adapted from runegate-infra/scripts/secrets/seed-kv-secrets.sh. The sister
# uploads whole .env/.vault BLOBS plus per-key entries and can auto-generate
# crypto material. kash-cards is simpler: ONE app, ONE Key Vault, and the app
# reads secrets purely as process env vars (see deploy/README.md), so we upload
# ONLY per-key entries -- one KV secret per KEY=VALUE line across both files.
#
# ── KV secret-name convention (underscore <-> dash) ──────────────────────────
# Key Vault secret names allow only [A-Za-z0-9-]; our env-var names are
# UPPER_SNAKE_CASE ([A-Za-z0-9_]). We map the name by replacing every '_' with
# '-'  (e.g. DB_PASSWORD -> DB-PASSWORD, PGCRYPTO_WEBHOOK_SECRET ->
# PGCRYPTO-WEBHOOK-SECRET).
#
# This is a clean, REVERSIBLE bijection for this key set: none of the env-var
# names in .env/.vault contain a literal '-', so inject-secrets.ps1 reverses it
# unambiguously by replacing every '-' back to '_' to recover the original
# env-var name. (If a future key ever needs a literal dash, it would collide
# under this scheme -- keep env-var names underscore-only to preserve the
# round-trip.) This script EMITS the NAME -> KV-secret mapping for each upload
# so the reverse mapping is auditable from the log.
#
# Reads (relative to deploy/), selected by ENV -- each environment has its OWN
# secret set (the runegate-infra convention; dev and prod never share values):
#   secrets/.env.<env>    non-secret config (DB host/name/user, URLs, env gate)
#   secrets/.vault.<env>  secrets (DB password, provider keys)
# There is NO fallback to an unsuffixed file, so a missing .env.prod can never
# quietly seed dev values into the prod Key Vault.
# Both are flat KEY=VALUE; comments (#...) and blank lines are skipped, and any
# KEY whose value is blank is skipped (operator hasn't filled it in yet).
#
# Idempotent: hash-compares the local value against what's already in KV and
# skips the upload when identical (no churn in the KV audit log). NEVER prints
# secret VALUES -- only names. Values reach `az` via a mode-600 temp file, never
# on the command line (argv is world-readable to local processes).
#
# Two ways to run:
#   1. Inline from the provision wrapper (first-time seed).
#      Skip with SKIP_SEED_KV=true.
#   2. Standalone for rotation:
#        ./seed-kv-secrets.sh                 (defaults to ENV=dev)
#        ENV=dev  ./seed-kv-secrets.sh
#        ENV=prod ./seed-kv-secrets.sh        (seeds the prod KV from .env.prod/.vault.prod)

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"   # .../deploy/scripts/secrets
DEPLOY_ROOT="$(cd "$SCRIPT_DIR/../.." && pwd)"                  # .../deploy
CONFIG_DIR="$DEPLOY_ROOT/config"
SECRETS_DIR="$DEPLOY_ROOT/secrets"
ENV="${ENV:-dev}"
ENV_FILE="$CONFIG_DIR/.env.provision.${ENV}"

log()  { printf '[..] %s\n' "$*"; }
ok()   { printf '[ok] %s\n' "$*"; }
warn() { printf '[!!] %s\n' "$*" >&2; }
die()  { printf '[xx] %s\n' "$*" >&2; exit 1; }

# Windows/git-bash: az needs a Windows path for --file args (cygpath). Passthrough on Linux.
winpath() { if command -v cygpath >/dev/null 2>&1; then cygpath -w "$1"; else printf '%s' "$1"; fi; }

# Honor wrapper-level skip.
if [[ "${SKIP_SEED_KV:-false}" == "true" ]]; then
    ok "SKIP_SEED_KV=true -- leaving KV untouched (operator opted out)"
    exit 0
fi

# Preflight.
[[ -f "$ENV_FILE" ]] || die "missing $ENV_FILE -- copy .env.provision.${ENV}.example and fill it in"
command -v az      >/dev/null 2>&1 || die "az CLI not on PATH"
command -v openssl >/dev/null 2>&1 || die "openssl not on PATH (used for content hashing)"

# shellcheck disable=SC1090
. "$ENV_FILE"
: "${KEYVAULT_NAME:?missing KEYVAULT_NAME in $ENV_FILE}"
KV_NAME="$KEYVAULT_NAME"
log "Key Vault: $KV_NAME"

# Per-env secret sets (no unsuffixed fallback -- see header).
ENV_SECRETS="$SECRETS_DIR/.env.${ENV}"
VAULT_SECRETS="$SECRETS_DIR/.vault.${ENV}"

# Fail loudly if THIS environment has no secret files at all. Without this a typo'd
# or unset ENV (e.g. seeding "prod" before .env.prod/.vault.prod exist) would upload
# nothing and exit 0 -- a silent empty Key Vault that only surfaces as app startup
# faults after deploy. Require at least one of the pair to exist.
if [[ ! -f "$ENV_SECRETS" && ! -f "$VAULT_SECRETS" ]]; then
    die "no secret files for ENV=$ENV ($ENV_SECRETS / $VAULT_SECRETS) -- copy secrets/.env.example + .vault.example to the .$ENV names and fill them"
fi

# Hash stdin -> hex sha256. Portable across macOS / Linux.
sha256() { openssl dgst -sha256 | awk '{print $NF}'; }

# Yield each "KEY=VALUE" line's KEY from $1 (skips comments / blanks). Pure
# bash + BASH_REMATCH so it runs on macOS bash 3.2 and Linux bash 4+.
list_keys() {
    while IFS= read -r line || [[ -n "$line" ]]; do
        [[ -z "${line//[[:space:]]/}" ]] && continue
        [[ "$line" =~ ^[[:space:]]*# ]] && continue
        if [[ "$line" =~ ^[[:space:]]*([A-Za-z_][A-Za-z0-9_]*)[[:space:]]*= ]]; then
            printf '%s\n' "${BASH_REMATCH[1]}"
        fi
    done < "$1"
}

# Single-line KEY=VALUE extraction: strips surrounding whitespace, surrounding
# quotes, and unquoted inline comments. The leading ^# short-circuit prevents a
# `KEY=   #comment` line from uploading the comment text as a value.
extract_value() {
    awk -F= -v k="$1" '
        /^[[:space:]]*#/ { next }
        $0 !~ "^[[:space:]]*"k"[[:space:]]*=" { next }
        {
            v = substr($0, index($0,"=") + 1)
            sub(/^[[:space:]]+/, "", v); sub(/[[:space:]]+$/, "", v)
            if (v ~ /^".*"$/)            { v = substr(v, 2, length(v) - 2) }
            else if (v ~ /^'\''.*'\''$/) { v = substr(v, 2, length(v) - 2) }
            else if (v ~ /^#/)           { v = "" }
            else                         { sub(/[[:space:]]+#.*$/, "", v) }
            print v
            exit
        }' "$2"
}

# Map an env-var name to its KV secret name: every '_' -> '-'. Reversible for
# this key set because the source names never contain a literal '-'.
kv_name_for() { printf '%s' "${1//_/-}"; }

# Upload a single scalar value to KV without putting it on the argv (which is
# visible to any local process via `ps`/Get-Process). umask 077 forces the
# mktemp file to 600 regardless of the caller's umask.
upload_kv_value_via_tmpfile() {
    local secret_name="$1" value="$2" tmp rc=0
    tmp=$( (umask 077 && mktemp) )
    [[ -f "$tmp" ]] || { warn "$secret_name: failed to create temp file"; return 1; }
    printf '%s' "$value" > "$tmp"
    az keyvault secret set --vault-name "$KV_NAME" --name "$secret_name" --file "$(winpath "$tmp")" >/dev/null || rc=$?
    rm -f "$tmp"
    return "$rc"
}

uploaded=0
unchanged=0
skipped_blank=0

# Process one KEY=VALUE source file. $2 is a human label for the log only.
seed_file() {
    local src_file="$1" label="$2"
    if [[ ! -f "$src_file" ]]; then
        warn "$label not found at $src_file -- skipping (copy the matching .example and fill it in)"
        return 0
    fi
    log "reading $label ($src_file)"

    local key value secret_name local_hash remote_value remote_hash
    while IFS= read -r key; do
        value=$(extract_value "$key" "$src_file")
        secret_name=$(kv_name_for "$key")

        # Skip blanks -- operator hasn't filled this one in yet. Loud enough at
        # runtime (the app faults on a missing preloaded secret); no need to
        # list every empty template line here.
        if [[ -z "$value" ]]; then
            skipped_blank=$((skipped_blank + 1))
            continue
        fi

        # Hash-compare against the current KV value to suppress no-op uploads.
        local_hash=$(printf '%s' "$value" | sha256)
        remote_value=$(az keyvault secret show \
            --vault-name "$KV_NAME" --name "$secret_name" \
            --query value -o tsv 2>/dev/null || true)
        if [[ -n "$remote_value" ]]; then
            remote_hash=$(printf '%s' "$remote_value" | sha256)
            if [[ "$local_hash" == "$remote_hash" ]]; then
                ok "$key -> $secret_name: unchanged (matches KV)"
                unchanged=$((unchanged + 1))
                continue
            fi
            log "$key -> $secret_name: value differs -- uploading new version"
        else
            log "$key -> $secret_name: not in KV -- uploading initial version"
        fi

        upload_kv_value_via_tmpfile "$secret_name" "$value" \
            || die "$key -> $secret_name: upload failed (check 'Key Vault Secrets Officer' on $KV_NAME)"
        ok "$key -> $secret_name: uploaded"
        uploaded=$((uploaded + 1))
    done < <(list_keys "$src_file")
}

# Both files are seeded intentionally: .vault holds secrets, and .env holds the
# non-secret runtime config (env gate, provider URLs, sender) that inject-secrets.ps1
# injects as pool env on the server -- the app reads it via SecretsConfig, and
# load-env.ps1 is local-only. Key Vault is the on-box config source either way.
seed_file "$ENV_SECRETS"   ".env.${ENV}"
seed_file "$VAULT_SECRETS" ".vault.${ENV}"

echo
ok "seed-kv-secrets done: $uploaded uploaded, $unchanged unchanged, $skipped_blank skipped (blank)"
