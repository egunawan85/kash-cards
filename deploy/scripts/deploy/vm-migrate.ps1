# vm-migrate.ps1 -- bring the on-box SQL Express database to the current schema using a
# baseline + ordered, run-once migrations, then grant the least-priv app login. SCHEMA
# ONLY: this script creates/updates tables; it never seeds data rows (vm-seed.ps1 owns that).
#
# DESIGN -- why this is not a dacpac re-publish:
#   The declarative dacpac (deploy/sql/kashnow-schema.dacpac) is a desired-state snapshot
#   exported from the canonical DB. Publishing it diffs the live DB against that snapshot
#   and renovates the DB to MATCH it -- including DROPping anything not in the snapshot and
#   ALTERing columns back to the snapshot's shape. That is unsafe here for two reasons the
#   migrations below intentionally diverge from the snapshot:
#     1. The lockout columns (FailureCount/LockoutEnd, migrations 0005/0006) are added by
#        migration, NOT in the dacpac -- a re-publish would DROP them (and BlockOnPossible-
#        DataLoss then aborts the whole publish, so a second `schema` run could never run).
#     2. TXID and Address are deliberately NARROWED to indexable widths (migrations 0002/
#        0003) so the money-safety unique indexes (deposit dedup, one-address-per-owner) can
#        exist -- the dacpac has them as nvarchar(max), and a re-publish would try to widen
#        them back, breaking those indexes (a max column cannot be an index key).
#   So the dacpac is used ONLY as a one-time BASELINE for a brand-new empty database. Every
#   change after the baseline -- including everything above -- is an ordered, run-once
#   migration recorded in dbo.SchemaMigrations. An already-provisioned box is NEVER diffed
#   against the dacpac again; it just runs whatever migrations it has not yet recorded. This
#   makes `schema` idempotent and fail-safe by construction, not by luck.
#
# What it does:
#   1. Resolve sqlpackage (only needed for a fresh-box baseline) + sqlcmd.
#   2. Load DB_NAME / DB_APP_LOGIN from deploy/config/.env.provision.<env>.
#   3. Decide fresh-vs-existing: a DB that does not exist (or exists with zero user tables)
#      is FRESH -> publish the dacpac baseline once. A DB that already has user tables is
#      ADOPTED -> skip the dacpac entirely.
#   4. Ensure dbo.SchemaMigrations exists; record the baseline marker (0000-baseline-dacpac).
#   5. Apply every deploy/sql/migrations/*.sql not yet in the ledger, in filename order;
#      record each on success. Migrations are internally IF-NOT-EXISTS guarded, so the very
#      first run on an already-provisioned box replays them as no-ops and just populates the
#      ledger (self-healing adoption -- no manual baselining).
#   6. Ensure DB_APP_LOGIN exists and has db_datareader/db_datawriter + EXECUTE on [DB_NAME].
#
# Connection: integrated auth (the operator/run-command identity is a SQL Express sysadmin
# on the dev box), server "localhost\SQLEXPRESS". The app login (kash_app) is a SEPARATE
# least-privilege SQL login whose password lives in Key Vault; this script only grants it
# rights, it does not set its password (vm-bootstrap / seed handles credential material).
#
# Invocation:
#   az vm run-command invoke `
#     --resource-group rg-kash-dev --name vm-kash-dev `
#     --command-id RunPowerShellScript `
#     --scripts @deploy/scripts/deploy/vm-migrate.ps1
#   # or over RDP:
#   powershell -NoProfile -ExecutionPolicy Bypass -File vm-migrate.ps1 -Env dev

[CmdletBinding()]
param(
    # Selects deploy/config/.env.provision.<env>. Mandatory-with-default 'dev':
    # the dev box is the only target today, but naming it explicitly keeps the
    # config-source path unambiguous if stg/prd are added later.
    [ValidateSet('dev','stg','prd')]
    [string]$Env = 'dev',

    # SQL Express server. Default matches SQL_INSTANCE=SQLEXPRESS in config.
    [string]$DbServer = 'localhost\SQLEXPRESS',

    # Override the target database (default: DB_NAME from config). Intended for
    # validation against a throwaway scratch DB (e.g. exercise the fresh-box baseline
    # path on an empty copy) without touching the live database. Shape-guarded below.
    [string]$DbName = ''
)

