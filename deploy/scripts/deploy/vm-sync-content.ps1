# vm-sync-content.ps1 -- on-box fast lane for front-end-only redeploys.
#
# The companion to deploy-iis.ps1 (full build/publish): this script does NOT
# build, restore, or recycle anything by default. It receives a small tarball of
# already-source-shaped front-end files (static assets + ASP.NET markup) from the
# operator devbox and drops them straight into the live published site root
# (C:\inetpub\kash-cards\<project>), where IIS serves them on the next request --
# static content immediately, .aspx/.ascx/.master markup recompiled on demand by
# ASP.NET. No MSBuild, no NuGet restore, no secret inject, no app-pool recycle.
#
# This is the on-box half of `deploy/deploy.sh sync`, which is how a front-end
# change reaches the box in seconds instead of the minutes a full `update` costs
# (fetch the whole repo zipball -> solution restore -> clean Release publish).
# The VM is NSG-dark, so -- like every other on-box step -- it runs via
# 'az vm run-command invoke'; the files arrive base64-encoded in -PayloadB64.
#
# SAFETY: this script will only ever WRITE FILES into one existing site root. It
# refuses to (a) extract anything that needs a compile (*.cs, *.csproj, *.resx,
# packages.config) or that would clobber on-box-patched config (*.config), and
# (b) extract any entry with an absolute path or a '..' segment (tar path
# traversal). The devbox half applies the same filter; this is defence in depth,
# because a payload reaching here as SYSTEM must not be trusted to be well-formed.
# It never touches IIS bindings, so the loopback-only invariant on the money
# tiers is untouched -- it only refreshes content under a site that already exists
# (which a prior full `update`/`deploy-iis` created and bound).
#
# Usage (run as Administrator, or as SYSTEM via 'az vm run-command invoke'):
#   pwsh -File vm-sync-content.ps1 -PayloadB64 <b64-of-a-.tgz> -Service <alias>
#                                  [-Project <name>] [-Recycle]
# -Service is the app pool minus 'kash-' (e.g. 'dashboard' -> kash-dashboard);
# the full pool name or the project name also resolve. -Project, when supplied by
# the devbox, is used directly and -Service is only used for the log line.

[CmdletBinding()]
param(
    # Base64 of a gzip tarball whose entries are paths RELATIVE to the project
    # root (e.g. 'Content/site.css', 'login.aspx'), so they extract directly
    # under the site root. Produced by deploy/deploy.sh sync.
    [Parameter(Mandatory = $true)][string]$PayloadB64,

    # Tier selector (alias / pool / project). Required for the log line and as the
    # fallback when -Project is not passed.
    [Parameter(Mandatory = $true)][string]$Service,

    # Resolved project name from the devbox. When set, used verbatim; otherwise
    # resolved from -Service via sites.json.
    [string]$Project,

    # Live publish root; one subdir per project (matches deploy-iis.ps1).
    [string]$PublishRoot = 'C:\inetpub\kash-cards',

    [ValidateSet('dev', 'stg', 'prd')]
    [string]$Env = 'dev',

    [string]$RepoRoot,

    # Recycle the tier's app pool after the sync. Off by default -- static and
    # markup edits take effect with no recycle; pass this only when a change needs
    # the worker to restart (rare for a pure front-end edit). A STRING, not a
    # [bool]/[switch]: run-command maps Name=Value -> -Name Value, and Windows
    # PowerShell 5.1 refuses to bind the literal "true" to a [bool] parameter.
    [string]$Recycle = ''
)
$DoRecycle = $Recycle -in @('true', '1', 'yes', 'on')

$ErrorActionPreference = 'Stop'
$ProgressPreference    = 'SilentlyContinue'
[Console]::OutputEncoding = [System.Text.Encoding]::UTF8

function Write-Step { param([string]$m) Write-Host "[..] $m" }
function Write-Ok   { param([string]$m) Write-Host "[ok] $m" }
function Write-Warn { param([string]$m) Write-Host "[!!] $m" -ForegroundColor Yellow }
function Stop-Sync {
    param([string]$m)
    Write-Host "[xx] $m" -ForegroundColor Red
    exit 1
}

# -- Resolve paths (same fallback as the sibling deploy scripts) -------------
if (-not $RepoRoot) {
    $candidate = Join-Path $PSScriptRoot '..\..\..'
    if (Test-Path (Join-Path $candidate 'deploy\config\sites.json')) {
        $RepoRoot = (Resolve-Path $candidate).Path
    } else {
        $RepoRoot = 'C:\src\kash-cards'
    }
}
$SitesJson = Join-Path $RepoRoot 'deploy\config\sites.json'

