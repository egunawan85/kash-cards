# inject-secrets.ps1 -- inject app secrets as per-app-pool ENVIRONMENT VARIABLES
# into applicationHost.config, sourced from Azure Key Vault.
#
# Adapted from runegate-infra/scripts/deploy/vm-deploy-apps.ps1 (which staged KV
# secrets to per-pool env and recycled). Re-shaped for kash-cards' 12-pool
# layout (sites.json) and its env-var contract: the app reads every secret from
# the PROCESS environment via QryptoCard.Sec.SecretsConfig -- never from source,
# never from a .vault file on the server. So the deploy-time job is to land each
# secret as an <environmentVariable> under the pool's <processModel> in
# applicationHost.config, where IIS injects it into the w3wp process env.
#
# Secrets handled (names only; values come from Key Vault, never source):
#   KASH_DATA_KEY, WASABICARD_API_KEY, WASABICARD_PUBLIC_KEY,
#   WASABICARD_PRIVATE_KEY, WASABICARD_PRIVATE_KEY_XML, WASABICARD_WSBPUBLIC_KEY,
#   PGCRYPTO_API_KEY, PGCRYPTO_SECRET_KEY, PGCRYPTO_WEBHOOK_SECRET,
#   POSTMARK_SERVER_TOKEN, EMAIL_PASSWORD, INT_CALLBACK_SHARED_SECRET,
#   AUTH_SERVICE_REVOKE_TOKEN.
# (DB_PASSWORD is NOT here -- it rides in the EF connection string in Web.config,
#  injected by deploy-iis.ps1, not as an env var.)
#
# DEV policy (this script's default): inject the FULL secret set into EVERY app
# pool. Simplest, and guarantees no missing-secret startup fault from a pool
# that turns out to need a secret we didn't scope to it. SecretsConfig.Preload
# fails an app at Application_Start if any required name is unset, so over-
# provisioning on the disposable dev box trades a little blast radius for zero
# missing-secret faults.
#
# !!! PROD MUST MINIMIZE PER POOL. !!!
#   On prod, do NOT spray every secret to every pool. Scope each secret to only
#   the pools that actually read it -- e.g. INT_CALLBACK_SHARED_SECRET and the
#   callback/webhook secrets (PGCRYPTO_WEBHOOK_SECRET, WASABICARD_WSBPUBLIC_KEY)
#   to the callback pools only; POSTMARK/EMAIL to the tiers that send mail;
#   AUTH_SERVICE_REVOKE_TOKEN to the auth-consuming tier. A leaked w3wp on the
#   public docs pool should not hand an attacker the money-tier callback secret.
#   See the $poolSecretPolicy hook below: replace the dev "all -> every pool"
#   map with an explicit per-pool allow-list. The mechanism (KV pull + write to
#   applicationHost.config + ACL) is identical; only the policy map changes.
#
# Idempotent: each env var is set to its current KV value every run (create or
# update in place); re-running converges. Never prints secret values.
#
# Usage (run as Administrator, or as SYSTEM via 'az vm run-command invoke'):
#   pwsh -File inject-secrets.ps1 [-Env dev] [-VaultName <kv>]
# VaultName resolves from -VaultName, else KEYVAULT_NAME in
# config/.env.provision.<env>.

[CmdletBinding()]
param(
    [ValidateSet('dev', 'stg', 'prd')]
    [string]$Env = 'dev',

    # Key Vault name. The VM's managed identity has Key Vault Secrets User
    # (data plane) and pulls each secret by name. If omitted, read from the
    # env file's KEYVAULT_NAME.
    [string]$VaultName,

    [string]$RepoRoot,

    # Optional per-tier targeting. When set, inject + recycle ONLY this one pool
    # (the per-tier redeploy path driven by deploy/deploy.sh) instead of all 12.
    # The alias is the app pool minus the 'kash-' prefix (e.g. 'int' -> kash-int);
    # the full pool name or the project name are also accepted.
    [string]$Service
)

$ErrorActionPreference = 'Stop'
$ProgressPreference    = 'SilentlyContinue'

# UTF-8 for native stdout. Secret values can carry non-cp1252 bytes (base64,
# XML keys); the default console codepage would silently corrupt them on
# capture. Mirrors the sister.
[Console]::OutputEncoding = [System.Text.Encoding]::UTF8
$env:PYTHONIOENCODING     = 'utf-8'

