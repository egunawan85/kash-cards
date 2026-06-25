#!/usr/bin/env bash
# azure-vm-provision.sh -- script 1 of 2.
#
# Adapted from runegate-infra/scripts/provision/azure-vm-provision.sh.
#
# Provisions the Azure VM that hosts kash-cards (QryptoCard): a legacy
# ASP.NET Framework 4.6.2/4.7.2 + WCF + EF6 app, IIS-hosted, fronted by a
# Cloudflare tunnel (outbound-only -- no inbound 443 at the NSG).
#
# Reads parameters from config/.env.provision.${ENV:-dev} (copy the committed
# .env.provision.dev.example, fill the operator blanks: subscription id +
# RDP source IP). Idempotent: re-run safely after editing parameters;
# every resource is created only if missing (create-if-missing).
#
# Run from your laptop. Requires:
#   - Azure CLI logged in (az login)
#   - The subscription set in the .env.provision file (script switches to it)
#   - openssl (random admin password)
#
# After it succeeds the box is reachable for a ONE-TIME bootstrap: retrieve
# the auto-generated admin password (stored in Key Vault, printed below),
# flip the NSG RDP rule to Allow (or set RDP_DEFAULT_ACTION=Allow for the
# RDP_SOURCE), RDP in once, run vm-bootstrap.ps1 -- or run it without RDP via
# `az vm run-command invoke`. Then flip RDP back to Deny.
#
# DEV is a single disposable resource group (SHARED_RG == COMPUTE_RG): the
# whole RG is torn down wholesale after the shakeout. PROD splits compute from
# the permanent KV+LogAnalytics RG; this script honors whichever names the
# .env.provision file sets, so the same code drives both.

set -euo pipefail

# -- Locate self -------------------------------------------------------------
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"   # .../deploy/scripts/provision
DEPLOY_ROOT="$(cd "$SCRIPT_DIR/../.." && pwd)"                   # .../deploy
CONFIG_DIR="$DEPLOY_ROOT/config"
ENV="${ENV:-dev}"
ENV_FILE="$CONFIG_DIR/.env.provision.${ENV}"

# -- Logging helpers ---------------------------------------------------------
log()  { printf '[..] %s\n' "$*"; }
ok()   { printf '[ok] %s\n' "$*"; }
warn() { printf '[!!] %s\n' "$*" >&2; }
die()  { printf '[xx] %s\n' "$*" >&2; exit 1; }

# -- Preflight ---------------------------------------------------------------
[ -f "$ENV_FILE" ] || die "missing $ENV_FILE -- copy .env.provision.dev.example and edit"

command -v az      >/dev/null 2>&1 || die "az CLI not found on PATH"
command -v openssl >/dev/null 2>&1 || die "openssl not found on PATH"

# shellcheck disable=SC1090
. "$ENV_FILE"

: "${AZURE_SUBSCRIPTION_ID:?set in $ENV_FILE -- az account show --subscription \"\$AZURE_SUBSCRIPTION_NAME\" --query id -o tsv}"
: "${AZURE_LOCATION:?set in $ENV_FILE}"
: "${ENV:?set in $ENV_FILE}"
: "${SHARED_RG:?set in $ENV_FILE}"
: "${COMPUTE_RG:?set in $ENV_FILE}"
: "${VNET_NAME:?set in $ENV_FILE}"
: "${SUBNET_NAME:?set in $ENV_FILE}"
: "${NSG_NAME:?set in $ENV_FILE}"
: "${PIP_NAME:?set in $ENV_FILE}"
: "${NIC_NAME:?set in $ENV_FILE}"
: "${VM_NAME:?set in $ENV_FILE}"
: "${VM_SIZE:?set in $ENV_FILE}"
: "${VM_IMAGE:?set in $ENV_FILE}"
: "${VM_ADMIN_USERNAME:?set in $ENV_FILE}"
: "${RDP_SOURCE:?set in $ENV_FILE -- your public IP for the one-time bootstrap RDP}"
: "${LOG_ANALYTICS_NAME:?set in $ENV_FILE}"
: "${KEYVAULT_NAME:?set in $ENV_FILE}"