# -- Resolve the target project ----------------------------------------------
# Prefer the devbox-supplied -Project; else resolve -Service via sites.json with
# the same alias rule as deploy-iis.ps1 / vm-iis-ops.ps1.
if ([string]::IsNullOrWhiteSpace($Project)) {
    if (-not (Test-Path $SitesJson)) { Stop-Sync "missing $SitesJson and no -Project supplied" }
    $sites = Get-Content -LiteralPath $SitesJson -Raw | ConvertFrom-Json
    $all   = @($sites.public + $sites.internal)
    $match = @($all | Where-Object {
        $_.appPool -eq "kash-$Service" -or $_.appPool -eq $Service -or $_.project -eq $Service
    })
    if ($match.Count -eq 0) {
        $aliases = (($all | ForEach-Object { $_.appPool -replace '^kash-', '' }) | Sort-Object) -join ', '
        Stop-Sync "unknown -Service '$Service' -- valid aliases: $aliases"
    }
    $Project = $match[0].project
    $pool    = $match[0].appPool
} else {
    # -Project given; still resolve the pool for an optional -Recycle.
    if (Test-Path $SitesJson) {
        $sites = Get-Content -LiteralPath $SitesJson -Raw | ConvertFrom-Json
        $all   = @($sites.public + $sites.internal)
        $m     = @($all | Where-Object { $_.project -eq $Project }) | Select-Object -First 1
        if ($m) { $pool = $m.appPool }
    }
}

$dest = Join-Path $PublishRoot $Project
if (-not (Test-Path $dest)) {
    Stop-Sync "site root not found: $dest -- run 'deploy.sh update $Service' once before syncing"
}
Write-Ok "sync target: $Project -> $dest"

# -- Locate tar.exe (bsdtar; ships in System32 on Server 2019+). We need a real
# tar here because Windows PowerShell 5.1 (.NET Framework, what run-command uses)
# has no built-in tar reader. -------------------------------------------------
$tar = (Get-Command tar.exe -ErrorAction SilentlyContinue).Source
if (-not $tar) {
    $sys32 = Join-Path $env:SystemRoot 'System32\tar.exe'
    if (Test-Path $sys32) { $tar = $sys32 }
}
if (-not $tar) { Stop-Sync "tar.exe not found (expected in System32 on Server 2019+) -- cannot unpack sync payload" }

# -- Decode the payload to a temp tarball ------------------------------------
if ([string]::IsNullOrWhiteSpace($PayloadB64)) { Stop-Sync "empty -PayloadB64" }
$tmpTgz = Join-Path $env:TEMP ("kc-sync-" + [guid]::NewGuid().ToString('N') + ".tgz")
try {
    try {
        [System.IO.File]::WriteAllBytes($tmpTgz, [System.Convert]::FromBase64String($PayloadB64))
    } catch {
        Stop-Sync "could not decode -PayloadB64 as base64: $($_.Exception.Message)"
    }

    # -- List entries and validate BEFORE writing anything. Two refusals:
    #    1. compile-triggers / on-box-patched config -- the fast lane must never
    #       ship a file that needs a build or clobber a patched Web.config.
    #    2. path traversal -- an absolute path or a '..' segment could escape the
    #       site root (bsdtar does not block these on its own).
    $entries = @(& $tar -tzf $tmpTgz 2>&1)
    if ($LASTEXITCODE -ne 0) { Stop-Sync "could not read sync payload as a gzip tar: $($entries -join '; ')" }
    $entries = @($entries | Where-Object { $_ -and -not ($_.ToString().EndsWith('/')) })
    if ($entries.Count -eq 0) { Stop-Sync "sync payload contains no files" }

    $blockRe = '(?i)(\.(cs|csproj|resx|config)$)|(^|/)packages\.config$|(^|/)(bin|obj)/'
    foreach ($e in $entries) {
        $p = $e.ToString().Replace('\', '/')
        if ($p -match '^([a-zA-Z]:|/)' -or $p -split '/' -contains '..') {
            Stop-Sync "refusing sync: unsafe path in payload (absolute or traversal): '$e'"
        }
        if ($p -match $blockRe) {
            Stop-Sync "refusing sync: payload contains a build-triggering or config file ('$e') -- run 'deploy.sh build $Service' / 'update $Service' instead"
        }
    }
    Write-Ok "payload validated: $($entries.Count) file(s), no compile-triggers, no traversal"

    # -- Extract into the site root (overwrites in place). ----------------------
    Write-Step "extracting $($entries.Count) file(s) into $dest"
    $extractErr = @(& $tar -xf $tmpTgz -C $dest 2>&1)
    if ($LASTEXITCODE -ne 0) { Stop-Sync "tar extract failed: $($extractErr -join '; ')" }
    foreach ($e in $entries) { Write-Ok "  synced: $e" }
} finally {
    Remove-Item -Force $tmpTgz -ErrorAction SilentlyContinue
}

# -- Optional recycle. Default off: the point of sync is to skip it. ---------
if ($DoRecycle) {
    if (-not $pool) {
        Write-Warn "no app pool resolved for '$Service' -- skipping -Recycle"
    } else {
        try {
            Import-Module WebAdministration -ErrorAction Stop
            if (Test-Path "IIS:\AppPools\$pool") {
                $state = (Get-WebAppPoolState -Name $pool -ErrorAction SilentlyContinue).Value
                if ($state -eq 'Started') {
                    Restart-WebAppPool -Name $pool -ErrorAction SilentlyContinue
                    Write-Ok "recycled app pool: $pool"
                } else {
                    Start-WebAppPool -Name $pool -ErrorAction SilentlyContinue
                    Write-Ok "started app pool (was $state): $pool"
                }
            } else {
                Write-Warn "app pool absent: $pool -- nothing to recycle"
            }
        } catch {
            Write-Warn "recycle failed for ${pool}: $($_.Exception.Message)"
        }
    }
}

Write-Host ''
Write-Host "SYNC_RESULT: PASS ($($entries.Count) file(s) -> $Project)"