# -- Logging helpers (match the sister) --------------------------------------
function Write-Step { param([string]$m) Write-Host "[..] $m" }
function Write-Ok   { param([string]$m) Write-Host "[ok] $m" }
function Write-Warn { param([string]$m) Write-Host "[!!] $m" -ForegroundColor Yellow }
function Stop-Inject {
    param([string]$m)
    Write-Host "[xx] $m" -ForegroundColor Red
    exit 1
}

# -- Resolve paths -----------------------------------------------------------
if (-not $RepoRoot) {
    # From the repo, repo root is three dirs up. When sent detached via
    # `az vm run-command` ($PSScriptRoot is a temp dir), fall back to the fixed clone.
    $candidate = Join-Path $PSScriptRoot '..\..\..'
    if (Test-Path (Join-Path $candidate 'deploy\config\sites.json')) {
        $RepoRoot = (Resolve-Path $candidate).Path
    } else {
        $RepoRoot = 'C:\src\kash-cards'
    }
}
$ConfigDir = Join-Path $RepoRoot 'deploy\config'
$SitesJson = Join-Path $ConfigDir 'sites.json'
$EnvFile   = Join-Path $ConfigDir ".env.provision.$Env"
if (-not (Test-Path $SitesJson)) { Stop-Inject "missing $SitesJson" }

# -- Preflight: admin (applicationHost.config writes need it). ---------------
$identity  = [Security.Principal.WindowsIdentity]::GetCurrent()
$principal = [Security.Principal.WindowsPrincipal]::new($identity)
if (-not $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) {
    Stop-Inject "must run as Administrator (or as SYSTEM via 'az vm run-command invoke')"
}
Write-Ok "running as: $($identity.Name)"

try {
    Import-Module WebAdministration -ErrorAction Stop
} catch {
    Stop-Inject "WebAdministration module not available -- is the IIS management feature installed? ($($_.Exception.Message))"
}

# -- Resolve Key Vault name --------------------------------------------------
function Read-EnvFile {
    param([string]$Path)
    $h = @{}
    if (-not (Test-Path $Path)) { return $h }
    foreach ($line in Get-Content -LiteralPath $Path) {
        $t = $line.Trim()
        if ($t -eq '' -or $t.StartsWith('#')) { continue }
        $idx = $t.IndexOf('=')
        if ($idx -lt 1) { continue }
        $k = $t.Substring(0, $idx).Trim()
        $v = $t.Substring($idx + 1).Trim().Trim('"')
        if ($v -match '^([^"#]*\S)\s+#') { $v = $matches[1] }
        $h[$k] = $v
    }
    return $h
}

if (-not $VaultName) {
    $envCfg   = Read-EnvFile $EnvFile
    $VaultName = $envCfg['KEYVAULT_NAME']
}
if (-not $VaultName) {
    Stop-Inject "no -VaultName and KEYVAULT_NAME not set in $EnvFile"
}
Write-Ok "Key Vault: $VaultName"

# -- az CLI + login --identity -----------------------------------------------
$az = (Get-Command az -ErrorAction SilentlyContinue).Source
if (-not $az) {
    $cand = 'C:\Program Files\Microsoft SDKs\Azure\CLI2\wbin\az.cmd'
    if (Test-Path $cand) { $az = $cand }
}
if (-not $az) { Stop-Inject "az CLI not found on PATH -- needed to pull secrets from Key Vault" }

Write-Step "az login --identity (VM managed identity)"
& $az login --identity --output none 2>$null
if ($LASTEXITCODE -ne 0) { Stop-Inject "az login --identity failed (exit $LASTEXITCODE)" }
Write-Ok "logged in as VM managed identity"

# -- The full app secret set the app reads via SecretsConfig (names only). ---
# These are the env-var names the app reads via SecretsConfig. KV secret names
# CANNOT contain '_', so seed-kv-secrets.sh stores them with '_'->'-' (e.g.
# DB_PASSWORD -> DB-PASSWORD). Get-Secrets reverses that for the KV lookup and
# writes the env var back under the original underscored name listed here.
$AllSecretNames = @(
    'KASH_DATA_KEY',
    'WASABICARD_API_KEY',
    'WASABICARD_PUBLIC_KEY',
    'WASABICARD_PRIVATE_KEY',
    'WASABICARD_PRIVATE_KEY_XML',
    'WASABICARD_WSBPUBLIC_KEY',
    'PGCRYPTO_API_KEY',
    'PGCRYPTO_SECRET_KEY',
    'PGCRYPTO_WEBHOOK_SECRET',
    'POSTMARK_SERVER_TOKEN',
    'EMAIL_PASSWORD',
    'INT_CALLBACK_SHARED_SECRET',
    'AUTH_SERVICE_REVOKE_TOKEN',
    'SCHEDULER_SHARED_SECRET'
)

