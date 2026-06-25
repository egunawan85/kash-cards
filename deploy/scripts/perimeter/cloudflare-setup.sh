#!/usr/bin/env bash
# Adapted from runegate-infra/scripts/perimeter/cloudflare-setup.sh
#
# cloudflare-setup.sh -- idempotent Cloudflare configuration for the kash-cards
# stack. The VM has NO inbound 443 (the NSG denies it); all public reach comes
# in through a Cloudflare tunnel that connects OUTBOUND from the box to the edge
# and forwards to the per-site loopback ports defined in config/sites.json.
#
# Two modes, selected by CLOUDFLARE_QUICK_TUNNEL (from config/.env.provision.<env>):
#
#   QUICK  (=true, the dev default)
#     A Cloudflare *quick* tunnel (`cloudflared tunnel --url ...`) needs no
#     Cloudflare account, no zone, and no API token -- it just prints a random
#     https://<random>.trycloudflare.com URL and forwards it to ONE local
#     service. That single-URL limit is the whole story: a quick tunnel cannot
#     express the 8 distinct public hostnames this app needs. So in this mode we
#     expose only the PRIMARY public site -- the Dashboard on :8087 -- enough to
#     smoke-test the box end to end through the real Cloudflare edge. The other 7
#     public sites stay loopback-only until the named-tunnel (prod) path runs.
#     This mode does NOT touch the Cloudflare API; it only prints the command to
#     run on the VM and records the intent. The real per-site zone wiring (named
#     tunnel + ingress + DNS + zone hardening + WAF) happens with QUICK=false.
#
#   NAMED  (=false, the prod-shaped path)
#     Create/find a *named* tunnel (CLOUDFLARE_TUNNEL_NAME), push its connector
#     token to Key Vault, generate ingress rules from sites.json mapping each
#     public site's hostname "<hostPrefix>-<ENV>.<CLOUDFLARE_ZONE>" -> the local
#     loopback port, create the proxied CNAME records, apply zone hardening
#     (min TLS 1.2 + TLS 1.3, Always-HTTPS, HSTS), turn Browser-Integrity-Check
#     OFF for the callback host (it rejects the WasabiCard/Runegate webhook
#     POSTs), and install an interim edge IP-allowlist on the callback host that
#     permits ONLY the provider source IPs. Idempotent: every run converges.
#
# Inputs (config/.env.provision.<env>, sourced below):
#   ENV, CLOUDFLARE_QUICK_TUNNEL, CLOUDFLARE_ZONE, CLOUDFLARE_TUNNEL_NAME,
#   KEYVAULT_NAME, AZURE_SUBSCRIPTION_ID
# NAMED mode additionally needs (env or config/.env.cloudflare.<env>):
#   CLOUDFLARE_API_TOKEN, CLOUDFLARE_ACCOUNT_ID, CLOUDFLARE_ZONE_ID
#
# NAMED-mode API token scopes:
#   - Account / Cloudflare Tunnel / Edit
#   - Zone    / DNS               / Edit
#   - Zone    / Zone WAF          / Edit
#   - Zone    / Zone Settings     / Edit
#   - Zone    / Zone              / Read
#
# Does NOT run the VM-side install; that's vm-install-cloudflared.ps1. No secret
# values are committed -- the connector token is fetched from the CF API at run
# time and written only to Key Vault.

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
DEPLOY_ROOT="$(cd "$SCRIPT_DIR/../.." && pwd)"
CONFIG_DIR="$DEPLOY_ROOT/config"
SITES_JSON="$CONFIG_DIR/sites.json"
# jq here is a native Windows binary; when the caller exports MSYS_NO_PATHCONV=1
# (the orchestrator does, for az), a unix /c/... path reaches jq unconverted and it
# cannot open the file. Hand jq a forward-slash Windows path (cygpath -m), which the
# bash builtins below (-f test, dot-source) also accept.
if command -v cygpath >/dev/null 2>&1; then SITES_JSON="$(cygpath -m "$SITES_JSON")"; fi
ENV="${ENV:-dev}"
ENV_FILE="$CONFIG_DIR/.env.provision.${ENV}"
CF_FILE="$CONFIG_DIR/.env.cloudflare.${ENV}"   # optional; NAMED-mode CF creds