# RDP_DEFAULT_ACTION defaults to Deny (RDP closed; flip to Allow for a one-off
# GUI session, then flip back). ENABLE_PURGE_PROTECTION defaults to false (dev
# wants easy teardown; prod sets true).
RDP_DEFAULT_ACTION="${RDP_DEFAULT_ACTION:-Deny}"
ENABLE_PURGE_PROTECTION="${ENABLE_PURGE_PROTECTION:-false}"

case "$ENV" in dev|stg|prd|prod) ;; *) die "ENV must be dev, stg, prd, or prod, got: $ENV" ;; esac
case "$RDP_DEFAULT_ACTION" in Allow|Deny) ;; *) die "RDP_DEFAULT_ACTION must be Allow or Deny" ;; esac
case "$ENABLE_PURGE_PROTECTION" in true|false) ;; *) die "ENABLE_PURGE_PROTECTION must be true or false" ;; esac

# -- Subscription + signed-in identity ---------------------------------------
log "switching to subscription $AZURE_SUBSCRIPTION_ID"
az account set --subscription "$AZURE_SUBSCRIPTION_ID" >/dev/null
az account show --query '{id:id, name:name, user:user.name}' -o table

# -- Preflight: confirm signed-in user can write role assignments ------------
# Granting the VM identity "Key Vault Secrets User" needs
# Microsoft.Authorization/roleAssignments/write, which lives in Owner and User
# Access Administrator. Contributor is NOT enough. Fail fast with a pointer
# rather than partway through.
SIGNED_IN_OBJECT_ID=$(az ad signed-in-user show --query id -o tsv)
SIGNED_IN_USER_NAME=$(az account show --query user.name -o tsv)
PREFLIGHT_ROLES=$(az role assignment list \
  --assignee "$SIGNED_IN_OBJECT_ID" \
  --scope "/subscriptions/$AZURE_SUBSCRIPTION_ID" \
  --query "[?roleDefinitionName=='Owner' || roleDefinitionName=='User Access Administrator'].roleDefinitionName" \
  -o tsv 2>/dev/null | sort -u | tr '\n' ',' | sed 's/,$//' || true)
if [ -z "$PREFLIGHT_ROLES" ]; then
  printf '[xx] signed-in user lacks Owner / User Access Administrator on this subscription.\n\n' >&2
  printf '  user        : %s\n'   "$SIGNED_IN_USER_NAME" >&2
  printf '  object id   : %s\n'   "$SIGNED_IN_OBJECT_ID" >&2
  printf '  subscription: %s\n\n' "$AZURE_SUBSCRIPTION_ID" >&2
  printf 'The script needs Microsoft.Authorization/roleAssignments/write to grant\n' >&2
  printf 'the VM managed identity Key Vault Secrets User. Contributor is not enough.\n\n' >&2
  printf 'Fix (one-time, via portal): Subscriptions -> Access control (IAM) ->\n' >&2
  printf 'Add role assignment -> Owner -> assign to yourself -> wait ~1 min, re-run.\n' >&2
  exit 1
fi
ok "preflight: $SIGNED_IN_USER_NAME has $PREFLIGHT_ROLES on subscription"

# -- Resource provider registration ------------------------------------------
# Fresh subscriptions don't have every namespace registered. Idempotent:
# already-registered namespaces short-circuit.
ensure_provider() {
  ns="$1"
  state=$(az provider show --namespace "$ns" --query registrationState -o tsv 2>/dev/null || echo "NotFound")
  if [ "$state" = "Registered" ]; then
    ok "RP registered: $ns"
  else
    log "registering RP: $ns (state was $state; may take 1-2 min)"
    az provider register --namespace "$ns" --wait >/dev/null
    ok "registered RP: $ns"
  fi
}
ensure_provider Microsoft.KeyVault
ensure_provider Microsoft.Network
ensure_provider Microsoft.Compute
ensure_provider Microsoft.OperationalInsights
ensure_provider Microsoft.ManagedIdentity