# Non-secret runtime config the app reads via SecretsConfig.GetOptional. On the
# server these don't come from a file (load-env.ps1 is local-only), so inject them as
# pool env vars too -- otherwise tiers fall back to code defaults (e.g. the callback
# tier would use the PROD WasabiCard URL instead of the dev sandbox). Seeded into KV
# from secrets/.env by seed-kv-secrets.sh, under the same '_' -> '-' name mapping.
$ConfigNames = @(
    'QRYPTO_ENVIRONMENT',
    'WASABICARD_API_URL',
    'PGCRYPTO_API_URL',
    'PUBLIC_BASE_URL',
    'ADMIN_BASE_URL',
    'EMAIL_FROM',
    'EMAIL_SMTP_GATEWAY',
    'EMAIL_SMTP_PORT'
)

# OPTIONAL per-env config: injected when present in this env's Key Vault, SKIPPED when absent
# (the app falls back to its own safe defaults via SecretsConfig.GetOptional). This is how the
# WasabiCard auto-funding feature is turned on/configured per environment from the deploy --
# e.g. set WASABICARD_AUTOFUND_ENABLED=1 in the prd vault, leave it unset in dev (stays OFF).
# Adding a name here NEVER forces every env to seed it.
$OptionalConfigNames = @(
    'WASABICARD_AUTOFUND_ENABLED',
    'WASABICARD_FLOOR_USD',
    'WASABICARD_TARGET_USD',
    'WASABICARD_EAGER_THRESHOLD_USD',
    'WASABICARD_DAILY_CAP_USD',
    'WASABICARD_MIN_TRANSFER_USD',
    'WASABICARD_WC_FEE_PCT',
    'WASABICARD_INFLIGHT_STALE_MIN',
    'WASABICARD_REALERT_HOURS',
    'WASABICARD_DEPOSIT_ADDRESS',
    'OPS_ALERT_EMAIL',
    # Card pricing: customer CardPrice = WasabiCard wholesale + CARD_PRICE_MARKUP% (rounded up),
    # or a flat CARD_PRICE_GLOBAL for all cards. Unset => 0% markup (sell at wholesale, break-even).
    # Per-card overrides (CARD_PRICE_MARKUP_<cardTypeId>) are read by the app too; add the specific
    # name here if you want to seed one from KV for a given card.
    'CARD_PRICE_MARKUP',
    'CARD_PRICE_GLOBAL'
)

# -- Pull each secret from KV once (cache in a local map). Never log values. --
# Pulling once and reusing across pools avoids N*pools KV round-trips and keeps
# the value lifetime as short and as in-memory as possible.
function Get-Secrets {
    # -Optional: a name absent/empty in KV is SKIPPED (not fatal). Used for optional per-env
    # config (e.g. the WasabiCard auto-fund knobs) so an environment that doesn't set them still
    # deploys; the app falls back to its own defaults via SecretsConfig.GetOptional.
    param([string[]]$Names, [switch]$Optional)
    $map = @{}
    # Windows PowerShell 5.1 defaults to TLS 1.0/1.1 for Invoke-RestMethod; Key Vault requires
    # TLS 1.2, so without this the KV HTTPS handshake hangs (IMDS is plain HTTP and is unaffected).
    [Net.ServicePointManager]::SecurityProtocol = [Net.ServicePointManager]::SecurityProtocol -bor [Net.SecurityProtocolType]::Tls12

    # Auth ONCE: get a managed-identity access token for Key Vault from IMDS, then reuse it for
    # every secret via direct REST GETs in THIS session. The old path ran `az keyvault secret show`
    # per secret -- each a fresh az process that re-authenticated MSI (~several seconds each), so a
    # full set cost minutes. One token + N cheap REST calls cuts that to seconds. Same MSI identity,
    # same KV endpoint, same in-memory-only value handling (never logged); only the transport changed.
    $imds = 'http://169.254.169.254/metadata/identity/oauth2/token?api-version=2018-02-01&resource=https%3A%2F%2Fvault.azure.net'
    try {
        $token = (Invoke-RestMethod -Method Get -Uri $imds -Headers @{ Metadata = 'true' } -TimeoutSec 30).access_token
    } catch {
        Stop-Inject "failed to obtain a managed-identity token for Key Vault (IMDS): $($_.Exception.Message)"
    }
    if ([string]::IsNullOrWhiteSpace($token)) { Stop-Inject 'managed-identity token for Key Vault came back empty' }
    $authHeader = @{ Authorization = "Bearer $token" }

    foreach ($name in ($Names | Select-Object -Unique)) {
        # KV secret names can't contain '_'; seed-kv-secrets.sh stored them dashed.
        $kvName = $name -replace '_', '-'
        Write-Step "pulling secret '$kvName' from $VaultName"
        $uri = "https://$VaultName.vault.azure.net/secrets/$kvName" + '?api-version=7.4'
        try {
            $val = (Invoke-RestMethod -Method Get -Uri $uri -Headers $authHeader -TimeoutSec 30).value
        } catch {
            if ($Optional) { Write-Ok "  '$name' not set in $VaultName (optional) -- skipping"; continue }
            Stop-Inject "failed to read '$kvName' from $VaultName : $($_.Exception.Message)"
        }
        # Do NOT trim interior/edge whitespace beyond a stray trailing newline -- XML keys and
        # base64 values are whitespace-significant; KV returns the value verbatim.
        if ([string]::IsNullOrWhiteSpace($val)) {
            if ($Optional) { Write-Ok "  '$name' blank in $VaultName (optional) -- skipping"; continue }
            Stop-Inject "secret '$name' is empty in $VaultName -- seed it before injecting"
        }
        $map[$name] = $val
        Write-Ok "  '$name' pulled (length hidden)"
    }
    return $map
}