log()  { printf '[..] %s\n' "$*"; }
ok()   { printf '[ok] %s\n' "$*"; }
warn() { printf '[!!] %s\n' "$*" >&2; }
die()  { printf '[xx] %s\n' "$*" >&2; exit 1; }

# -- Preflight --------------------------------------------------------------
[[ -f "$ENV_FILE" ]]  || die "missing $ENV_FILE -- copy from .env.provision.${ENV}.example and fill in"
[[ -f "$SITES_JSON" ]] || die "missing $SITES_JSON"
command -v jq >/dev/null 2>&1 || die "jq not on PATH (brew install jq / choco install jq)"

# shellcheck disable=SC1090
. "$ENV_FILE"

: "${CLOUDFLARE_ZONE:?missing CLOUDFLARE_ZONE in $ENV_FILE}"
: "${CLOUDFLARE_TUNNEL_NAME:?missing CLOUDFLARE_TUNNEL_NAME in $ENV_FILE}"
: "${KEYVAULT_NAME:?missing KEYVAULT_NAME in $ENV_FILE}"
QUICK="${CLOUDFLARE_QUICK_TUNNEL:-true}"

ENV_UPPER=$(printf '%s' "$ENV" | tr '[:lower:]' '[:upper:]')
KV_SECRET_NAME="CLOUDFLARED-TUNNEL-TOKEN-${ENV_UPPER}"

# Compose a public site's external hostname: "<hostPrefix>-<ENV>.<zone>".
site_hostname() { printf '%s-%s.%s' "$1" "$ENV" "$CLOUDFLARE_ZONE"; }

# Primary public site for the quick-tunnel single-URL exposure: the Dashboard.
PRIMARY_PREFIX="dashboard"
PRIMARY_PORT=$(jq -r --arg p "$PRIMARY_PREFIX" \
    '.public[] | select(.hostPrefix == $p) | .port' "$SITES_JSON")
[[ -n "$PRIMARY_PORT" && "$PRIMARY_PORT" != "null" ]] \
    || die "could not find primary public site (hostPrefix=$PRIMARY_PREFIX) in $SITES_JSON"

# Callback host: BIC-off + the interim edge IP-lock target.
CALLBACK_PREFIX="callback"
CALLBACK_PORT=$(jq -r --arg p "$CALLBACK_PREFIX" \
    '.public[] | select(.hostPrefix == $p) | .port' "$SITES_JSON")
[[ -n "$CALLBACK_PORT" && "$CALLBACK_PORT" != "null" ]] \
    || die "could not find callback site (hostPrefix=$CALLBACK_PREFIX) in $SITES_JSON"
CALLBACK_HOST="$(site_hostname "$CALLBACK_PREFIX")"

# ----------------------------------------------------------------------------
# Interim edge IP-lock allowlist for the callback host.
#
# PLACEHOLDER -- fill with the real WasabiCard / Runegate provider egress CIDRs
# before relying on this in prod. Until the provider publishes a stable source
# range, this is the documented config seam: any IP NOT in this list is blocked
# at the edge on the callback host. The managed-code signature check on the
# callback endpoint is the real authority; this is the perimeter belt to that
# suspenders, so a leaked URL is not enough to even reach the origin.
#
# Format: Cloudflare IP-list expression literals, space-separated, e.g.
#   CALLBACK_ALLOW_IPS=("203.0.113.0/24" "198.51.100.7")
# Leave empty to SKIP the IP-lock rule (logged as skipped, not an error) so the
# dev shakeout isn't blocked on provider IPs that aren't known yet.
# ----------------------------------------------------------------------------
CALLBACK_ALLOW_IPS=(
    # "203.0.113.0/24"   # <-- WasabiCard egress range (PLACEHOLDER)
    # "198.51.100.0/24"  # <-- Runegate provider range (PLACEHOLDER)
)

log "Environment        : $ENV"
log "Cloudflare zone    : $CLOUDFLARE_ZONE"
log "Tunnel name        : $CLOUDFLARE_TUNNEL_NAME"
log "Mode               : $([[ "$QUICK" == "true" ]] && echo 'QUICK (single-URL dev)' || echo 'NAMED (per-site)')"