$ErrorActionPreference = 'Stop'
$ProgressPreference    = 'SilentlyContinue'
[Console]::OutputEncoding = [System.Text.Encoding]::UTF8

function Write-Step { param([string]$m) Write-Host "[..] $m" }
function Write-Ok   { param([string]$m) Write-Host "[ok] $m" }
function Write-Warn { param([string]$m) Write-Host "[!!] $m" -ForegroundColor Yellow }
function Stop-Run   { param([string]$m) Write-Host "[xx] $m" -ForegroundColor Red; exit 1 }

# Any UNHANDLED terminating error must still emit the [xx] marker the deploy.sh wrapper
# scans for. `az vm run-command` exits 0 even when this script dies, and
# $ErrorActionPreference='Stop' makes every cmdlet failure terminating -- so without this
# trap a crash that never reaches a Stop-Run (e.g. an unexpected cast/Get-Content failure)
# would print a markerless .NET error and be read as SUCCESS, letting a later app deploy
# proceed against an un-migrated DB. The trap closes that gap; deliberate Stop-Run exits
# are normal (non-terminating) and do not re-trigger it.
trap { Write-Host "[xx] unhandled error: $($_.Exception.Message)" -ForegroundColor Red; exit 1 }

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
$ConfigFile     = Join-Path $DeployRoot "config\.env.provision.$Env"
$DacpacFile     = Join-Path $DeployRoot 'sql\kashnow-schema.dacpac'
$MigrationsDir  = Join-Path $DeployRoot 'sql\migrations'

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
# Target DB: the -DbName param (scratch/validation override) wins; otherwise config's
# DB_NAME. Shape-guard either way -- $DbName is spliced into N'...' literals (DB_ID etc.)
# and passed as the sqlcmd -d argument, so it must not contain quotes/brackets/semicolons.
if (-not $DbName) { $DbName = $cfg['DB_NAME'] }
$AppLogin = $cfg['DB_APP_LOGIN']
if (-not $DbName)   { Stop-Run "DB_NAME missing from $ConfigFile (and no -DbName override given)" }
if (-not $AppLogin) { Stop-Run "DB_APP_LOGIN missing from $ConfigFile" }
if ($DbName -notmatch '^[A-Za-z0-9_-]+$') {
    Stop-Run "DB_NAME '$DbName' has an unexpected shape -- refusing to splice into T-SQL / sqlcmd args"
}
# Guard the login name against injection into the dynamic GRANT T-SQL below
# (we cannot parameterize a principal name). Mirrors the sister's name checks.
if ($AppLogin -notmatch '^[A-Za-z_][A-Za-z0-9_]{0,127}$') {
    Stop-Run "DB_APP_LOGIN '$AppLogin' has an unexpected shape -- refusing to splice into GRANT T-SQL"
}
Write-Ok "config: DB_NAME=$DbName  DB_APP_LOGIN=$AppLogin  (env=$Env)"

# -- Locate sqlcmd (always needed) and sqlpackage (only needed for a fresh baseline).
$sqlcmd = (Get-Command sqlcmd -ErrorAction SilentlyContinue).Source
if (-not $sqlcmd -and (Test-Path 'C:\Tools\sqlcmd.exe')) { $sqlcmd = 'C:\Tools\sqlcmd.exe' }  # vm-bootstrap installs go-sqlcmd here (not on the SYSTEM PATH)
if (-not $sqlcmd) { Stop-Run 'sqlcmd not found on PATH or C:\Tools\sqlcmd.exe -- vm-bootstrap installs go-sqlcmd there' }

