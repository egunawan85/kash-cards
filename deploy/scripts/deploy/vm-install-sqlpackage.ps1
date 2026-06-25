# vm-install-sqlpackage.ps1 -- install Microsoft sqlpackage on the dev VM so
# vm-publish-schema.ps1 can publish the schema-only DACPAC to on-box SQL Express.
#
# Adapted from runegate-infra/scripts/deploy/vm-install-sqlpackage.ps1. The
# sister installs the self-contained Windows .zip to a fixed C:\Tools path. We
# keep that fixed-path install as the durable artifact (it survives PATH-
# inheritance quirks on non-interactive SSH/run-command sessions) but PREFER
# `dotnet tool install -g microsoft.sqlpackage` when the .NET SDK is already on
# the box -- it is the lighter, self-updating path and lands sqlpackage on the
# dotnet-tools PATH entry. The published-zip route is the fallback for boxes
# without a usable `dotnet`.
#
# Why a separate installer (vs. folding into bootstrap): publishing a schema is
# a deploy-time op, not steady state -- keeping it standalone lets an operator
# run it ad-hoc on an already-provisioned VM without a full re-bootstrap. This
# mirrors the sister's vm-install-cloudflared.ps1 pattern.
#
# Idempotent: if `sqlpackage` already resolves and runs `/version`, we skip.
# A present-but-broken binary triggers a (re)install via whichever route works.
#
# Install / discovery locations checked, in order:
#   1. `sqlpackage` already on PATH (covers a prior dotnet-tool install).
#   2. The dotnet global-tools dir (%USERPROFILE%\.dotnet\tools), which a
#      dotnet-tool install may not have added to THIS session's PATH yet.
#   3. C:\Tools\sqlpackage\sqlpackage.exe -- the published-zip fixed path.
#
# Invocation (from operator's laptop):
#   az vm run-command invoke `
#     --resource-group rg-kash-dev --name vm-kash-dev `
#     --command-id RunPowerShellScript `
#     --scripts @deploy/scripts/deploy/vm-install-sqlpackage.ps1
# Or over RDP/SSH:
#   powershell -NoProfile -ExecutionPolicy Bypass -File vm-install-sqlpackage.ps1

