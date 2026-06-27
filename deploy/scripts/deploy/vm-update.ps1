# vm-update.ps1 -- ON-BOX deploy orchestrator. Runs the WHOLE `update` locally in ONE process:
#   (config-backup) -> fetch-source -> write-config -> [db-backup + migrate] -> build+publish
#   -> inject-secrets -> iis-start.
#
# WHY: the box is NSG-dark, so deploy.sh drives it via `az vm run-command`. Each such call has a
# fixed ~30s control-plane round-trip; the old `update` made ~6 of them (plus a per-tier build
# loop), so round-trip overhead dominated the wall-clock. This script collapses all of that into
# ONE run-command: deploy.sh fires it once, and every step runs back-to-back on the box with NO
# round-trips between them -- the runegate on-box model, achieved without opening any inbound
# (this still rides the same control plane; no RDP/WinRM/SSH listener is involved).
#
# Each sibling step runs as a CHILD powershell whose full output is captured to an on-box log; this
# script's STDOUT carries only concise [ok]/[xx] step markers (so the run-command response stays
# small and the [xx] fatal marker -- which deploy.sh scans for -- is always visible). A child's own
# `exit` cannot terminate this orchestrator early. Any non-zero child exit OR any [xx] in a step's
# output aborts the whole update LOUDLY (prints the log tail), so a half-finished deploy can never
# report success.
[CmdletBinding()]
param(
    [Parameter(Mandatory)][string]$RepoUrl,
    [string]$Branch = 'main',
    [Parameter(Mandatory)][string]$KvName,
    [Parameter(Mandatory)][string]$ConfigB64,
    [ValidateSet('dev', 'stg', 'prd')][string]$Env = 'dev',
    [int]$WithSchema = 0,
    [string]$Service = ''
)
$ErrorActionPreference = 'Stop'

$RepoRoot = 'C:\src\kash-cards'
$D        = Join-Path $RepoRoot 'deploy\scripts\deploy'
$Tmp      = Join-Path $RepoRoot 'tmp'
if (-not (Test-Path $Tmp)) { New-Item -ItemType Directory -Path $Tmp -Force | Out-Null }
$stamp = Get-Date -Format 'yyyyMMdd-HHmmss'
$Log   = Join-Path $Tmp "update-$stamp.log"

function Note($m) {
    Write-Host $m
    ("[{0:HH:mm:ss}] {1}" -f (Get-Date), $m) | Out-File -FilePath $Log -Append -Encoding utf8
}

# Run a sibling on-box script as a CHILD process; capture its output to the run log; fail LOUD on a
# non-zero exit OR any [xx] the child emitted. Child process so the sibling's own `exit N` (every
# vm-side script Stop-*/die's that way) sets $LASTEXITCODE instead of killing this orchestrator.
function Step($label, $script, [string[]]$a) {
    if (-not (Test-Path $script)) { Note "[xx] ${label}: script not found ($script)"; exit 1 }
    $stepLog = "$Log.step"
    "==== $label :: $script $($a -join ' ') ====" | Out-File -FilePath $Log -Append -Encoding utf8
    & powershell.exe -NoProfile -ExecutionPolicy Bypass -File $script @a *> $stepLog
    $code = $LASTEXITCODE; if ($null -eq $code) { $code = 0 }
    Get-Content -LiteralPath $stepLog -ErrorAction SilentlyContinue | Out-File -FilePath $Log -Append -Encoding utf8
    $xx = Select-String -Path $stepLog -Pattern '[xx]' -SimpleMatch -Quiet -ErrorAction SilentlyContinue
    Remove-Item $stepLog -ErrorAction SilentlyContinue
    if ($code -ne 0 -or $xx) {
        Note "[xx] $label FAILED (exit=$code) -- last 30 log lines:"
        Get-Content -LiteralPath $Log -Tail 30 -ErrorAction SilentlyContinue | ForEach-Object { Write-Host $_ }
        exit 1
    }
    Note "[ok] $label"
}

$scope = if ($Service) { "Service=$Service" } else { 'all tiers' }
Note "=== on-box update start (Env=$Env, Branch=$Branch, $scope$(if ($WithSchema -eq 1) { ', +schema' })) ==="

# -- S6: back up applicationHost.config BEFORE any step mutates it (deploy-iis pools/ACL,
# inject-secrets per-pool env). Emulates runegate's enable-https.ps1: timestamped .bak, keep the
# 5 most recent. This is the IIS-config rollback point for the deploy.
$apphost = Join-Path $env:windir 'System32\inetsrv\config\applicationHost.config'
if (Test-Path $apphost) {
    Copy-Item $apphost "$apphost.bak-$stamp" -Force
    Note "[ok] config-backup: applicationHost.config -> applicationHost.config.bak-$stamp"
    Get-ChildItem "$apphost.bak-*" -ErrorAction SilentlyContinue | Sort-Object LastWriteTime -Descending |
        Select-Object -Skip 5 | Remove-Item -Force -ErrorAction SilentlyContinue
} else {
    Note "[..] applicationHost.config not found ($apphost) -- skipping config backup"
}

$svc = @(); if ($Service) { $svc = @('-Service', $Service) }

# Order matters: fetch REPLACES the source tree (wiping the gitignored env config), so write-config
# MUST follow it; migrate/build/inject/start all need that restored config + the fresh source.
Step 'fetch-source'   "$D\vm-fetch-source.ps1" @('-RepoUrl', $RepoUrl, '-Branch', $Branch, '-KvName', $KvName)
Step 'write-config'   "$D\vm-write-config.ps1" @('-ConfigB64', $ConfigB64, '-Env', $Env)
if ($WithSchema -eq 1) {
    # vm-migrate performs the S6b DB backup (BACKUP DATABASE) before applying any migration.
    Step 'schema-migrate' "$D\vm-migrate.ps1" @('-Env', $Env)
}
Step 'build-publish'  "$D\deploy-iis.ps1"     (@('-Env', $Env) + $svc)   # pulls DB-PASSWORD from KV itself
Step 'inject-secrets' "$D\inject-secrets.ps1" (@('-Env', $Env) + $svc)
Step 'iis-start'      "$D\vm-iis-ops.ps1"     (@('-Action', 'start') + $svc)

Note "[ok] update complete (Env=$Env, $scope) -- log: $Log"
exit 0