# ============================================================================
# MODE A: QUICK TUNNEL (dev default) -- single URL, primary site only
# ============================================================================
if [[ "$QUICK" == "true" ]]; then
    echo
    log "=== Quick tunnel (dev shakeout) ==="
    warn "CLOUDFLARE_QUICK_TUNNEL=true: a Cloudflare quick tunnel maps exactly ONE"
    warn "URL to ONE local service. It CANNOT serve the 8 distinct public"
    warn "hostnames this app needs -- so only the PRIMARY public site (the"
    warn "Dashboard, :$PRIMARY_PORT) is exposed for the end-to-end smoke test."
    warn "The other 7 public sites and all 4 INT tiers stay loopback-only."
    warn "Set CLOUDFLARE_QUICK_TUNNEL=false (with the kash.cards zone + an API"
    warn "token) to bring up the real per-site named tunnel at prod."
    echo
    log "Run this ON THE VM to start the quick tunnel (foreground; for a service"
    log "use the named-tunnel path instead):"
    echo
    echo "    cloudflared tunnel --url http://localhost:${PRIMARY_PORT}"
    echo
    log "cloudflared prints a https://<random>.trycloudflare.com URL on start --"
    log "that single URL fronts the Dashboard. No zone, account, or token needed."
    echo
    ok "Quick-tunnel guidance emitted (no Cloudflare API state changed)."
    ok "Cloudflare setup complete for $ENV (quick mode)."
    exit 0
fi

# ============================================================================
# MODE B: NAMED TUNNEL (prod-shaped) -- per-site ingress, DNS, hardening, WAF
# ============================================================================
# NAMED mode needs the CF API. Creds may live in env or config/.env.cloudflare.<env>.
if [[ -f "$CF_FILE" ]]; then
    # shellcheck disable=SC1090
    . "$CF_FILE"
fi
command -v az   >/dev/null 2>&1 || die "az CLI not on PATH (needed to push the connector token to Key Vault)"
command -v curl >/dev/null 2>&1 || die "curl not on PATH"

: "${CLOUDFLARE_API_TOKEN:?missing CLOUDFLARE_API_TOKEN (env or $CF_FILE)}"
: "${CLOUDFLARE_ACCOUNT_ID:?missing CLOUDFLARE_ACCOUNT_ID (env or $CF_FILE)}"
: "${CLOUDFLARE_ZONE_ID:?missing CLOUDFLARE_ZONE_ID (env or $CF_FILE)}"

log "Key Vault          : $KEYVAULT_NAME"
log "KV secret          : $KV_SECRET_NAME"
log "Callback host      : $CALLBACK_HOST (BIC off + interim IP-lock)"

CF_API="https://api.cloudflare.com/client/v4"

cf_api() {
    # cf_api METHOD PATH [JSON-BODY] -- echoes body, dies on success=false.
    local method="$1" path="$2" body="${3:-}" response
    if [[ -n "$body" ]]; then
        response=$(curl -sS -X "$method" "${CF_API}${path}" \
            -H "Authorization: Bearer ${CLOUDFLARE_API_TOKEN}" \
            -H "Content-Type: application/json" \
            --data "$body")
    else
        response=$(curl -sS -X "$method" "${CF_API}${path}" \
            -H "Authorization: Bearer ${CLOUDFLARE_API_TOKEN}")
    fi
    if [[ "$(jq -r '.success' <<<"$response")" != "true" ]]; then
        warn "Cloudflare API call failed: $method $path"
        jq -r '.errors[]? | "  \(.code): \(.message)"' <<<"$response" >&2 || true
        die "abort"
    fi
    printf '%s' "$response"
}

log "Verifying API token..."
cf_api GET "/user/tokens/verify" >/dev/null
ok "API token is valid"

# ---------------------------------------------------------------------------
# Section 1: tunnel + ingress + DNS
# ---------------------------------------------------------------------------
echo
log "=== Section 1: tunnel, ingress, DNS ==="

# Find or create the named tunnel.
LIST=$(cf_api GET "/accounts/${CLOUDFLARE_ACCOUNT_ID}/cfd_tunnel?name=${CLOUDFLARE_TUNNEL_NAME}&is_deleted=false")
TUNNEL_ID=$(jq -r '.result[0].id // empty' <<<"$LIST")
if [[ -z "$TUNNEL_ID" ]]; then
    log "Tunnel '$CLOUDFLARE_TUNNEL_NAME' not found; creating..."
    CREATE_BODY=$(jq -n --arg name "$CLOUDFLARE_TUNNEL_NAME" '{name: $name, config_src: "cloudflare"}')
    CREATE=$(cf_api POST "/accounts/${CLOUDFLARE_ACCOUNT_ID}/cfd_tunnel" "$CREATE_BODY")
    TUNNEL_ID=$(jq -r '.result.id' <<<"$CREATE")
    ok "Created tunnel id=$TUNNEL_ID"
