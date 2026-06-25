# deploy/scripts/deploy/vm-write-config.ps1
# Writes the filled (gitignored) infra config onto the VM so the deploy-phase scripts
# (vm-publish-schema / deploy-iis / inject-secrets) can source it. The orchestrator
# base64-encodes deploy/config/.env.provision.<env> and passes it as -ConfigB64; this
# decodes it to the same relative path inside the cloned source. Idempotent (overwrite).
param(
    [Parameter(Mandatory = $true)][string]$ConfigB64,
    [string]$Env = 'dev',
    [string]$SourceDir = 'C:\src\kash-cards'
)
$ErrorActionPreference = 'Stop'
function Ok($m)  { Write-Host "[ok] $m" }
function Die($m) { Write-Host "[xx] $m"; exit 1 }

$dest = Join-Path $SourceDir "deploy\config\.env.provision.$Env"
$dir  = Split-Path $dest -Parent
if (-not (Test-Path $dir)) { Die "config dir $dir not found (run vm-fetch-source first)" }

$bytes = [Convert]::FromBase64String($ConfigB64)
$text  = [System.Text.Encoding]::UTF8.GetString($bytes)
# Write without a BOM; the sourcing scripts read plain KEY=VALUE lines.
[System.IO.File]::WriteAllText($dest, $text, (New-Object System.Text.UTF8Encoding($false)))

# Lock it down: it carries subscription id + names (not secrets, but not world-readable).
& icacls.exe $dest /inheritance:r | Out-Null
& icacls.exe $dest /grant:r 'BUILTIN\Administrators:F' 'NT AUTHORITY\SYSTEM:F' | Out-Null

$lineCount = ($text -split "`n").Count
Ok "wrote infra config -> $dest ($lineCount lines)"