[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$ProgressPreference    = 'SilentlyContinue'
[Console]::OutputEncoding = [System.Text.Encoding]::UTF8

function Write-Step { param([string]$m) Write-Host "[..] $m" }
function Write-Ok   { param([string]$m) Write-Host "[ok] $m" }
function Write-Warn { param([string]$m) Write-Host "[!!] $m" -ForegroundColor Yellow }
function Stop-Run   { param([string]$m) Write-Host "[xx] $m" -ForegroundColor Red; exit 1 }

# Microsoft's "always-latest" redirect for the self-contained Windows zip.
# TOFU over CA-validated TLS from microsoft.com is the same trust boundary the
# rest of the bootstrap uses for .NET / SQL Express; the binary is also code-
# signed by Microsoft. The fallback zip only runs when `dotnet tool` is not
# available, so this is the secondary path.
$SQLPACKAGE_URL  = 'https://aka.ms/sqlpackage-windows'
$INSTALL_DIR     = 'C:\Tools\sqlpackage'
$INSTALL_EXE     = Join-Path $INSTALL_DIR 'sqlpackage.exe'
$CACHE_DIR       = 'C:\bootstrap-cache'
$DOWNLOAD_ZIP    = Join-Path $CACHE_DIR 'sqlpackage-latest.zip'
$DOTNET_TOOLS    = Join-Path $env:USERPROFILE '.dotnet\tools'

# Make sure the known tool dirs are on THIS session's PATH so discovery and a
# fresh `dotnet tool install` are both visible without a new shell.
foreach ($d in @($DOTNET_TOOLS, $INSTALL_DIR)) {
    if ($d -and (($env:PATH -split ';') -notcontains $d)) {
        $env:PATH = "$d;$env:PATH"
    }
}

# Resolve sqlpackage to a runnable command (PATH entry or the fixed-path exe).
# Returns the invocation string, or $null if nothing is found.
function Resolve-SqlPackage {
    $onPath = (Get-Command sqlpackage -ErrorAction SilentlyContinue).Source
    if ($onPath) { return $onPath }
    if (Test-Path $INSTALL_EXE) { return $INSTALL_EXE }
    return $null
}

# Health check: present AND `/version` exits 0. Returns $true when good.
function Test-SqlPackage {
    param([string]$Exe)
    if (-not $Exe) { return $false }
    try {
        $out = & $Exe /version 2>&1
        if ($LASTEXITCODE -eq 0) {
            $line = ($out | Select-Object -First 1).ToString().Trim()
            Write-Ok "sqlpackage runs ($line) -- $Exe"
            return $true
        }
    } catch { }
    return $false
}

# -- 1. Already installed and healthy? Skip.
$existing = Resolve-SqlPackage
if (Test-SqlPackage -Exe $existing) {
    Write-Ok 'sqlpackage already installed -- skipping install'
    exit 0
}
if ($existing) {
    Write-Step "existing sqlpackage at '$existing' failed /version -- reinstalling"
}

# -- 2. Preferred route: dotnet global tool (only if `dotnet` is usable).
$dotnet = (Get-Command dotnet -ErrorAction SilentlyContinue).Source
if ($dotnet) {
    Write-Step 'installing sqlpackage via `dotnet tool install -g microsoft.sqlpackage`'
    # `update` upgrades if a stale/broken global tool is already registered;
    # `install` covers the not-yet-installed case. Try install, fall through
    # to update on the "already installed" error rather than treating it fatal.
    & $dotnet tool install --global microsoft.sqlpackage 2>&1 | ForEach-Object { Write-Host "    $_" }
    $installExit = $LASTEXITCODE
    if ($installExit -ne 0) {
        Write-Step 'install reported non-zero (likely already registered) -- trying `dotnet tool update`'
        & $dotnet tool update --global microsoft.sqlpackage 2>&1 | ForEach-Object { Write-Host "    $_" }
    }
    # The global-tools dir is already prepended to PATH above; re-resolve.
    $candidate = Resolve-SqlPackage
    if (Test-SqlPackage -Exe $candidate) {
        Write-Ok "sqlpackage installed via dotnet tool -- $candidate"
        exit 0
    }
    Write-Warn 'dotnet-tool install did not yield a runnable sqlpackage -- falling back to the published zip'
} else {
    Write-Step 'dotnet not on PATH -- using the published-zip install route'
}

# -- 3. Fallback route: self-contained Windows zip -> C:\Tools\sqlpackage.
New-Item -ItemType Directory -Path 'C:\Tools'   -Force | Out-Null
New-Item -ItemType Directory -Path $INSTALL_DIR -Force | Out-Null
New-Item -ItemType Directory -Path $CACHE_DIR   -Force | Out-Null

Write-Step 'downloading sqlpackage (latest) from aka.ms redirect'
try {
    Invoke-WebRequest -Uri $SQLPACKAGE_URL -OutFile $DOWNLOAD_ZIP -UseBasicParsing
} catch {
    Stop-Run "download failed: $($_.Exception.Message)"
}
$downloadedSize = (Get-Item $DOWNLOAD_ZIP).Length
$downloadedSha  = (Get-FileHash $DOWNLOAD_ZIP -Algorithm SHA256).Hash.ToLower()
Write-Ok ("downloaded {0} bytes; sha256={1}" -f $downloadedSize, $downloadedSha)

# Clean-extract: wipe first so a stale partial unpack can't shadow new contents.
Write-Step "extracting to $INSTALL_DIR"
Get-ChildItem $INSTALL_DIR -Force | Remove-Item -Recurse -Force -ErrorAction SilentlyContinue
try {
    Expand-Archive -Path $DOWNLOAD_ZIP -DestinationPath $INSTALL_DIR -Force
} catch {
    Stop-Run "extract failed: $($_.Exception.Message)"
}
if (-not (Test-Path $INSTALL_EXE)) {
    Stop-Run "extract reported success but $INSTALL_EXE missing"
}

# Final verification -- a failure here means something fundamental (missing
# .NET runtime, arch mismatch) and we'd rather catch it now than mid-publish.
if (-not (Test-SqlPackage -Exe $INSTALL_EXE)) {
    Stop-Run "sqlpackage /version failed after zip install ($INSTALL_EXE)"
}
Write-Ok "sqlpackage installed (published zip) at $INSTALL_EXE"

Remove-Item $DOWNLOAD_ZIP -Force -ErrorAction SilentlyContinue