else
    ok "Found existing tunnel id=$TUNNEL_ID"
fi
TUNNEL_HOST="${TUNNEL_ID}.cfargotunnel.com"

# Fetch connector token + push to Key Vault (idempotent: only writes on diff).
TOKEN_RESP=$(cf_api GET "/accounts/${CLOUDFLARE_ACCOUNT_ID}/cfd_tunnel/${TUNNEL_ID}/token")
TOKEN=$(jq -r '.result' <<<"$TOKEN_RESP")
[[ -n "$TOKEN" && "$TOKEN" != "null" ]] || die "empty connector token from CF API"
EXISTING=$(az keyvault secret show \
    --vault-name "$KEYVAULT_NAME" --name "$KV_SECRET_NAME" \
    --query value -o tsv 2>/dev/null || true)
if [[ "$EXISTING" == "$TOKEN" ]]; then
    ok "KV $KV_SECRET_NAME already current (no upload)"
else
    log "Uploading connector token to KV..."
    az keyvault secret set \
        --vault-name "$KEYVAULT_NAME" --name "$KV_SECRET_NAME" \
        --value "$TOKEN" --output none
    ok "KV $KV_SECRET_NAME updated"
fi

# Build desired ingress from sites.json: one rule per PUBLIC site
# (<hostPrefix>-<ENV>.<zone> -> http://localhost:<port>). INT tiers are
# loopback-only and deliberately get NO ingress. Catch-all 404 must be last.
INGRESS_JSON='[]'
while IFS=$'\t' read -r prefix port; do
    [[ -z "$prefix" ]] && continue
    hostname="$(site_hostname "$prefix")"
    INGRESS_JSON=$(jq --arg h "$hostname" --arg s "http://localhost:${port}" \
        '. + [{hostname: $h, service: $s}]' <<<"$INGRESS_JSON")
done < <(jq -r '.public[] | "\(.hostPrefix)\t\(.port)"' "$SITES_JSON" | tr -d '\r')
N_ROUTES=$(jq length <<<"$INGRESS_JSON")
INGRESS_JSON=$(jq '. + [{service: "http_status:404"}]' <<<"$INGRESS_JSON")

CONFIG_BODY=$(jq -n --argjson ingress "$INGRESS_JSON" '{config: {ingress: $ingress}}')
EXISTING_CONFIG=$(cf_api GET "/accounts/${CLOUDFLARE_ACCOUNT_ID}/cfd_tunnel/${TUNNEL_ID}/configurations")
EXISTING_INGRESS=$(jq -cS '.result.config.ingress // []' <<<"$EXISTING_CONFIG")
DESIRED_INGRESS=$(jq -cS '.' <<<"$INGRESS_JSON")
if [[ "$EXISTING_INGRESS" == "$DESIRED_INGRESS" ]]; then
    ok "Tunnel ingress already current ($N_ROUTES public route(s))"
else
    # Surface removed hostnames before the PUT so a hand-added dashboard route
    # doesn't vanish silently on a routine re-run.
    EXISTING_HOSTS=$(jq -r '.[]?.hostname // empty' <<<"$EXISTING_INGRESS" | sort -u)
    DESIRED_HOSTS=$(jq -r '.[]?.hostname // empty' <<<"$DESIRED_INGRESS" | sort -u)
    REMOVED=$(comm -23 <(printf '%s\n' "$EXISTING_HOSTS") <(printf '%s\n' "$DESIRED_HOSTS") || true)
    if [[ -n "$REMOVED" ]]; then
        while IFS= read -r host; do [[ -n "$host" ]] && warn "removing ingress route: $host"; done <<<"$REMOVED"
    fi
    log "Updating tunnel ingress..."
    cf_api PUT "/accounts/${CLOUDFLARE_ACCOUNT_ID}/cfd_tunnel/${TUNNEL_ID}/configurations" "$CONFIG_BODY" >/dev/null
    ok "Tunnel ingress updated ($N_ROUTES public route(s))"
fi