# ===========================================================================
# Per-pool secret policy.
#
# DEV (default below): every pool gets the FULL set. Simplest; no missing-secret
# startup faults. PROD: replace this with an explicit per-pool allow-list -- see
# the header's "PROD MUST MINIMIZE PER POOL" note. To do that, change this
# function to return only the names a given pool needs (keyed on $PoolName),
# e.g. callback-only secrets to the callback pools.
# ===========================================================================
function Get-SecretsForPool {
    param([string]$PoolName)
    # DEV policy: full set to every pool.
    return $AllSecretNames
    # PROD example (replace the line above):
    #   switch -Wildcard ($PoolName) {
    #       '*callback*' { return @('INT_CALLBACK_SHARED_SECRET','PGCRYPTO_WEBHOOK_SECRET','WASABICARD_WSBPUBLIC_KEY', ...) }
    #       '*scheduler*'{ return @('PGCRYPTO_API_KEY','PGCRYPTO_SECRET_KEY', ...) }
    #       default      { return @('KASH_DATA_KEY', ...) }
    #   }
}

# -- ACL applicationHost.config to Administrators + SYSTEM only. --------------
# It now holds secret values inline; lock it down. Idempotent (re-applying the
# same ACL is a no-op-equivalent).
function Protect-ApplicationHostConfig {
    $path = Join-Path $env:windir 'System32\inetsrv\config\applicationHost.config'
    if (-not (Test-Path $path)) {
        Write-Warn "applicationHost.config not found at $path -- skipping ACL hardening"
        return
    }
    Write-Step "ACL $path -> Administrators + SYSTEM only"
    & icacls.exe $path /inheritance:r | Out-Null
    & icacls.exe $path /grant:r 'BUILTIN\Administrators:F' 'NT AUTHORITY\SYSTEM:F' | Out-Null
    if ($LASTEXITCODE -ne 0) { Stop-Inject "icacls hardening of applicationHost.config failed (exit $LASTEXITCODE)" }
    Write-Ok "applicationHost.config ACL: Administrators + SYSTEM (F), inheritance removed"
}

# ===========================================================================
# Main.
# ===========================================================================
$sites    = Get-Content -LiteralPath $SitesJson -Raw | ConvertFrom-Json
$allPools = @($sites.public + $sites.internal | ForEach-Object { $_.appPool }) | Select-Object -Unique

# -- Optional per-tier targeting (the per-tier redeploy path). When -Service is
# set, narrow $allPools to the single matching pool so only that tier is
# re-injected and recycled; the other 11 pools keep their existing env untouched.
# Match on the 'kash-'-stripped alias, the full pool name, or the project name;
# unknown -> fail with the valid list.
if ($Service) {
    $byProject = @($sites.public + $sites.internal | Where-Object { $_.project -eq $Service } | ForEach-Object { $_.appPool })
    $match = @($allPools | Where-Object { $_ -eq "kash-$Service" -or $_ -eq $Service -or $byProject -contains $_ })
    if ($match.Count -eq 0) {
        $aliases = (($allPools | ForEach-Object { $_ -replace '^kash-', '' }) | Sort-Object) -join ', '
        Stop-Inject "unknown -Service '$Service' -- valid aliases: $aliases"
    }
    $allPools = $match
    Write-Ok "per-tier target pool: $($allPools -join ', ')"
}
Write-Ok "target app pools: $($allPools.Count) -- $($allPools -join ', ')"

