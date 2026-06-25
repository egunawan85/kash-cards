# deploy/scripts/deploy/vm-fetch-source.ps1
# Fetches the kash-cards source onto the VM for build-on-box. Idempotent: clones if
# absent, otherwise fetches + hard-resets to the requested ref. Runs ON the VM.
#
# (The sister runegate-infra used GitHub deploy keys; for the dev shakeout we take a
#  repo URL + optional token so a throwaway box needs no standing key. For prod, prefer
#  a short-lived deploy key or a managed-identity-gated artifact pull.)
param(
    [Parameter(Mandatory = $true)][string]$RepoUrl,   # https URL or git@ ssh; do NOT embed a token here
    [string]$Branch = 'main',
    [string]$TargetDir = 'C:\src\kash-cards',
    [string]$KvName,                                   # if set, pull an optional REPO-TOKEN secret from KV
    [string]$Token                                     # optional PAT fallback; prefer KV so it stays off argv/logs
)
$ErrorActionPreference = 'Stop'
function Step($m) { Write-Host "[..] $m" }
function Ok($m)   { Write-Host "[ok] $m" }
function Die($m)  { Write-Host "[xx] $m"; exit 1 }

if (-not (Get-Command git -ErrorAction SilentlyContinue)) { Die 'git not on PATH (install via vm-bootstrap)' }

# Prefer a token from Key Vault (via the VM managed identity) over -Token, so a PAT
# never rides argv / run-command parameters / operator logs. A public repo simply has
# no REPO-TOKEN secret and clones without one.
if (-not $Token -and $KvName -and (Get-Command az -ErrorAction SilentlyContinue)) {
    & az login --identity --output none 2>$null
    if ($LASTEXITCODE -eq 0) {
        $kvTok = (& az keyvault secret show --vault-name $KvName --name 'REPO-TOKEN' --query value -o tsv 2>$null)
        if (-not [string]::IsNullOrWhiteSpace($kvTok)) { $Token = $kvTok; Step 'using git token from Key Vault (REPO-TOKEN)' }
    }
}

# Inject the token into an https URL without logging it.
$url = $RepoUrl
if ($Token -and $RepoUrl -match '^https://') {
    $url = $RepoUrl -replace '^https://', "https://$Token@"
}

if (Test-Path (Join-Path $TargetDir '.git')) {
    Step "updating existing checkout at $TargetDir -> $Branch"
    git -C $TargetDir remote set-url origin $url 2>&1 | Out-Null
    git -C $TargetDir fetch --depth 1 origin $Branch 2>&1 | Out-Null
    git -C $TargetDir checkout -B $Branch "origin/$Branch" 2>&1 | Out-Null
    git -C $TargetDir reset --hard "origin/$Branch" 2>&1 | Out-Null
} else {
    Step "cloning $Branch into $TargetDir"
    $parent = Split-Path $TargetDir -Parent
    if ($parent -and -not (Test-Path $parent)) { New-Item -ItemType Directory -Force -Path $parent | Out-Null }
    git clone --depth 1 --branch $Branch $url $TargetDir 2>&1 | Out-Null
}
# Scrub the token from the stored remote so it isn't persisted on disk.
if ($Token) { git -C $TargetDir remote set-url origin $RepoUrl 2>&1 | Out-Null }

$head = (git -C $TargetDir rev-parse --short HEAD)
Ok "source at $TargetDir @ $head ($Branch)"
