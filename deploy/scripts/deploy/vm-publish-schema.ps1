# vm-publish-schema.ps1 -- publish the schema-only DACPAC to the on-box SQL
# Express instance, then apply the bearer-token DDL and grant the least-priv
# app login. SCHEMA ONLY: this script creates/updates tables; it never seeds
# data rows (a separate C# seeder owns that). Safe to re-run -- sqlpackage
# Publish is an incremental diff, the token DDL is IF-NOT-EXISTS guarded, and
# the login/user grant is existence-checked.
#
# Adapted from runegate-infra/scripts/deploy/vm-import-bacpac.ps1. Differences
# from the sister: that script IMPORTS a data-bearing .bacpac into a side-by-
# side staging DB and runs a swap; here we PUBLISH a schema-only .dacpac
# directly onto the target DB on a fresh box (incremental, in place), with no
# staging/swap and no data smoke-check (there are no rows yet).
#
# What it does:
#   1. Resolve sqlpackage (PATH / dotnet-tools / C:\Tools\sqlpackage).
#   2. Resolve sqlcmd (for the token DDL + the grant).
#   3. Load DB_NAME / DB_APP_LOGIN from deploy/config/.env.provision.<env>.
#   4. sqlpackage /Action:Publish the DACPAC -> [DB_NAME] (creates 38 tables on
#      a fresh DB; incremental diff on re-run).
#   5. Apply the additive deploy/sql DDL scripts in order (bearer-token tables +
#      prepaid-balance uniqueness/webhook-dedup indexes; all IF-NOT-EXISTS guarded).
#   6. Ensure DB_APP_LOGIN exists and has db_datareader/db_datawriter +
#      EXECUTE on [DB_NAME].
#
# Connection: integrated auth (the operator/run-command identity is a SQL
# Express sysadmin on the dev box), server "localhost\SQLEXPRESS". The app
# login (kash_app) is a SEPARATE least-privilege SQL login whose password
# lives in Key Vault as DB_PASSWORD; this script only grants it rights, it
# does not set its password (vm-bootstrap / seed handles credential material).
#
# Invocation:
#   az vm run-command invoke `
#     --resource-group rg-kash-dev --name vm-kash-dev `
#     --command-id RunPowerShellScript `
#     --scripts @deploy/scripts/deploy/vm-publish-schema.ps1
#   # or over RDP:
#   powershell -NoProfile -ExecutionPolicy Bypass -File vm-publish-schema.ps1 -Env dev

[CmdletBinding()]
param(
    # Selects deploy/config/.env.provision.<env>. Mandatory-with-default 'dev':
    # the dev box is the only target today, but naming it explicitly keeps the
    # config-source path unambiguous if stg/prd are added later.
    [ValidateSet('dev','stg','prd')]
    [string]$Env = 'dev',

    # SQL Express server. Default matches SQL_INSTANCE=SQLEXPRESS in config.
    [string]$DbServer = 'localhost\SQLEXPRESS'
)

$ErrorActionPreference = 'Stop'
$ProgressPreference    = 'SilentlyContinue'
[Console]::OutputEncoding = [System.Text.Encoding]::UTF8

function Write-Step { param([string]$m) Write-Host "[..] $m" }
function Write-Ok   { param([string]$m) Write-Host "[ok] $m" }
function Write-Warn { param([string]$m) Write-Host "[!!] $m" -ForegroundColor Yellow }
function Stop-Run   { param([string]$m) Write-Host "[xx] $m" -ForegroundColor Red; exit 1 }

# -- Resolve repo paths relative to this script (deploy/scripts/deploy/...).
$ScriptDir   = $PSScriptRoot
# From the repo, deploy root is two dirs up. When sent detached via `az vm run-command`
# ($PSScriptRoot is a temp dir), fall back to the fixed clone location.
$candidate   = Join-Path $ScriptDir '..\..'
if (Test-Path (Join-Path $candidate 'sql\kashnow-schema.dacpac')) {
    $DeployRoot = (Resolve-Path $candidate).Path
} else {
    $DeployRoot = 'C:\src\kash-cards\deploy'
}
$ConfigFile  = Join-Path $DeployRoot "config\.env.provision.$Env"
$DacpacFile  = Join-Path $DeployRoot 'sql\kashnow-schema.dacpac'
# Additive, idempotent DDL applied (in order) after the DACPAC publish. Each file is
# internally IF-NOT-EXISTS / guarded so re-runs are safe. create-token-tables.sql stays
# first; the prepaid-balance index scripts only touch tables the DACPAC already creates.
$DdlScriptNames = @(
    'create-token-tables.sql',
    'create-wallet-indexes.sql',
    'create-webhook-dedup-index.sql',
    'create-referral-commission-dedup-index.sql',
    'create-otp-lockout-columns.sql',
    'create-login-lockout-columns.sql'
)
$DdlScripts  = $DdlScriptNames | ForEach-Object { Join-Path $DeployRoot "sql\$_" }