# Canonical sqlpackage install paths are the dotnet global-tools dir and
# C:\Tools\sqlpackage (see vm-install-sqlpackage.ps1); PATH covers operators who
# installed elsewhere. Resolved lazily -- an adopted box never needs it.
function Resolve-SqlPackage {
    foreach ($d in @((Join-Path $env:USERPROFILE '.dotnet\tools'), 'C:\Tools\sqlpackage')) {
        if (($env:PATH -split ';') -notcontains $d) { $env:PATH = "$d;$env:PATH" }
    }
    $sp = (Get-Command sqlpackage -ErrorAction SilentlyContinue).Source
    if (-not $sp) {
        foreach ($c in @('C:\Tools\sqlpackage\sqlpackage.exe',
                         'C:\Program Files\Microsoft SQL Server\170\DAC\bin\SqlPackage.exe',
                         'C:\Program Files\Microsoft SQL Server\160\DAC\bin\SqlPackage.exe',
                         'C:\Program Files\Microsoft SQL Server\150\DAC\bin\SqlPackage.exe')) {
            if (Test-Path $c) { $sp = $c; break }
        }
    }
    return $sp
}

if (-not (Test-Path $MigrationsDir)) { Stop-Run "migrations dir not found: $MigrationsDir" }
$Migrations = @(Get-ChildItem -LiteralPath $MigrationsDir -Filter '*.sql' -File | Sort-Object Name)
# Migration filenames are committed and controlled, but we splice each into an INSERT
# (ledger id) and an existence check -- validate the shape so it can never break out of
# the N'...' literal (defense-in-depth; all current names are 0001-token-tables.sql etc.).
foreach ($m in $Migrations) {
    if ($m.Name -notmatch '^[A-Za-z0-9._-]+$') {
        Stop-Run "migration filename '$($m.Name)' has an unexpected shape -- refusing to splice into ledger SQL"
    }
}

# Integrated-auth sqlcmd arg sets. -C trusts SQL Express's self-signed cert; -E uses the
# current Windows identity; -b makes sqlcmd return non-zero on a T-SQL error so
# $LASTEXITCODE is real. master for the existence probe, $DbName for everything else.
$masterArgs = @('-S', $DbServer, '-E', '-C', '-l', '15', '-b', '-d', 'master')
$dbArgs     = @('-S', $DbServer, '-E', '-C', '-l', '15', '-b', '-d', $DbName)

function Invoke-Scalar {
    param([string[]]$BaseArgs, [string]$Query)
    $out = & $sqlcmd @BaseArgs -h -1 -W -Q "SET NOCOUNT ON; $Query" 2>&1
    if ($LASTEXITCODE -ne 0) { Stop-Run "sqlcmd query failed (exit $LASTEXITCODE): $Query`n$out" }
    $lines = @($out | Where-Object { $_ -and ($_.ToString().Trim() -ne '') })
    if ($lines.Count -eq 0) { return '' }
    return ($lines[-1].ToString().Trim())
}