# -- Resource group(s) -------------------------------------------------------
# Dev is a single disposable RG (SHARED_RG == COMPUTE_RG); ensure_rg is
# idempotent so creating the same name twice is a harmless no-op. Prod sets
# two distinct names and both get created.
ensure_rg() {
  rg="$1"
  if az group show --name "$rg" >/dev/null 2>&1; then
    ok "RG exists: $rg"
  else
    log "creating RG: $rg"
    az group create --name "$rg" --location "$AZURE_LOCATION" >/dev/null
    ok "created RG: $rg"
  fi
}
ensure_rg "$SHARED_RG"
ensure_rg "$COMPUTE_RG"

# -- Log Analytics workspace (shared RG) -------------------------------------
if az monitor log-analytics workspace show \
     --resource-group "$SHARED_RG" --workspace-name "$LOG_ANALYTICS_NAME" >/dev/null 2>&1; then
  ok "Log Analytics workspace exists: $LOG_ANALYTICS_NAME"
else
  log "creating Log Analytics workspace: $LOG_ANALYTICS_NAME (30d retention, explicit)"
  az monitor log-analytics workspace create \
    --resource-group "$SHARED_RG" \
    --workspace-name "$LOG_ANALYTICS_NAME" \
    --location "$AZURE_LOCATION" \
    --retention-time 30 >/dev/null
  ok "created Log Analytics workspace: $LOG_ANALYTICS_NAME"
fi
LAW_ID=$(az monitor log-analytics workspace show \
  --resource-group "$SHARED_RG" --workspace-name "$LOG_ANALYTICS_NAME" \
  --query id -o tsv)

# -- Key Vault (shared RG) ---------------------------------------------------
# RBAC auth mode (no access policies); soft-delete is on by default and cannot
# be disabled; purge-protection per ENABLE_PURGE_PROTECTION (dev: false for
# easy teardown; prod: true). KV names are globally unique 3-24 chars -- if
# KEYVAULT_NAME is taken, bump the suffix in the .env.provision file.
if az keyvault show --name "$KEYVAULT_NAME" --resource-group "$SHARED_RG" >/dev/null 2>&1; then
  ok "Key Vault exists: $KEYVAULT_NAME"
else
  log "creating Key Vault: $KEYVAULT_NAME (RBAC mode, 90d retention, purge-protection=$ENABLE_PURGE_PROTECTION)"
  KV_CREATE_ARGS="--name $KEYVAULT_NAME --resource-group $SHARED_RG --location $AZURE_LOCATION --enable-rbac-authorization true --retention-days 90"
  if [ "$ENABLE_PURGE_PROTECTION" = "true" ]; then
    KV_CREATE_ARGS="$KV_CREATE_ARGS --enable-purge-protection true"
  fi
  # shellcheck disable=SC2086
  az keyvault create $KV_CREATE_ARGS >/dev/null
  ok "created Key Vault: $KEYVAULT_NAME"
fi
KV_ID=$(az keyvault show --name "$KEYVAULT_NAME" --resource-group "$SHARED_RG" --query id -o tsv)

# Diagnostic logs: KV -> Log Analytics. Always-PUT (idempotent ARM PUT), so
# re-running with a changed --logs list overwrites cleanly.
log "wiring KV diagnostic logs -> Log Analytics (kv-to-law)"
az monitor diagnostic-settings create \
  --name "kv-to-law" \
  --resource "$KV_ID" \
  --workspace "$LAW_ID" \
  --logs    '[{"category":"AuditEvent","enabled":true}]' \
  --metrics '[{"category":"AllMetrics","enabled":true}]' >/dev/null
ok "kv-to-law diagnostic-setting applied"