# -- Load DB_NAME / DB_APP_LOGIN from the provision config. The file is plain
# KEY=VALUE (same format load-env.ps1 parses); we only need two keys, so parse
# narrowly rather than dot-sourcing shell syntax into PowerShell.
if (-not (Test-Path $ConfigFile)) {
    Stop-Run "config not found: $ConfigFile (copy .env.provision.$Env.example -> .env.provision.$Env)"
}
$cfg = @{}
foreach ($line in Get-Content -LiteralPath $ConfigFile) {
    $t = $line.Trim()
    if ($t -eq '' -or $t.StartsWith('#')) { continue }
    $idx = $t.IndexOf('=')
    if ($idx -lt 1) { continue }
    $k = $t.Substring(0, $idx).Trim()
    $v = $t.Substring($idx + 1).Trim()
    # Strip an inline comment + surrounding quotes (config uses both styles).
    $v = ($v -replace '\s+#.*$', '').Trim()
    if ($v.Length -ge 2 -and (($v[0] -eq '"' -and $v[-1] -eq '"') -or ($v[0] -eq "'" -and $v[-1] -eq "'"))) {
        $v = $v.Substring(1, $v.Length - 2)
    }
    $cfg[$k] = $v
}
$DbName   = $cfg['DB_NAME']
$AppLogin = $cfg['DB_APP_LOGIN']
if (-not $DbName)   { Stop-Run "DB_NAME missing from $ConfigFile" }
if (-not $AppLogin) { Stop-Run "DB_APP_LOGIN missing from $ConfigFile" }
# Guard the login name against injection into the dynamic GRANT T-SQL below
# (we cannot parameterize a principal name). Mirrors the sister's name checks.
if ($AppLogin -notmatch '^[A-Za-z_][A-Za-z0-9_]{0,127}$') {
    Stop-Run "DB_APP_LOGIN '$AppLogin' has an unexpected shape -- refusing to splice into GRANT T-SQL"
}
Write-Ok "config: DB_NAME=$DbName  DB_APP_LOGIN=$AppLogin  (env=$Env)"

# -- Locate sqlpackage. Canonical install paths are the dotnet global-tools
# dir and C:\Tools\sqlpackage (see vm-install-sqlpackage.ps1); PATH covers
# operators who installed elsewhere.
foreach ($d in @((Join-Path $env:USERPROFILE '.dotnet\tools'), 'C:\Tools\sqlpackage')) {
    if (($env:PATH -split ';') -notcontains $d) { $env:PATH = "$d;$env:PATH" }
}
$sqlpackage = (Get-Command sqlpackage -ErrorAction SilentlyContinue).Source
if (-not $sqlpackage) {
    foreach ($candidate in @('C:\Tools\sqlpackage\sqlpackage.exe',
                             'C:\Program Files\Microsoft SQL Server\170\DAC\bin\SqlPackage.exe',
                             'C:\Program Files\Microsoft SQL Server\160\DAC\bin\SqlPackage.exe',
                             'C:\Program Files\Microsoft SQL Server\150\DAC\bin\SqlPackage.exe')) {
        if (Test-Path $candidate) { $sqlpackage = $candidate; break }
    }
}
if (-not $sqlpackage) {
    Stop-Run 'sqlpackage not found -- run vm-install-sqlpackage.ps1 first'
}
Write-Ok "sqlpackage: $sqlpackage"

$sqlcmd = (Get-Command sqlcmd -ErrorAction SilentlyContinue).Source
if (-not $sqlcmd -and (Test-Path 'C:\Tools\sqlcmd.exe')) { $sqlcmd = 'C:\Tools\sqlcmd.exe' }  # vm-bootstrap installs go-sqlcmd here (not on the SYSTEM PATH)
if (-not $sqlcmd) { Stop-Run 'sqlcmd not found on PATH or C:\Tools\sqlcmd.exe -- vm-bootstrap installs go-sqlcmd there' }

if (-not (Test-Path $DacpacFile)) { Stop-Run "DACPAC not found: $DacpacFile" }
foreach ($ddl in $DdlScripts) {
    if (-not (Test-Path $ddl)) { Stop-Run "DDL script not found: $ddl" }
}
Write-Ok "dacpac: $DacpacFile"

# Integrated-auth sqlcmd base args. -C trusts SQL Express's self-signed cert
# (same pattern as the sister's -C usage); -E uses the current Windows identity;
# -b makes sqlcmd return a non-zero exit on T-SQL error so $LASTEXITCODE is real.
$masterArgs = @('-S', $DbServer, '-E', '-C', '-l', '15', '-b', '-d', 'master')

function Invoke-Scalar {
    param([string[]]$BaseArgs, [string]$Query)
    $out = & $sqlcmd @BaseArgs -h -1 -W -Q "SET NOCOUNT ON; $Query" 2>&1
    if ($LASTEXITCODE -ne 0) { Stop-Run "sqlcmd query failed (exit $LASTEXITCODE): $Query`n$out" }
    $lines = @($out | Where-Object { $_ -and ($_.ToString().Trim() -ne '') })
    if ($lines.Count -eq 0) { return '' }
    return ($lines[-1].ToString().Trim())
}