# -- 1. Decide fresh-vs-existing. A DB that does not exist, or exists with zero user
# tables, is FRESH (needs the baseline). A DB that already has user tables is ADOPTED
# (already provisioned -- never re-diff it against the dacpac).
$dbExists = Invoke-Scalar -BaseArgs $masterArgs -Query `
    "SELECT CASE WHEN DB_ID(N'$DbName') IS NULL THEN 0 ELSE 1 END"
$isFresh = $true
if ($dbExists -eq '1') {
    # "Provisioned" = the baseline's core identity table exists. Using a baseline SENTINEL
    # (dbo.tblM_User) rather than a generic sys.tables count means our own dbo.SchemaMigrations
    # ledger -- or any stray operator-created table -- can never tip an unprovisioned DB into the
    # adopt path; and a half-published baseline missing core tables correctly re-runs the baseline
    # (CreateNewDatabase=False updates in place). tblM_User is always present after a real baseline.
    $hasBaseline = Invoke-Scalar -BaseArgs $dbArgs -Query "SELECT CASE WHEN OBJECT_ID(N'dbo.tblM_User') IS NULL THEN 0 ELSE 1 END"
    if ($hasBaseline -notmatch '^[01]$') { Stop-Run "baseline-sentinel probe returned an unexpected result: '$hasBaseline'" }
    if ($hasBaseline -eq '1') { $isFresh = $false }
    Write-Ok "[$DbName] exists; baseline sentinel dbo.tblM_User $(if ($isFresh) {'ABSENT -> FRESH (empty/partial)'} else {'present -> ADOPT (already provisioned)'})"
} else {
    Write-Ok "[$DbName] does not exist -> FRESH (will create from baseline)"
}

# -- 2. Fresh only: publish the dacpac baseline ONCE. CreateNewDatabase=False creates the
# DB if absent and updates in place if present; BlockOnPossibleDataLoss=True is a belt-and-
# suspenders net (an empty DB has nothing to lose). This is the ONLY time the dacpac runs.
if ($isFresh) {
    if (-not (Test-Path $DacpacFile)) { Stop-Run "DACPAC baseline not found: $DacpacFile" }
    $sqlpackage = Resolve-SqlPackage
    if (-not $sqlpackage) { Stop-Run 'sqlpackage not found -- run vm-install-sqlpackage.ps1 first (needed for the fresh-box baseline)' }
    Write-Ok "sqlpackage: $sqlpackage"
    Write-Step "baseline: sqlpackage /Action:Publish -> [$DbName] on $DbServer"
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
    if ($LASTEXITCODE -ne 0) { Stop-Run "baseline publish failed (exit $LASTEXITCODE)" }
    Write-Ok ("baseline published in {0:N0} s" -f ((Get-Date) - $start).TotalSeconds)

    $state = Invoke-Scalar -BaseArgs $masterArgs -Query `
        "SELECT ISNULL((SELECT state_desc FROM sys.databases WHERE name = N'$DbName'), 'MISSING')"
    if ($state -ne 'ONLINE') { Stop-Run "[$DbName] state is '$state' after baseline, expected ONLINE" }
    Write-Ok "[$DbName] is ONLINE"
}

# -- S6b: BACK UP the database before vm-migrate changes anything, so a bad migration (or a bad
# `update --with-schema`) is recoverable. Only for an already-provisioned (non-fresh) DB -- a
# fresh baseline has nothing worth saving. Writes to the SQL instance's DEFAULT backup directory
# (a RELATIVE filename, which the SQL service account can always write -- no ACL juggling), then
# prunes to the 5 most recent for this DB. SQL Express supports BACKUP DATABASE (no COMPRESSION).
if (-not $isFresh) {
    $bakStamp = Get-Date -Format 'yyyyMMdd-HHmmss'
    $bakName  = "$DbName-premigrate-$bakStamp.bak"
    Write-Step "DB backup before migrate: BACKUP DATABASE [$DbName] -> $bakName (default backup dir)"
    & $sqlcmd @dbArgs -Q "BACKUP DATABASE [$DbName] TO DISK = N'$bakName' WITH INIT, FORMAT, NAME = N'$DbName pre-migrate $bakStamp';"
    if ($LASTEXITCODE -ne 0) { Stop-Run "DB backup failed (exit $LASTEXITCODE) -- refusing to migrate without a recoverable backup" }
    Write-Ok "DB backed up: $bakName"
    $bakDir = Invoke-Scalar -BaseArgs $masterArgs -Query "SELECT CAST(SERVERPROPERTY('InstanceDefaultBackupPath') AS nvarchar(4000))"
    if ($bakDir -and (Test-Path -LiteralPath $bakDir)) {
        Get-ChildItem -LiteralPath $bakDir -Filter "$DbName-premigrate-*.bak" -ErrorAction SilentlyContinue |
            Sort-Object LastWriteTime -Descending | Select-Object -Skip 5 |
            Remove-Item -Force -ErrorAction SilentlyContinue
    } else {
        Write-Ok "  (backup prune skipped: default backup dir not resolvable on this SQL version)"
    }
}

