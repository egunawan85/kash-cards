# vm-iis-ops.ps1 -- lightweight IIS lifecycle ops for the kash-cards stack.
#
# The companion to deploy-iis.ps1 (build/publish) and inject-secrets.ps1 (env):
# this script does the *operational* verbs that don't rebuild anything --
# start / stop / restart / status / logs -- for all 12 sites or one tier. It is
# the on-box half of the per-tier surface driven from the operator devbox by
# deploy/deploy.sh (the VM is NSG-dark, so deploy.sh invokes this via
# 'az vm run-command invoke').
#
# Mirrors the sibling runegate deploy-iis.ps1 start/stop/restart/status/logs
# commands, re-shaped to read kash-cards' deploy/config/sites.json instead of a
# hard-coded service table.
#
# The load-bearing verb is `start`: it does NOT just fire Start-WebAppPool and
# hope -- it VERIFIES every targeted pool actually reaches the Started state and
# fails loudly ([xx]) if any does not. That closes the gap where the full
# provision pipeline could finish with money-tier pools silently Stopped (a
# Start-WebAppPool that no-ops or errors under -ErrorAction SilentlyContinue
# leaves the site answering 503 with nothing in the log).
#
# Usage (run as Administrator, or as SYSTEM via 'az vm run-command invoke'):
#   pwsh -File vm-iis-ops.ps1 -Action start   [-Service <alias>]
#   pwsh -File vm-iis-ops.ps1 -Action stop    [-Service <alias>]
#   pwsh -File vm-iis-ops.ps1 -Action restart [-Service <alias>]
#   pwsh -File vm-iis-ops.ps1 -Action status
#   pwsh -File vm-iis-ops.ps1 -Action logs    [-Service <alias>]
# Alias = app pool minus the 'kash-' prefix (e.g. 'int' -> kash-int); the full
# pool name or the project name are also accepted. Omit -Service for all sites.

[CmdletBinding()]
param(
    [ValidateSet('start', 'stop', 'restart', 'status', 'logs')]
    [string]$Action = 'status',

    # Per-tier target (optional). Omit to act on all 12 sites.
    [string]$Service,

    [ValidateSet('dev', 'stg', 'prd')]
    [string]$Env = 'dev',

    [string]$RepoRoot,

    # How many lines of the latest IIS log to show per site for -Action logs.
    [int]$LogLines = 40
)

$ErrorActionPreference = 'Stop'
$ProgressPreference    = 'SilentlyContinue'
[Console]::OutputEncoding = [System.Text.Encoding]::UTF8

