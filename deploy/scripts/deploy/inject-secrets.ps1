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
#   DBKEY, APPKEY, WASABICARD_API_KEY, WASABICARD_PUBLIC_KEY,
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

    [string]$RepoRoot
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
    'DBKEY',
    'APPKEY',
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
    'EMAIL_FROM',
    'EMAIL_SMTP_GATEWAY',
    'EMAIL_SMTP_PORT'
)

# -- Pull each secret from KV once (cache in a local map). Never log values. --
# Pulling once and reusing across pools avoids N*pools KV round-trips and keeps
# the value lifetime as short and as in-memory as possible.
function Get-Secrets {
    param([string[]]$Names)
    $map = @{}
    foreach ($name in $Names) {
        # KV secret names can't contain '_'; seed-kv-secrets.sh stored them dashed.
        $kvName = $name -replace '_', '-'
        Write-Step "pulling secret '$kvName' from $VaultName"
        $tmp = New-TemporaryFile
        try {
            $val = & $az keyvault secret show --vault-name $VaultName --name $kvName `
                --query value --output tsv 2>$tmp
            if ($LASTEXITCODE -ne 0) {
                $err = (Get-Content $tmp -Raw) -replace '\s+$', ''
                Stop-Inject "failed to read '$kvName' from $VaultName (exit $LASTEXITCODE): $err"
            }
        } finally {
            Remove-Item $tmp -ErrorAction SilentlyContinue
        }
        # tsv of a single value may arrive as a 1-element array; join + trim the
        # trailing newline az appends. Do NOT trim interior whitespace (XML keys
        # / base64 are significant).
        $val = ($val | Out-String) -replace "`r?`n$", ''
        if ([string]::IsNullOrWhiteSpace($val)) {
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
    #       default      { return @('DBKEY','APPKEY', ...) }
    #   }
}

# -- Write one env var into a pool's <environmentVariables> in
# applicationHost.config. Create-or-update; idempotent. Uses the
# WebAdministration config provider so the write lands in
# applicationHost.config (not web.config). -----------------------------------
function Set-PoolEnvVar {
    param([string]$PoolName, [string]$Name, [string]$Value)

    $filter = "system.applicationHost/applicationPools/add[@name='$PoolName']/environmentVariables"
    # Remove an existing same-named entry first (Set-WebConfigurationProperty
    # cannot update a collection element keyed by name in place), then add.
    $existing = Get-WebConfiguration -PSPath 'MACHINE/WEBROOT/APPHOST' `
        -Filter "$filter/add[@name='$Name']" -ErrorAction SilentlyContinue
    if ($existing) {
        Remove-WebConfigurationProperty -PSPath 'MACHINE/WEBROOT/APPHOST' `
            -Filter $filter -Name '.' -AtElement @{ name = $Name } -ErrorAction SilentlyContinue
    }
    Add-WebConfigurationProperty -PSPath 'MACHINE/WEBROOT/APPHOST' `
        -Filter $filter -Name '.' -Value @{ name = $Name; value = $Value }
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
Write-Ok "target app pools: $($allPools.Count) -- $($allPools -join ', ')"

# Pull every secret + config value once up front (fail fast if any is missing/empty).
$secretValues = Get-Secrets -Names ($AllSecretNames + $ConfigNames)

foreach ($pool in $allPools) {
    if (-not (Test-Path "IIS:\AppPools\$pool")) {
        Stop-Inject "app pool '$pool' does not exist -- run deploy-iis.ps1 first (it creates the pools)"
    }
    $names = @(Get-SecretsForPool -PoolName $pool) + $ConfigNames
    Write-Host ''
    Write-Host "=== $pool : injecting $($names.Count) var(s) ==="
    foreach ($name in $names) {
        Set-PoolEnvVar -PoolName $pool -Name $name -Value $secretValues[$name]
        Write-Ok "  $pool <- $name (value hidden)"
    }
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