# -- Grant the running user KV Secrets Officer (to write the admin password) --
if az role assignment list \
     --assignee "$SIGNED_IN_OBJECT_ID" --scope "$KV_ID" \
     --role "Key Vault Secrets Officer" --query '[0].id' -o tsv | grep -q .; then
  ok "current user has KV Secrets Officer on $KEYVAULT_NAME"
else
  log "granting current user KV Secrets Officer on $KEYVAULT_NAME"
  az role assignment create \
    --assignee-object-id "$SIGNED_IN_OBJECT_ID" \
    --assignee-principal-type User \
    --role "Key Vault Secrets Officer" \
    --scope "$KV_ID" >/dev/null
  ok "granted KV Secrets Officer to current user"
  log "waiting 30s for RBAC propagation"
  sleep 30
fi

# -- VNet + subnet -----------------------------------------------------------
if az network vnet show --resource-group "$COMPUTE_RG" --name "$VNET_NAME" >/dev/null 2>&1; then
  ok "VNet exists: $VNET_NAME"
else
  log "creating VNet $VNET_NAME with subnet $SUBNET_NAME"
  az network vnet create \
    --resource-group "$COMPUTE_RG" \
    --name "$VNET_NAME" \
    --location "$AZURE_LOCATION" \
    --address-prefixes 10.30.0.0/16 \
    --subnet-name "$SUBNET_NAME" \
    --subnet-prefixes 10.30.1.0/24 >/dev/null
  ok "created VNet + subnet"
fi

# -- NSG with default-deny + explicit allow/deny rules -----------------------
# The platform default rules already deny inbound from Internet (priority
# 65500). On top of that:
#   - RDP (3389) allowed ONLY from RDP_SOURCE, governed by RDP_DEFAULT_ACTION
#     (default Deny -- flip to Allow for the one-off bootstrap, then back).
#   - HTTPS (443) EXPLICITLY denied from Internet: the Cloudflare tunnel is
#     outbound-only, so the box never needs an inbound origin port. The
#     explicit deny documents intent and survives any future loosening of the
#     platform defaults.
if az network nsg show --resource-group "$COMPUTE_RG" --name "$NSG_NAME" >/dev/null 2>&1; then
  ok "NSG exists: $NSG_NAME"
else
  log "creating NSG: $NSG_NAME"
  az network nsg create \
    --resource-group "$COMPUTE_RG" \
    --name "$NSG_NAME" \
    --location "$AZURE_LOCATION" >/dev/null
  ok "created NSG"
fi

ensure_nsg_rule() {
  name="$1"; priority="$2"; port="$3"; source="$4"; access="$5"; desc="$6"
  if az network nsg rule show \
       --resource-group "$COMPUTE_RG" --nsg-name "$NSG_NAME" --name "$name" >/dev/null 2>&1; then
    log "updating NSG rule: $name"
    az network nsg rule update \
      --resource-group "$COMPUTE_RG" --nsg-name "$NSG_NAME" --name "$name" \
      --priority "$priority" --access "$access" --protocol Tcp --direction Inbound \
      --source-address-prefixes "$source" --destination-address-prefixes '*' \
      --destination-port-ranges "$port" --description "$desc" >/dev/null
  else
    log "creating NSG rule: $name"
    az network nsg rule create \
      --resource-group "$COMPUTE_RG" --nsg-name "$NSG_NAME" --name "$name" \
      --priority "$priority" --access "$access" --protocol Tcp --direction Inbound \
      --source-address-prefixes "$source" --destination-address-prefixes '*' \
      --destination-port-ranges "$port" --description "$desc" >/dev/null
  fi
  ok "NSG rule: $name (priority=$priority, $access, source=$source, port=$port)"
}

ensure_nsg_rule "Allow-RDP"  110 3389 "$RDP_SOURCE" "$RDP_DEFAULT_ACTION" "RDP -- default Deny; flip to Allow for the one-off bootstrap GUI session, then flip back"
ensure_nsg_rule "Deny-HTTPS" 200 443  "Internet"    Deny                  "Inbound 443 denied; cloudflared tunnel is outbound-only, no inbound origin port needed"