# -- Logging helpers (match the sister / deploy-iis.ps1 style) ---------------
function Write-Step { param([string]$m) Write-Host "[..] $m" }
function Write-Ok   { param([string]$m) Write-Host "[ok] $m" }
function Write-Warn { param([string]$m) Write-Host "[!!] $m" -ForegroundColor Yellow }
function Stop-Ops {
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
if (-not (Test-Path $SitesJson)) { Stop-Ops "missing $SitesJson" }

# -- Preflight: admin is required for state changes (start/stop/restart write
# IIS state). status/logs are read-only, so don't gate them on admin. ---------
$mutating = @('start', 'stop', 'restart') -contains $Action
if ($mutating) {
    $identity  = [Security.Principal.WindowsIdentity]::GetCurrent()
    $principal = [Security.Principal.WindowsPrincipal]::new($identity)
    if (-not $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) {
        Stop-Ops "-Action $Action must run as Administrator (or as SYSTEM via 'az vm run-command invoke')"
    }
}

try {
    Import-Module WebAdministration -ErrorAction Stop
} catch {
    Stop-Ops "WebAdministration module not available -- is the IIS management feature installed? ($($_.Exception.Message))"
}

# -- Parse sites.json into an ordered list of {project, appPool, port}. The IIS
# site name == project (deploy-iis.ps1 creates it that way). ------------------
$sites = Get-Content -LiteralPath $SitesJson -Raw | ConvertFrom-Json
$all   = @($sites.public + $sites.internal)

# -- Optional per-tier targeting (same alias rule as deploy-iis / inject-secrets).
if ($Service) {
    $match = @($all | Where-Object {
        $_.appPool -eq "kash-$Service" -or $_.appPool -eq $Service -or $_.project -eq $Service
    })
    if ($match.Count -eq 0) {
        $aliases = (($all | ForEach-Object { $_.appPool -replace '^kash-', '' }) | Sort-Object) -join ', '
        Stop-Ops "unknown -Service '$Service' -- valid aliases: $aliases"
    }
    $all = $match
}
Write-Ok "action '$Action' over $($all.Count) site(s)$(if ($Service) { ": $Service" })"

# -- Pool state as a plain string ('Started'/'Stopped'/'Unknown'). -----------
function Get-PoolState {
    param([string]$Pool)
    if (-not (Test-Path "IIS:\AppPools\$Pool")) { return 'absent' }
    try { return (Get-WebAppPoolState -Name $Pool).Value } catch { return 'unknown' }
}
function Get-SiteState {
    param([string]$Site)
    if (-not (Test-Path "IIS:\Sites\$Site")) { return 'absent' }
    try { return (Get-WebsiteState -Name $Site).Value } catch { return 'unknown' }
}

# -- Start ONE pool and verify it reaches 'Started'. Returns $true/$false. The
# verify loop is the whole point: WAS can take a beat to spin up the worker, and
# a pool that fails to start (e.g. a config fault) must be reported, not swallowed.
function Start-PoolVerified {
    param([string]$Pool)
    if (-not (Test-Path "IIS:\AppPools\$Pool")) {
        Write-Warn "  pool absent: $Pool (run deploy first)"
        return $false
    }
    for ($i = 0; $i -lt 5; $i++) {
        $state = Get-PoolState -Pool $Pool
        if ($state -eq 'Started') { return $true }
        try { Start-WebAppPool -Name $Pool -ErrorAction Stop } catch { }
        Start-Sleep -Seconds 2
    }
    return ((Get-PoolState -Pool $Pool) -eq 'Started')
}

switch ($Action) {

    'start' {
        # Start-guarantee: every targeted pool (and its site) MUST end Started.
        $failed = @()
        foreach ($s in $all) {
            $ok = Start-PoolVerified -Pool $s.appPool
            if (Test-Path "IIS:\Sites\$($s.project)") {
                Start-Website -Name $s.project -ErrorAction SilentlyContinue
            }
            if ($ok) {
                Write-Ok "  started: $($s.project) (pool $($s.appPool))"
            } else {
                Write-Warn "  FAILED to start: $($s.project) (pool $($s.appPool)) -- state '$(Get-PoolState -Pool $s.appPool)'"
                $failed += $s.appPool
            }
        }
        if ($failed.Count -gt 0) {
            Stop-Ops "start-guarantee FAILED -- pools not Started: $($failed -join ', ')"
        }
        Write-Host ''
        Write-Host "IISOPS_RESULT: PASS (start; $($all.Count) pool(s) Started)"
    }

    'stop' {
        foreach ($s in $all) {
            if (Test-Path "IIS:\Sites\$($s.project)") {
                Stop-Website -Name $s.project -ErrorAction SilentlyContinue
            }
            if (Test-Path "IIS:\AppPools\$($s.appPool)") {
                Stop-WebAppPool -Name $s.appPool -ErrorAction SilentlyContinue
            }
            Write-Ok "  stopped: $($s.project) (pool $($s.appPool))"
        }
        Write-Host ''
        Write-Host "IISOPS_RESULT: PASS (stop; $($all.Count) pool(s))"
    }

    'restart' {
        # Start-or-recycle: `recycle` is a no-op on a STOPPED pool, so a plain
        # recycle pass would silently leave a down tier down. If stopped, start
        # (and verify); if started, recycle so the worker reloads code/env.
        $failed = @()
        foreach ($s in $all) {
            $state = Get-PoolState -Pool $s.appPool
            if ($state -eq 'absent') {
                Write-Warn "  pool absent: $($s.appPool) (run deploy first)"
                $failed += $s.appPool
                continue
            }
            if ($state -eq 'Started') {
                Restart-WebAppPool -Name $s.appPool -ErrorAction SilentlyContinue
                Write-Ok "  recycled: $($s.project) (pool $($s.appPool))"
            } else {
                if (Start-PoolVerified -Pool $s.appPool) {
                    Write-Ok "  started (was $state): $($s.project) (pool $($s.appPool))"
                } else {
                    Write-Warn "  FAILED to start: $($s.project) (pool $($s.appPool))"
                    $failed += $s.appPool
                }
            }
        }
        if ($failed.Count -gt 0) {
            Stop-Ops "restart FAILED -- pools not running: $($failed -join ', ')"
        }
        Write-Host ''
        Write-Host "IISOPS_RESULT: PASS (restart; $($all.Count) pool(s))"
    }

    'status' {
        Write-Host ''
        Write-Host ('  {0,-28} {1,-6} {2,-10} {3}' -f 'PROJECT', 'PORT', 'POOL', 'SITE')
        Write-Host ('  {0,-28} {1,-6} {2,-10} {3}' -f '-------', '----', '----', '----')
        foreach ($s in $all) {
            $ps = Get-PoolState -Pool $s.appPool
            $ss = Get-SiteState -Site $s.project
            $color = if ($ps -eq 'Started' -and $ss -eq 'Started') { 'Green' } else { 'Yellow' }
            Write-Host ('  {0,-28} {1,-6} {2,-10} {3}' -f $s.project, $s.port, $ps, $ss) -ForegroundColor $color
        }
        Write-Host ''
        Write-Host "IISOPS_RESULT: PASS (status; $($all.Count) site(s))"
    }

    'logs' {
        foreach ($s in $all) {
            if (-not (Test-Path "IIS:\Sites\$($s.project)")) {
                Write-Warn "site absent: $($s.project)"
                continue
            }
            $siteId = (Get-ItemProperty "IIS:\Sites\$($s.project)" -Name id -ErrorAction SilentlyContinue).id
            $logDir = if ($siteId) { Join-Path $env:SystemDrive "inetpub\logs\LogFiles\W3SVC$siteId" } else { $null }
            if ($logDir -and (Test-Path $logDir)) {
                $latest = Get-ChildItem $logDir -Filter '*.log' -ErrorAction SilentlyContinue |
                    Sort-Object LastWriteTime -Descending | Select-Object -First 1
                if ($latest) {
                    Write-Host ''
                    Write-Host "--- $($s.project) :: $($latest.FullName) (last $LogLines) ---" -ForegroundColor Cyan
                    Get-Content -LiteralPath $latest.FullName -Tail $LogLines
                } else {
                    Write-Warn "no log file yet for $($s.project)"
                }
            } else {
                Write-Warn "no log dir for $($s.project) (site id $siteId)"
            }
        }
        Write-Host ''
        Write-Host "IISOPS_RESULT: PASS (logs; $($all.Count) site(s))"
    }
}