# -- 3. Ensure the migrations ledger exists, then record the baseline marker. On an adopted
# box this records 0000-baseline-dacpac WITHOUT having run the dacpac (the box already has
# the baseline schema from a prior provision).
Write-Step "ensuring dbo.SchemaMigrations ledger on [$DbName]"
$ledgerSql = @"
SET NOCOUNT ON;
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = N'SchemaMigrations' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    CREATE TABLE dbo.SchemaMigrations (
        MigrationId nvarchar(260) NOT NULL CONSTRAINT PK_SchemaMigrations PRIMARY KEY,
        AppliedUtc  datetime2     NOT NULL CONSTRAINT DF_SchemaMigrations_AppliedUtc DEFAULT SYSUTCDATETIME()
    );
END
IF NOT EXISTS (SELECT 1 FROM dbo.SchemaMigrations WHERE MigrationId = N'0000-baseline-dacpac')
    BEGIN TRY INSERT INTO dbo.SchemaMigrations (MigrationId) VALUES (N'0000-baseline-dacpac'); END TRY
    BEGIN CATCH IF ERROR_NUMBER() <> 2627 THROW; END CATCH; -- 2627 = a concurrent run beat us; benign
"@
& $sqlcmd @dbArgs -Q $ledgerSql
if ($LASTEXITCODE -ne 0) { Stop-Run "ensure-ledger step failed (exit $LASTEXITCODE)" }
Write-Ok "ledger ready (baseline recorded)"

# -- 4. Apply every migration not yet recorded, in filename order. Each migration is
# internally IF-NOT-EXISTS guarded and GO-batched; -b fails the script on a real error.
# We apply, then record -- if a crash lands between the two, the next run re-applies (a
# guarded no-op) and records, so the ledger can never permanently skip an unapplied change.
$applied = 0; $skipped = 0
foreach ($m in $Migrations) {
    $id = $m.Name
    $already = Invoke-Scalar -BaseArgs $dbArgs -Query `
        "SELECT COUNT(*) FROM dbo.SchemaMigrations WHERE MigrationId = N'$id'"
    if ($already -notmatch '^\d+$') { Stop-Run "ledger-count probe for ${id} returned a non-numeric result: '$already'" }
    if ([int]$already -gt 0) {
        Write-Ok "skip (already applied): $id"
        $skipped++
        continue
    }
    Write-Step "applying migration: $id"
    & $sqlcmd @dbArgs -i $m.FullName
    if ($LASTEXITCODE -ne 0) { Stop-Run "migration failed ($id, exit $LASTEXITCODE)" }
    # Record the apply. The TRY/CATCH swallows a PK-duplicate (2627) so two overlapping
    # runs cannot turn a benign "already recorded by the other run" into a hard abort; any
    # other error re-throws -> sqlcmd -b -> non-zero -> Stop-Run.
    & $sqlcmd @dbArgs -Q "SET NOCOUNT ON; BEGIN TRY INSERT INTO dbo.SchemaMigrations (MigrationId) VALUES (N'$id'); END TRY BEGIN CATCH IF ERROR_NUMBER() <> 2627 THROW; END CATCH;"
    if ($LASTEXITCODE -ne 0) { Stop-Run "recording migration failed ($id, exit $LASTEXITCODE)" }
    Write-Ok "$id applied + recorded"
    $applied++
}
Write-Ok "migrations: $applied applied, $skipped already-current ($($Migrations.Count) total)"

# -- 5. Ensure the least-priv app login can use the DB. Idempotent: create the server login
# only if absent (no password set here -- credential material is owned by the bootstrap/seed
# step), map a DB user, and (re)grant role membership + EXECUTE. Name shape-validated above.
Write-Step "ensuring [$AppLogin] has rights on [$DbName]"
$grantSql = @"
SET NOCOUNT ON;
IF NOT EXISTS (SELECT 1 FROM sys.server_principals WHERE name = N'$AppLogin')
BEGIN
    PRINT 'login [$AppLogin] absent -- creating placeholder (password owned by bootstrap/seed)';
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
Write-Ok "schema migrate complete for [$DbName] (schema only -- no data seeded)"