# Attach NSG to subnet (belt-and-braces with NIC-level NSG below).
SUBNET_NSG=$(az network vnet subnet show \
  --resource-group "$COMPUTE_RG" --vnet-name "$VNET_NAME" --name "$SUBNET_NAME" \
  --query networkSecurityGroup.id -o tsv 2>/dev/null || true)
if [ -z "$SUBNET_NSG" ]; then
  log "attaching NSG to subnet"
  az network vnet subnet update \
    --resource-group "$COMPUTE_RG" --vnet-name "$VNET_NAME" --name "$SUBNET_NAME" \
    --network-security-group "$NSG_NAME" >/dev/null
  ok "NSG attached to subnet"
else
  ok "NSG already attached to subnet"
fi

# -- Public IP ---------------------------------------------------------------
if az network public-ip show --resource-group "$COMPUTE_RG" --name "$PIP_NAME" >/dev/null 2>&1; then
  ok "Public IP exists: $PIP_NAME"
else
  log "creating Public IP: $PIP_NAME (Standard SKU, static)"
  az network public-ip create \
    --resource-group "$COMPUTE_RG" \
    --name "$PIP_NAME" \
    --location "$AZURE_LOCATION" \
    --sku Standard \
    --allocation-method Static \
    --version IPv4 >/dev/null
  ok "created Public IP"
fi
PIP_ADDR=$(az network public-ip show \
  --resource-group "$COMPUTE_RG" --name "$PIP_NAME" --query ipAddress -o tsv)

# -- NIC ---------------------------------------------------------------------
if az network nic show --resource-group "$COMPUTE_RG" --name "$NIC_NAME" >/dev/null 2>&1; then
  ok "NIC exists: $NIC_NAME"
else
  log "creating NIC: $NIC_NAME"
  az network nic create \
    --resource-group "$COMPUTE_RG" \
    --name "$NIC_NAME" \
    --location "$AZURE_LOCATION" \
    --vnet-name "$VNET_NAME" \
    --subnet "$SUBNET_NAME" \
    --public-ip-address "$PIP_NAME" \
    --network-security-group "$NSG_NAME" >/dev/null
  ok "created NIC"
fi

# -- Admin password (generate once, store in KV) -----------------------------
ADMIN_PWD_SECRET="vm-admin-password"
if az keyvault secret show --vault-name "$KEYVAULT_NAME" --name "$ADMIN_PWD_SECRET" >/dev/null 2>&1; then
  ok "admin password already in KV (secret: $ADMIN_PWD_SECRET)"
  ADMIN_PWD=$(az keyvault secret show --vault-name "$KEYVAULT_NAME" --name "$ADMIN_PWD_SECRET" --query value -o tsv)
else
  log "generating admin password and storing in KV"
  # Windows complexity: >=1 upper, >=1 lower, >=1 digit, >=1 special, 12+ chars.
  ADMIN_PWD="$(openssl rand -base64 24 | tr -d '/=+' | head -c 20)Aa1!"
  az keyvault secret set \
    --vault-name "$KEYVAULT_NAME" --name "$ADMIN_PWD_SECRET" --value "$ADMIN_PWD" >/dev/null
  ok "admin password stored in KV (secret: $ADMIN_PWD_SECRET)"
fi

# -- VM ----------------------------------------------------------------------
# System-assigned managed identity so vm-bootstrap.ps1 can pull secrets from
# KV without a stored credential. --nics uses the pre-built NIC (PIP + NSG
# already attached); the CLI does not auto-create networking when --nics is
# passed. Patch mode Manual: the operator runs Windows Update during planned
# windows rather than the agent installing on its own schedule.
if az vm show --resource-group "$COMPUTE_RG" --name "$VM_NAME" >/dev/null 2>&1; then
  ok "VM exists: $VM_NAME"
