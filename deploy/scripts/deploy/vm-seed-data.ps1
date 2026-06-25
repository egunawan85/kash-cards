# deploy/scripts/deploy/vm-seed-data.ps1
# Builds and runs QryptoCard.DevSeed against the on-box SQL Express to insert the
# minimal master/config rows + the seedable admin + the pre-seeded smoke API user,
# and emits the smoke credentials. Runs ON the VM after vm-publish-schema. Idempotent
# (DevSeed deletes its own seed identities before inserting).
param(
    [string]$SourceDir  = 'C:\src\kash-cards',
    [string]$DbName     = 'qrypto-card',
    [Parameter(Mandatory = $true)][string]$KvName,   # Key Vault holding DBKEY/APPKEY
    [string]$SmokeOut   = 'C:\src\kash-cards\deploy\secrets\.smoke.env'
)
$ErrorActionPreference = 'Stop'
function Step($m) { Write-Host "[..] $m" }
function Ok($m)   { Write-Host "[ok] $m" }
function Die($m)  { Write-Host "[xx] $m"; exit 1 }

# DBKEY/APPKEY encrypt the seeded passwords. This script runs via run-command as
# SYSTEM with a clean environment — the per-app-pool env vars inject-secrets writes do
# NOT apply here — so pull them from Key Vault via the VM managed identity and set them
# on THIS process only. Values are never logged.
& az login --identity 2>&1 | Out-Null
if ($LASTEXITCODE -ne 0) { Die 'az login --identity failed (VM managed identity / az CLI on PATH?)' }
foreach ($k in 'DBKEY', 'APPKEY') {
    $v = (& az keyvault secret show --vault-name $KvName --name $k --query value -o tsv 2>$null)
    if ([string]::IsNullOrWhiteSpace($v)) { Die "$k not found in Key Vault $KvName -- seed it (seed-kv-secrets.sh) and re-run" }
    [Environment]::SetEnvironmentVariable($k, $v, 'Process')
}

$sec  = Join-Path $SourceDir 'QryptoCard.Sec\QryptoCard.Sec.csproj'
$seed = Join-Path $SourceDir 'QryptoCard.DevSeed\QryptoCard.DevSeed.csproj'
if (-not (Test-Path $seed)) { Die "DevSeed project not found at $seed (run vm-fetch-source first)" }

# Build Sec (DevSeed references the built DLL), then DevSeed.
$msbuild = & "${env:ProgramFiles(x86)}\Microsoft Visual Studio\Installer\vswhere.exe" -latest `
    -requires Microsoft.Component.MSBuild -find 'MSBuild\**\Bin\MSBuild.exe' | Select-Object -First 1
if (-not $msbuild) { Die 'MSBuild not found (install VS Build Tools via vm-bootstrap)' }

Step 'building QryptoCard.Sec'
& $msbuild $sec /p:Configuration=Debug /v:minimal /nologo
if ($LASTEXITCODE -ne 0) { Die 'Sec build failed' }

# Build with full MSBuild, NOT `dotnet build`: DevSeed is SDK-style net48 but references
# the legacy (packages.config) QryptoCard.Sec, which the .NET SDK build cannot resolve
# (CS0234). MSBuild 17 builds the SDK-style project + the legacy ProjectReference together.
# /t:Restore restores DevSeed's PackageReferences.
Step 'building QryptoCard.DevSeed (MSBuild)'
& $msbuild $seed /t:Restore,Build /p:Configuration=Debug /v:minimal /nologo
if ($LASTEXITCODE -ne 0) { Die 'DevSeed build failed' }

$exe  = Join-Path $SourceDir 'QryptoCard.DevSeed\bin\Debug\net48\QryptoCard.DevSeed.exe'
$conn = "Server=localhost\SQLEXPRESS;Database=$DbName;Integrated Security=True;TrustServerCertificate=True"

Step "seeding $DbName"
& $exe $conn $SmokeOut
if ($LASTEXITCODE -ne 0) { Die 'seed run failed' }
Ok "seed complete; smoke credentials at $SmokeOut"