# Create or update a proxied CNAME for each public site hostname.
while IFS=$'\t' read -r prefix _port; do
    [[ -z "$prefix" ]] && continue
    hostname="$(site_hostname "$prefix")"
    EXISTING_REC=$(cf_api GET "/zones/${CLOUDFLARE_ZONE_ID}/dns_records?type=CNAME&name=${hostname}")
    REC_ID=$(jq -r '.result[0].id // empty' <<<"$EXISTING_REC")
    REC_CONTENT=$(jq -r '.result[0].content // empty' <<<"$EXISTING_REC")
    REC_PROXIED=$(jq -r '.result[0].proxied // false' <<<"$EXISTING_REC")
    DESIRED_BODY=$(jq -n --arg name "$hostname" --arg content "$TUNNEL_HOST" \
        '{type: "CNAME", name: $name, content: $content, proxied: true, ttl: 1, comment: "managed by deploy/scripts/perimeter/cloudflare-setup.sh"}')
    if [[ -z "$REC_ID" ]]; then
        log "Creating CNAME $hostname -> $TUNNEL_HOST..."
        cf_api POST "/zones/${CLOUDFLARE_ZONE_ID}/dns_records" "$DESIRED_BODY" >/dev/null
        ok "CNAME $hostname created"
    elif [[ "$REC_CONTENT" != "$TUNNEL_HOST" || "$REC_PROXIED" != "true" ]]; then
        log "Updating CNAME $hostname (was: $REC_CONTENT proxied=$REC_PROXIED)..."
        cf_api PUT "/zones/${CLOUDFLARE_ZONE_ID}/dns_records/${REC_ID}" "$DESIRED_BODY" >/dev/null
        ok "CNAME $hostname updated"
    else
        ok "CNAME $hostname already current"
    fi
done < <(jq -r '.public[] | "\(.hostPrefix)\t\(.port)"' "$SITES_JSON" | tr -d '\r')

# ---------------------------------------------------------------------------
# Capability gate for Sections 2-3.
# ---------------------------------------------------------------------------
# Zone hardening + WAF need 'Zone Settings:Edit' and 'Zone WAF:Edit' on the token.
# A token scoped only to Account Tunnel + Zone DNS (enough for Section 1, which is
# what actually makes the app reachable) returns 10000 auth-error here. Probe once
# with a raw GET; if it can't read zone settings, skip hardening with a clear note
# instead of aborting. Sections 2-3 are idempotent, so a later run with a broader
# token completes the hardening.
_zhprobe=$(curl -sS -X GET "${CF_API}/zones/${CLOUDFLARE_ZONE_ID}/settings/always_use_https" \
    -H "Authorization: Bearer ${CLOUDFLARE_API_TOKEN}")
if [[ "$(jq -r '.success' <<<"$_zhprobe")" != "true" ]]; then
    echo
    warn "API token lacks Zone Settings / WAF permission -- skipping zone hardening + WAF (Sections 2-3)."
    warn "Tunnel, ingress, and DNS are live (the app is reachable). For full hardening, add"
    warn "'Zone Settings:Edit' + 'Zone WAF:Edit' to the token and re-run (idempotent)."
    echo
    ok "cloudflare-setup done (Section 1: tunnel + ingress + DNS; hardening deferred -- token scope)"
    exit 0
fi

# ---------------------------------------------------------------------------
# Section 2: zone hardening (TLS / HSTS / security)
# ---------------------------------------------------------------------------
echo
log "=== Section 2: zone hardening ==="

# Scalar settings: PATCH /zones/{id}/settings/{key} with {"value":...}.
# Each row: "<api-key>:<desired-value>:<human-label>". Browser Integrity Check
# is set OFF per-host (Section 3) for the callback receiver, not zone-wide.
SIMPLE_SETTINGS=(
    "always_use_https:on:Always Use HTTPS"
    "min_tls_version:1.2:Minimum TLS Version"
    "tls_1_3:on:TLS 1.3"
    "automatic_https_rewrites:on:Automatic HTTPS Rewrites"
    "ssl:strict:SSL/TLS encryption mode (Full strict)"
    "security_level:medium:Security Level"
)
for row in "${SIMPLE_SETTINGS[@]}"; do
    key="${row%%:*}"; rest="${row#*:}"; desired="${rest%%:*}"; label="${rest#*:}"
    current=$(jq -r '.result.value' <<<"$(cf_api GET "/zones/${CLOUDFLARE_ZONE_ID}/settings/${key}")")
    if [[ "$current" == "$desired" ]]; then
        ok "${label}: already '${desired}'"
    else
        log "${label}: '${current}' -> '${desired}'"
        cf_api PATCH "/zones/${CLOUDFLARE_ZONE_ID}/settings/${key}" "$(jq -nc --arg v "$desired" '{value: $v}')" >/dev/null
        ok "${label}: set to '${desired}'"
    fi
