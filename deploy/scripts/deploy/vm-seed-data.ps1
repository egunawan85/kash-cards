# deploy/scripts/deploy/vm-seed-data.ps1
# Builds and runs QryptoCard.DevSeed against the on-box SQL Express to insert the
# minimal master/config rows + the seedable admin + the pre-seeded smoke API user,
# and emits the smoke credentials. Runs ON the VM after vm-publish-schema. Idempotent
# (DevSeed deletes its own seed identities before inserting).
param(
    [string]$SourceDir  = 'C:\src\kash-cards',
    [string]$DbName     = 'qrypto-card',
    [string]$SmokeOut   = 'C:\src\kash-cards\deploy\secrets\.smoke.env'
)
$ErrorActionPreference = 'Stop'
function Step($m) { Write-Host "[..] $m" }
function Ok($m)   { Write-Host "[ok] $m" }
function Die($m)  { Write-Host "[xx] $m"; exit 1 }

# DBKEY/APPKEY must be in the environment (inject-secrets sets these per pool; for the
# seed run they are read from the process env — pass them in or load from .vault/KV).
foreach ($k in 'DBKEY', 'APPKEY') {
    if (-not [Environment]::GetEnvironmentVariable($k)) { Die "$k not in environment (needed to encrypt seeded passwords)" }
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

Step 'building QryptoCard.DevSeed'
& dotnet build $seed -c Debug -v quiet --nologo
if ($LASTEXITCODE -ne 0) { Die 'DevSeed build failed' }

$exe  = Join-Path $SourceDir 'QryptoCard.DevSeed\bin\Debug\net48\QryptoCard.DevSeed.exe'
$conn = "Server=localhost\SQLEXPRESS;Database=$DbName;Integrated Security=True;TrustServerCertificate=True"

Step "seeding $DbName"
& $exe $conn $SmokeOut
if ($LASTEXITCODE -ne 0) { Die 'seed run failed' }
Ok "seed complete; smoke credentials at $SmokeOut"
