# deploy/scripts/deploy/vm-fetch-source.ps1
# Fetches the kash-cards source onto the VM for build-on-box. Uses the GitHub zipball
# via built-in Invoke-WebRequest/Expand-Archive -- no git needed on the VM. Idempotent:
# replaces TargetDir with fresh source each run (config is written separately by
# vm-write-config; secrets come from Key Vault, so neither is in the zipball).
param(
    [Parameter(Mandatory = $true)][string]$RepoUrl,   # https://github.com/<owner>/<repo>[.git]
    [string]$Branch = 'main',
    [string]$TargetDir = 'C:\src\kash-cards',
    [string]$KvName,                                   # optional: pull REPO-TOKEN for a private repo
    [string]$Token                                     # optional PAT fallback
)
$ErrorActionPreference = 'Stop'
function Step($m) { Write-Host "[..] $m" }
function Ok($m)   { Write-Host "[ok] $m" }
function Die($m)  { Write-Host "[xx] $m"; exit 1 }

# Derive owner/repo + the codeload zip URL from the GitHub URL.
$u = $RepoUrl -replace '\.git$', ''
if ($u -notmatch 'github\.com[:/]+([^/]+)/([^/]+)/?$') { Die "RepoUrl is not a recognized GitHub URL: $RepoUrl" }
$owner = $Matches[1]; $repo = $Matches[2]
$zipUrl = "https://github.com/$owner/$repo/archive/refs/heads/$Branch.zip"

# Optional token (private repo): prefer Key Vault via the VM managed identity.
if (-not $Token -and $KvName -and (Get-Command az -ErrorAction SilentlyContinue)) {
    & az login --identity --output none 2>$null
    if ($LASTEXITCODE -eq 0) {
        $kvTok = (& az keyvault secret show --vault-name $KvName --name 'REPO-TOKEN' --query value -o tsv 2>$null)
        if (-not [string]::IsNullOrWhiteSpace($kvTok)) { $Token = $kvTok; Step 'using git token from Key Vault (REPO-TOKEN)' }
    }
}
$headers = @{}
if ($Token) { $headers['Authorization'] = "token $Token" }

[System.Net.ServicePointManager]::SecurityProtocol = [System.Net.SecurityProtocolType]::Tls12
$tmpZip = Join-Path $env:TEMP ("kc-src-" + [guid]::NewGuid().ToString('N') + ".zip")
$tmpDir = Join-Path $env:TEMP ("kc-src-" + [guid]::NewGuid().ToString('N'))
try {
    Step "downloading $zipUrl"
    Invoke-WebRequest -Uri $zipUrl -Headers $headers -OutFile $tmpZip -UseBasicParsing
    Step "extracting"
    Expand-Archive -Path $tmpZip -DestinationPath $tmpDir -Force
    # GitHub zipballs contain a single top-level dir: <repo>-<branch>.
    $inner = Get-ChildItem -Path $tmpDir -Directory | Select-Object -First 1
    if (-not $inner) { Die "unexpected zip layout (no top-level directory)" }

    if (Test-Path $TargetDir) { Remove-Item -Recurse -Force $TargetDir }
    $parent = Split-Path $TargetDir -Parent
    if ($parent -and -not (Test-Path $parent)) { New-Item -ItemType Directory -Force -Path $parent | Out-Null }
    Move-Item -Path $inner.FullName -Destination $TargetDir
} finally {
    Remove-Item -Force $tmpZip -ErrorAction SilentlyContinue
    Remove-Item -Recurse -Force $tmpDir -ErrorAction SilentlyContinue
}
Ok "source at $TargetDir (from $owner/$repo@$Branch)"