# -- 1. Publish the schema-only DACPAC. Publish is incremental: on a fresh DB
# it creates all 38 tables; on re-run it diffs and applies only changes.
#   /p:CreateNewDatabase=False         -> update in place if the DB exists
#   /p:BlockOnPossibleDataLoss=True    -> safety: never silently drop/alter
#                                         a column in a way that loses data
#   /p:CommandTimeout=0                -> no per-statement timeout
#   /TargetTrustServerCertificate:True -> self-signed SQL Express TLS cert
Write-Step "sqlpackage /Action:Publish -> [$DbName] on $DbServer"
$publishArgs = @(
    '/Action:Publish',
    "/SourceFile:$DacpacFile",
    "/TargetServerName:$DbServer",
    "/TargetDatabaseName:$DbName",
    '/TargetTrustServerCertificate:True',
    '/p:CreateNewDatabase=False',
    '/p:BlockOnPossibleDataLoss=True',
    '/p:CommandTimeout=0'
)
$start = Get-Date
& $sqlpackage @publishArgs
$publishExit = $LASTEXITCODE
if ($publishExit -ne 0) {
    Stop-Run "sqlpackage Publish failed (exit $publishExit)"
}
Write-Ok ("schema published in {0:N0} s" -f ((Get-Date) - $start).TotalSeconds)

# Sanity: confirm the DB now exists and is ONLINE before we touch it further.
$state = Invoke-Scalar -BaseArgs $masterArgs -Query `
    "SELECT ISNULL((SELECT state_desc FROM sys.databases WHERE name = N'$DbName'), 'MISSING')"
if ($state -ne 'ONLINE') { Stop-Run "[$DbName] state is '$state' after publish, expected ONLINE" }
Write-Ok "[$DbName] is ONLINE"

# -- 2. Apply the additive DDL scripts in order. Each is internally IF-NOT-EXISTS
# guarded and GO-batched, so it is safe to re-run; -b means any real error (not a
# benign already-exists) still fails the script. This includes the prepaid-balance
# uniqueness + webhook-dedup indexes the wallet code relies on for race-safety and
# replay protection (without them, replayed deposit webhooks can double-credit).
$dbArgs = @('-S', $DbServer, '-E', '-C', '-l', '15', '-b', '-d', $DbName)
foreach ($ddl in $DdlScripts) {
    $leaf = Split-Path -Leaf $ddl
    Write-Step "applying DDL: $leaf"
    & $sqlcmd @dbArgs -i $ddl
    if ($LASTEXITCODE -ne 0) { Stop-Run "DDL failed ($leaf, exit $LASTEXITCODE)" }
    Write-Ok "$leaf applied (idempotent)"
}

# -- 3. Ensure the least-priv app login can use the DB. Idempotent: create the
# server login only if absent (no password set here -- credential material is
# owned by the bootstrap/seed step), map a DB user, and (re)grant role
# membership + EXECUTE. The login name was shape-validated above.
Write-Step "ensuring [$AppLogin] has rights on [$DbName]"
$grantSql = @"
SET NOCOUNT ON;
IF NOT EXISTS (SELECT 1 FROM sys.server_principals WHERE name = N'$AppLogin')
BEGIN
    -- Login is expected to be created with its KV password by the bootstrap
    -- step; create a placeholder here only so a standalone schema publish on a
    -- bare box does not fail the grant. The bootstrap step is authoritative for
    -- the password (ALTER LOGIN there sets the real KV value).
    PRINT 'login [$AppLogin] absent -- creating placeholder (password owned by bootstrap/seed)';
    -- NEWID() is not a valid password literal; derive a random one via a variable.
    -- Dynamic SQL also keeps CREATE LOGIN out of batch-parse when the branch is skipped.
    DECLARE @pw nvarchar(64) = CONVERT(nvarchar(36), NEWID());
    EXEC(N'CREATE LOGIN [$AppLogin] WITH PASSWORD = ''' + @pw + N''', CHECK_POLICY = OFF;');
END
"@
& $sqlcmd @masterArgs -Q $grantSql
if ($LASTEXITCODE -ne 0) { Stop-Run "ensure-login step failed (exit $LASTEXITCODE)" }

$userSql = @"
SET NOCOUNT ON;
IF NOT EXISTS (SELECT 1 FROM sys.database_principals WHERE name = N'$AppLogin')
    CREATE USER [$AppLogin] FOR LOGIN [$AppLogin];
ALTER ROLE db_datareader ADD MEMBER [$AppLogin];
ALTER ROLE db_datawriter ADD MEMBER [$AppLogin];
GRANT EXECUTE TO [$AppLogin];
"@
& $sqlcmd @dbArgs -Q $userSql
if ($LASTEXITCODE -ne 0) { Stop-Run "grant-rights step failed (exit $LASTEXITCODE)" }
Write-Ok "[$AppLogin] has db_datareader + db_datawriter + EXECUTE on [$DbName]"

Write-Host ''
Write-Ok "schema publish complete for [$DbName] (schema only -- no data seeded)"