# Pull every secret + config value once up front (fail fast if any is missing/empty).
$secretValues = Get-Secrets -Names ($AllSecretNames + $ConfigNames)

# Pull OPTIONAL config (skips any not set in this env's vault) and merge in. The names actually
# found drive which optional vars get injected below.
$optionalValues = Get-Secrets -Names $OptionalConfigNames -Optional
foreach ($k in $optionalValues.Keys) { $secretValues[$k] = $optionalValues[$k] }
$presentOptional = @($OptionalConfigNames | Where-Object { $secretValues.ContainsKey($_) })

# Write every pool's env vars in ONE applicationHost.config commit. The WebConfiguration cmdlets
# auto-commit on EACH Add (rewriting + reloading the whole config), so ~62 of them cost minutes.
# The ServerManager API stages all edits in memory and CommitChanges() writes once -- same end
# state (per-pool <environmentVariables> entries), seconds instead of minutes.
Add-Type -Path (Join-Path $env:windir 'System32\inetsrv\Microsoft.Web.Administration.dll')
$mgr = New-Object Microsoft.Web.Administration.ServerManager
try {
    $poolsColl = $mgr.GetApplicationHostConfiguration().GetSection('system.applicationHost/applicationPools').GetCollection()
    foreach ($pool in $allPools) {
        $poolEl = $poolsColl | Where-Object { $_.GetAttributeValue('name') -eq $pool } | Select-Object -First 1
        if (-not $poolEl) { Stop-Inject "app pool '$pool' not found in applicationHost.config -- run deploy-iis.ps1 first (it creates the pools)" }
        $envColl = $poolEl.GetCollection('environmentVariables')
        $names = @(Get-SecretsForPool -PoolName $pool) + $ConfigNames + $presentOptional
        Write-Host ''
        Write-Host "=== $pool : injecting $($names.Count) var(s) ==="
        foreach ($name in $names) {
            # name-keyed collection: remove any existing same-named entry, then add (mirrors the
            # old Get/Remove/Add-WebConfigurationProperty, but staged -- no commit until the end).
            $existing = $envColl | Where-Object { $_.GetAttributeValue('name') -eq $name } | Select-Object -First 1
            if ($existing) { [void]$envColl.Remove($existing) }
            $e = $envColl.CreateElement('add')
            $e['name']  = [string]$name
            # Coerce to [string]: the Microsoft.Web.Administration element setter is COM-backed and
            # throws DISP_E_TYPEMISMATCH if handed anything but a string (hit in prod when the merged
            # optional-config values reached this assignment as a non-[string]).
            $e['value'] = [string]$secretValues[$name]
            [void]$envColl.Add($e)
            Write-Ok "  $pool <- $name (value hidden)"
        }
    }
    $mgr.CommitChanges()   # single write for ALL pools
    Write-Ok "applicationHost.config committed (all pools in one write)"
} finally {
    $mgr.Dispose()
}

# Lock down the config that now carries the values.
Protect-ApplicationHostConfig

# Recycle pools so any already-started worker (AlwaysRunning) drops its stale/empty
# environment and picks up the injected vars; otherwise verify would probe an app
# that faulted on missing secrets.
foreach ($pool in $allPools) {
    if (Test-Path "IIS:\AppPools\$pool") { Restart-WebAppPool -Name $pool -ErrorAction SilentlyContinue }
}

Write-Host ''
Write-Host '==========================================================================='
Write-Host "  Secret injection complete: $env:COMPUTERNAME"
Write-Host '==========================================================================='
Write-Host "  Pools:    $($allPools.Count)"
Write-Host "  Secrets:  $($AllSecretNames.Count) per pool (DEV: full set to every pool)"
Write-Host "  Key Vault: $VaultName"
Write-Host ''
Write-Host '  Pools recycled so w3wp picks up the new env.'
Write-Host '  PROD reminder: minimize per pool -- see header + Get-SecretsForPool.'
Write-Host ''
Write-Host "INJECT_RESULT: PASS ($($allPools.Count) pools)"