done

# HSTS: structured value. Compare with jq -S so CF field-order doesn't trigger
# a spurious update on idempotent re-runs.
hsts_desired=$(jq -nc '{value: {strict_transport_security: {enabled: true, max_age: 31536000, include_subdomains: true, nosniff: true, preload: false}}}')
hsts_current=$(jq -cS '.result.value' <<<"$(cf_api GET "/zones/${CLOUDFLARE_ZONE_ID}/settings/security_header")")
hsts_target=$(jq -cS '.value' <<<"$hsts_desired")
if [[ "$hsts_current" == "$hsts_target" ]]; then
    ok "HSTS: already current (1y, includeSubDomains, nosniff, no preload)"
else
    log "HSTS: updating (was: $hsts_current)"
    cf_api PATCH "/zones/${CLOUDFLARE_ZONE_ID}/settings/security_header" "$hsts_desired" >/dev/null
    ok "HSTS: enabled (1y, includeSubDomains, nosniff, no preload)"
fi

# ---------------------------------------------------------------------------
# Section 3: callback-host WAF custom rules
#   (a) BIC-off skip   -- the callback receiver takes service-to-service webhook
#       POSTs (WasabiCard/Runegate); Browser-Integrity-Check would bounce those
#       non-browser clients at the edge. Skip BIC + UA-block on the callback host.
#   (b) interim IP-lock -- block every request to the callback host whose source
#       IP is NOT in CALLBACK_ALLOW_IPS (provider egress ranges). Skipped, with a
#       loud note, while that list is the empty PLACEHOLDER.
# Declarative reconcile of the http_request_firewall_custom phase: order matters
# (skip first, then block), and a no-drift run is a true no-op.
# ---------------------------------------------------------------------------
echo
log "=== Section 3: callback-host WAF rules ==="

CALLBACK_BIC_SKIP_EXPR="$(printf 'http.host eq "%s"' "$CALLBACK_HOST")"
WAF_BIC_SKIP_AP='{"phases":["http_request_sbfm"],"products":["bic","uaBlock"]}'

# Build the desired rule array. Rule 1 is always the BIC-off skip. Rule 2 (the
# IP-lock) is appended ONLY when CALLBACK_ALLOW_IPS is non-empty -- an empty
# allowlist would otherwise block ALL callback traffic, which is worse than the
# interim gap it's meant to close.
DESIRED_WAF=$(jq -nc \
    --arg bic "$CALLBACK_BIC_SKIP_EXPR" \
    --argjson ap "$WAF_BIC_SKIP_AP" \
    '[{action:"skip", expression:$bic, description:"callback-host-bic-off (managed by deploy/scripts/perimeter/cloudflare-setup.sh)", enabled:true, action_parameters:$ap}]')

if [[ "${#CALLBACK_ALLOW_IPS[@]}" -gt 0 ]]; then
    # Compose: "ip.src in {a b c}" Cloudflare list literal.
    IP_LITERALS=""
    for cidr in "${CALLBACK_ALLOW_IPS[@]}"; do IP_LITERALS+="${cidr} "; done
    IP_LITERALS="${IP_LITERALS% }"
    CALLBACK_IPLOCK_EXPR="$(printf 'http.host eq "%s" and not (ip.src in {%s})' "$CALLBACK_HOST" "$IP_LITERALS")"
    DESIRED_WAF=$(jq -c --arg lock "$CALLBACK_IPLOCK_EXPR" \
        '. + [{action:"block", expression:$lock, description:"callback-host-ip-lock (managed by deploy/scripts/perimeter/cloudflare-setup.sh)", enabled:true}]' <<<"$DESIRED_WAF")
    ok "callback IP-lock: ${#CALLBACK_ALLOW_IPS[@]} provider CIDR(s) configured"
