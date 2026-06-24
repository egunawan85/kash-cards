# deploy/scripts/load-env.ps1
# LOCAL/DEV helper: loads deploy/secrets/.env + .vault into the CURRENT process's
# environment so the app (and QryptoCard.Sec.SecretsConfig) can read them when run
# locally. On the SERVER, secrets are injected per app-pool from Key Vault instead
# (inject-secrets.ps1) — this script is for local/dev only.
#
# Usage (dot-source so the vars persist in your session):
#   . .\deploy\scripts\load-env.ps1
param(
    [string]$SecretsDir = (Join-Path $PSScriptRoot '..\secrets')
)
$ErrorActionPreference = 'Stop'

function Import-EnvFile([string]$path) {
    if (-not (Test-Path -LiteralPath $path)) { Write-Warning "env file not found: $path"; return }
    $count = 0
    foreach ($line in Get-Content -LiteralPath $path) {
        $t = $line.Trim()
        if ($t -eq '' -or $t.StartsWith('#')) { continue }
        $idx = $t.IndexOf('=')
        if ($idx -lt 1) { continue }
        $name  = $t.Substring(0, $idx).Trim()
        $value = $t.Substring($idx + 1).Trim()
        [Environment]::SetEnvironmentVariable($name, $value, 'Process')
        $count++
    }
    Write-Host ("Loaded {0} var(s) from {1}" -f $count, (Split-Path -Leaf $path))
}

Import-EnvFile (Join-Path $SecretsDir '.env')
Import-EnvFile (Join-Path $SecretsDir '.vault')
Write-Host "Done. These vars live in the current process only."