else
  log "creating VM: $VM_NAME ($VM_SIZE, $VM_IMAGE)"
  az vm create \
    --resource-group "$COMPUTE_RG" \
    --name "$VM_NAME" \
    --location "$AZURE_LOCATION" \
    --size "$VM_SIZE" \
    --image "$VM_IMAGE" \
    --admin-username "$VM_ADMIN_USERNAME" \
    --admin-password "$ADMIN_PWD" \
    --nics "$NIC_NAME" \
    --assign-identity '[system]' \
    --os-disk-name "osdisk-kash-${ENV}" \
    --storage-sku Premium_LRS \
    --patch-mode Manual \
    --enable-auto-update false >/dev/null
  ok "created VM"
fi

VM_PRINCIPAL_ID=$(az vm identity show \
  --resource-group "$COMPUTE_RG" --name "$VM_NAME" --query principalId -o tsv)

# -- Grant the VM managed identity Key Vault Secrets User on the vault --------
if az role assignment list \
     --assignee "$VM_PRINCIPAL_ID" --scope "$KV_ID" \
     --role "Key Vault Secrets User" --query '[0].id' -o tsv | grep -q .; then
  ok "VM MI has Key Vault Secrets User on $KEYVAULT_NAME"
else
  log "granting VM MI Key Vault Secrets User on $KEYVAULT_NAME"
  az role assignment create \
    --assignee-object-id "$VM_PRINCIPAL_ID" \
    --assignee-principal-type ServicePrincipal \
    --role "Key Vault Secrets User" \
    --scope "$KV_ID" >/dev/null
  ok "granted Key Vault Secrets User to VM MI"
fi

# -- Summary -----------------------------------------------------------------
cat <<EOF

===========================================================================
  Provisioning complete.
===========================================================================

  Subscription   $AZURE_SUBSCRIPTION_ID
  Region         $AZURE_LOCATION
  Environment    $ENV

  Shared RG      $SHARED_RG
    Key Vault    $KEYVAULT_NAME (RBAC, soft-delete on, purge-protection=$ENABLE_PURGE_PROTECTION)
    Log Analytics $LOG_ANALYTICS_NAME

  Compute RG     $COMPUTE_RG
    VM           $VM_NAME ($VM_SIZE, system-assigned managed identity)
    Public IP    $PIP_ADDR
    NSG          $NSG_NAME
                 - RDP   (3389)  $RDP_SOURCE   $RDP_DEFAULT_ACTION
                 - HTTPS (443)   Internet      Deny  (cloudflared is outbound-only)

  Admin password stored in Key Vault as secret '$ADMIN_PWD_SECRET'.
  Retrieve it with:
    az keyvault secret show --vault-name $KEYVAULT_NAME \\
      --name $ADMIN_PWD_SECRET --query value -o tsv

===========================================================================
  Next steps -- run vm-bootstrap.ps1 on the VM
===========================================================================

  Option A (no RDP -- via Azure's management plane):

       az vm run-command invoke \\
         --resource-group $COMPUTE_RG --name $VM_NAME \\
         --command-id RunPowerShellScript \\
         --scripts @vm-bootstrap.ps1

  Option B (RDP once for the GUI bootstrap):

       # open RDP from your IP, retrieve the password, connect:
       az network nsg rule update -g $COMPUTE_RG \\
         --nsg-name $NSG_NAME --name Allow-RDP --access Allow
       mstsc /v:$PIP_ADDR        # log in as $VM_ADMIN_USERNAME
       # ... paste vm-bootstrap.ps1 into an elevated PowerShell, then flip back:
       az network nsg rule update -g $COMPUTE_RG \\
         --nsg-name $NSG_NAME --name Allow-RDP --access Deny

  vm-bootstrap.ps1 installs IIS + ASP.NET 4.x + WCF HTTP Activation + URL
  Rewrite, the .NET FX 4.6.2 AND 4.7.2 targeting packs + VS Build Tools (so
  the 12 projects build on-box), and SQL Server Express (loopback 127.0.0.1:1433,
  mixed-mode) with the least-privilege $DB_APP_LOGIN login on the app DB.

===========================================================================
EOF