else
    warn "CALLBACK_ALLOW_IPS is the empty PLACEHOLDER -- the interim edge IP-lock on"
    warn "$CALLBACK_HOST is SKIPPED. Fill the provider (WasabiCard/Runegate) egress"
    warn "CIDRs in CALLBACK_ALLOW_IPS before relying on this perimeter at prod."
fi

waf_canon() {
    jq -S '[.[] | {action, expression, description, enabled: (.enabled // true), action_parameters: (.action_parameters // null)}]'
}
WAF_N=$(jq length <<<"$DESIRED_WAF")
DESIRED_WAF_CANON=$(waf_canon <<<"$DESIRED_WAF")

# Entrypoint ruleset is auto-provisioned on first custom rule; a never-touched
# zone returns success=false (404) here -- create in that case, else reconcile.
WAF_RS_RAW=$(curl -sS -X GET "${CF_API}/zones/${CLOUDFLARE_ZONE_ID}/rulesets/phases/http_request_firewall_custom/entrypoint" \
    -H "Authorization: Bearer ${CLOUDFLARE_API_TOKEN}")
if [[ "$(jq -r '.success' <<<"$WAF_RS_RAW")" != "true" ]]; then
    log "No custom ruleset on zone yet; creating it with ${WAF_N} rule(s)..."
    WAF_CREATE_BODY=$(jq -nc --argjson rules "$DESIRED_WAF" \
        '{name:"default", kind:"zone", phase:"http_request_firewall_custom", rules:$rules}')
    cf_api POST "/zones/${CLOUDFLARE_ZONE_ID}/rulesets" "$WAF_CREATE_BODY" >/dev/null
    ok "WAF custom rules: created ruleset with ${WAF_N} rule(s)"
else
    WAF_RULESET_ID=$(jq -r '.result.id' <<<"$WAF_RS_RAW")
    WAF_CURRENT_CANON=$(jq '[.result.rules[]?]' <<<"$WAF_RS_RAW" | waf_canon)
    if [[ "$WAF_CURRENT_CANON" == "$DESIRED_WAF_CANON" ]]; then
        ok "WAF custom rules: already current (${WAF_N} rule(s))"
    else
        log "WAF custom rules drift detected; reconciling ruleset ${WAF_RULESET_ID}..."
        cf_api PUT "/zones/${CLOUDFLARE_ZONE_ID}/rulesets/${WAF_RULESET_ID}" \
            "$(jq -nc --argjson rules "$DESIRED_WAF" '{rules:$rules}')" >/dev/null
        ok "WAF custom rules: reconciled to desired state (${WAF_N} rule(s))"
    fi
fi

# ---------------------------------------------------------------------------
# Summary
# ---------------------------------------------------------------------------
echo
ok "Cloudflare setup complete for $CLOUDFLARE_ZONE ($ENV, named mode)"
echo
echo "  Tunnel : $CLOUDFLARE_TUNNEL_NAME ($TUNNEL_ID)"
echo "  Origin : $TUNNEL_HOST"
echo "  Public routes ($N_ROUTES):"
while IFS=$'\t' read -r prefix port; do
    [[ -z "$prefix" ]] && continue
    echo "    https://$(site_hostname "$prefix")  ->  http://localhost:${port}"
done < <(jq -r '.public[] | "\(.hostPrefix)\t\(.port)"' "$SITES_JSON" | tr -d '\r')
echo
echo "  Zone hardening: Always Use HTTPS, Min TLS 1.2, TLS 1.3, Auto HTTPS"
echo "  Rewrites, SSL Full (strict), Security Level Medium, HSTS (1y +"
echo "  includeSubDomains, no preload)."
echo
echo "  Callback host ($CALLBACK_HOST):"
echo "    - Browser Integrity Check OFF (service-to-service webhook POSTs)"
if [[ "${#CALLBACK_ALLOW_IPS[@]}" -gt 0 ]]; then
    echo "    - Edge IP-lock: only ${#CALLBACK_ALLOW_IPS[@]} provider CIDR(s) allowed"
else
    echo "    - Edge IP-lock: SKIPPED (CALLBACK_ALLOW_IPS placeholder is empty)"
fi
echo
echo "  VM-side install (run separately):"
echo "    az vm run-command invoke --command-id RunPowerShellScript \\"
echo "      --scripts @deploy/scripts/perimeter/vm-install-cloudflared.ps1 \\"
echo "      --parameters \"KvName=${KEYVAULT_NAME}\" \"Env=${ENV}\""
