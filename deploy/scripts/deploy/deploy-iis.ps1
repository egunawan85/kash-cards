# deploy-iis.ps1 -- bring the whole kash-cards IIS stack up on one box.
#
# Adapted from runegate-infra/scripts/deploy/vm-deploy-apps.ps1 (the per-app
# build/publish/Web.config-patch driver). Re-shaped from runegate's two-app
# model to kash-cards' single-solution, 12-site layout described in
# deploy/config/sites.json.
#
# What it does, for EACH of the 12 sites in sites.json (8 public + 4 internal):
#   1. Create an IIS app pool (managed runtime v4.0, integrated pipeline).
#   2. Create an IIS site whose ONLY binding is 127.0.0.1:<port>. Every site --
#      public AND internal -- is loopback-bound: the Cloudflare tunnel fronts
#      the public ones, and the 4 internal WCF money tiers must NEVER get a
#      non-loopback binding. There is no public 0.0.0.0/IP binding anywhere.
#   3. NuGet-restore (packages.config) then MSBuild-publish the project to the
#      site root under <PublishRoot>\<project>.
#   4. Public tiers only: rewrite the published Web.config's WCF
#      <client><endpoint address="..."> per sites.json "wcfEndpointRewrites"
#      (string-replace fromHostPort -> toHostPort, e.g. localhost:50434 ->
#      localhost:8091) so the API tier talks to the on-box INT tier, not the
#      old dev-laptop port.
#   5. connectionStringTiers projects only (the 3 INT tiers): rewrite the EF6
#      "DBEntities" connection string to the on-box dev DB. ALL THREE point at
#      the SAME catalog (the committed configs wrongly carried three different
#      catalogs -- qrypto-card / qrypto-card-kashnow / qrypto-card-dev). We
#      rebuild the string from a canonical template rather than patching, so
#      the divergent originals all converge.
#
# Secrets: this script does NOT inject app secrets -- that is inject-secrets.ps1
# (per-pool env vars from Key Vault). The ONLY secret this script touches is the
# DB password, which the EF connection string carries in Web.config (it is NOT
# delivered as an env var). The password is pulled from Key Vault at deploy time
# (secret name DB_PASSWORD) or passed via -DbPassword.
#
# Idempotent: every step is check-then-do (app pool/site created only if absent;
# Web.config rebuilt deterministically from config each run). One command brings
# the whole stack up; re-running converges.
#
# Usage (run as Administrator, or as SYSTEM via 'az vm run-command invoke'):
#   pwsh -File deploy-iis.ps1 [-Env dev] [-DbPassword <pw>] [-RepoRoot <path>]
# The DB password resolves in this order: -DbPassword param, then Key Vault
# (KEYVAULT_NAME from config/.env.provision.<env>).

[CmdletBinding()]
param(
    [ValidateSet('dev', 'stg', 'prd')]
    [string]$Env = 'dev',

    # Repo root holding the .sln and the per-project source dirs. Defaults to
    # three levels up from this script (deploy/scripts/deploy -> repo root).
    [string]$RepoRoot,

    # Where each site's published output lands. One subdir per project.
    [string]$PublishRoot = 'C:\inetpub\kash-cards',

    # DB password for the EF connection string. If omitted, pulled from Key
    # Vault (secret name DB_PASSWORD) using KEYVAULT_NAME from the env file.
    # Passed as a param mainly for local dry-runs; on the VM, prefer KV.
    [string]$DbPassword
)

$ErrorActionPreference = 'Stop'
$ProgressPreference    = 'SilentlyContinue'

# Force UTF-8 for native-command stdout (az, nuget, msbuild). The image's
# default cp1252 console encoding silently drops non-cp1252 bytes from captured
# output -- including a DB password that contains them. Mirrors the sister.
[Console]::OutputEncoding = [System.Text.Encoding]::UTF8
$env:PYTHONIOENCODING     = 'utf-8'

# -- Logging helpers (match the sister / vm-bootstrap.ps1 style) -------------
function Write-Step { param([string]$m) Write-Host "[..] $m" }
function Write-Ok   { param([string]$m) Write-Host "[ok] $m" }
function Write-Warn { param([string]$m) Write-Host "[!!] $m" -ForegroundColor Yellow }
function Stop-Deploy {
    param([string]$m)
    Write-Host "[xx] $m" -ForegroundColor Red
    exit 1
}

# -- Resolve paths -----------------------------------------------------------
if (-not $RepoRoot) {
    # From the repo, repo root is three dirs up. When sent detached via
    # `az vm run-command` ($PSScriptRoot is a temp dir, not the repo), fall back to the
    # fixed clone location vm-fetch-source.ps1 writes to.
    $candidate = Join-Path $PSScriptRoot '..\..\..'
    if (Test-Path (Join-Path $candidate 'deploy\config\sites.json')) {
        $RepoRoot = (Resolve-Path $candidate).Path
    } else {
        $RepoRoot = 'C:\src\kash-cards'
    }
}
$DeployDir  = Join-Path $RepoRoot 'deploy'
$ConfigDir  = Join-Path $DeployDir 'config'
$SitesJson  = Join-Path $ConfigDir 'sites.json'
$EnvFile    = Join-Path $ConfigDir ".env.provision.$Env"

foreach ($p in @($RepoRoot, $DeployDir, $ConfigDir, $SitesJson)) {
    if (-not (Test-Path $p)) { Stop-Deploy "missing required path: $p" }
}
Write-Ok "repo root: $RepoRoot"
Write-Ok "publish root: $PublishRoot"

# -- Preflight: admin (IIS/applicationHost.config writes need it) ------------
$identity  = [Security.Principal.WindowsIdentity]::GetCurrent()
$principal = [Security.Principal.WindowsPrincipal]::new($identity)
if (-not $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) {
    Stop-Deploy "must run as Administrator (or as SYSTEM via 'az vm run-command invoke')"
}
Write-Ok "running as: $($identity.Name)"

# -- WebAdministration: the IIS PS module. Required for app pool / site CRUD.
try {
    Import-Module WebAdministration -ErrorAction Stop
} catch {
    Stop-Deploy "WebAdministration module not available -- is the IIS management feature installed? ($($_.Exception.Message))"
}

# -- Locate build tools. nuget for restore, MSBuild for publish. -------------
function Resolve-Tool {
    param([string]$Name, [string[]]$Candidates)
    $cmd = Get-Command $Name -ErrorAction SilentlyContinue
    if ($cmd) { return $cmd.Source }
    foreach ($c in $Candidates) { if (Test-Path $c) { return $c } }
    return $null
}

$NuGet = Resolve-Tool 'nuget' @('C:\Tools\nuget.exe')
if (-not $NuGet) { Stop-Deploy "nuget.exe not found on PATH or C:\Tools -- vm-bootstrap drops it in C:\Tools" }

# MSBuild from VS Build Tools (vm-bootstrap installs it). vswhere finds the
# newest install; fall back to the well-known BuildTools path.
$MSBuild = $null
$vswhere = 'C:\Program Files (x86)\Microsoft Visual Studio\Installer\vswhere.exe'
if (Test-Path $vswhere) {
    $found = & $vswhere -latest -requires Microsoft.Component.MSBuild `
        -find 'MSBuild\**\Bin\MSBuild.exe' 2>$null | Select-Object -First 1
    if ($found) { $MSBuild = $found }
}
if (-not $MSBuild) {
    $MSBuild = Resolve-Tool 'MSBuild' @(
        'C:\Program Files (x86)\Microsoft Visual Studio\2022\BuildTools\MSBuild\Current\Bin\MSBuild.exe',
        'C:\Program Files\Microsoft Visual Studio\2022\BuildTools\MSBuild\Current\Bin\MSBuild.exe'
    )
}
if (-not $MSBuild) { Stop-Deploy "MSBuild.exe not found -- install VS Build Tools (vm-bootstrap.ps1 does this)" }
Write-Ok "nuget:   $NuGet"
Write-Ok "msbuild: $MSBuild"

# -- Load infra params from config/.env.provision.<env>. We need KEYVAULT_NAME,
# DB_NAME, DB_APP_LOGIN, SQL_INSTANCE. The file may not exist for a local
# dry-run; fall back to the .example defaults where sensible.
function Read-EnvFile {
    param([string]$Path)
    $h = @{}
    if (-not (Test-Path $Path)) { return $h }
    foreach ($line in Get-Content -LiteralPath $Path) {
        $t = $line.Trim()
        if ($t -eq '' -or $t.StartsWith('#')) { continue }
        $idx = $t.IndexOf('=')
        if ($idx -lt 1) { continue }
        $k = $t.Substring(0, $idx).Trim()
        $v = $t.Substring($idx + 1).Trim().Trim('"')
        # Strip trailing inline comments on simple unquoted values.
        if ($v -match '^([^"#]*\S)\s+#') { $v = $matches[1] }
        $h[$k] = $v
    }
    return $h
}

$envCfg = Read-EnvFile $EnvFile
if ($envCfg.Count -eq 0) {
    Write-Warn "no $EnvFile -- using built-in dev defaults for DB_NAME / SQL_INSTANCE / DB_APP_LOGIN"
}
$KeyVaultName = $envCfg['KEYVAULT_NAME']
$DbName       = if ($envCfg['DB_NAME'])      { $envCfg['DB_NAME'] }      else { 'qrypto-card' }
$SqlInstance  = if ($envCfg['SQL_INSTANCE']) { $envCfg['SQL_INSTANCE'] } else { 'SQLEXPRESS' }
$DbAppLogin   = if ($envCfg['DB_APP_LOGIN']) { $envCfg['DB_APP_LOGIN'] } else { 'kash_app' }

# On-box SQL Express, loopback. EF "data source" = localhost\SQLEXPRESS.
$DbServer = "localhost\$SqlInstance"
Write-Ok "DB target: server=$DbServer catalog=$DbName user=$DbAppLogin (ALL INT tiers -> this single dev DB)"

# -- DB password: -DbPassword wins; else pull from Key Vault. -----------------
function Get-DbPasswordFromKeyVault {
    param([string]$VaultName)
    if (-not $VaultName) {
        Stop-Deploy "no -DbPassword and KEYVAULT_NAME not set in $EnvFile -- cannot obtain DB password"
    }
    $az = Resolve-Tool 'az' @('C:\Program Files\Microsoft SDKs\Azure\CLI2\wbin\az.cmd')
    if (-not $az) { Stop-Deploy "az CLI not found -- needed to pull DB_PASSWORD from Key Vault" }
    Write-Step "az login --identity (VM managed identity) for Key Vault access"
    & $az login --identity --output none 2>$null
    if ($LASTEXITCODE -ne 0) { Stop-Deploy "az login --identity failed (exit $LASTEXITCODE)" }
    # KV secret names cannot contain '_'; seed-kv-secrets.sh stored it as DB-PASSWORD.
    Write-Step "pulling DB password (secret 'DB-PASSWORD') from Key Vault $VaultName"
    $tmp = New-TemporaryFile
    try {
        $pw = & $az keyvault secret show --vault-name $VaultName --name 'DB-PASSWORD' `
            --query value --output tsv 2>$tmp
        if ($LASTEXITCODE -ne 0) {
            $err = (Get-Content $tmp -Raw) -replace '\s+$', ''
            Stop-Deploy "failed to read DB_PASSWORD from $VaultName (exit $LASTEXITCODE): $err"
        }
    } finally {
        Remove-Item $tmp -ErrorAction SilentlyContinue
    }
    $pw = ($pw | Out-String).Trim()
    if ([string]::IsNullOrWhiteSpace($pw)) {
        Stop-Deploy "DB_PASSWORD is empty in $VaultName -- seed it before deploying"
    }
    return $pw
}

if ([string]::IsNullOrWhiteSpace($DbPassword)) {
    $DbPassword = Get-DbPasswordFromKeyVault -VaultName $KeyVaultName
    Write-Ok "DB password obtained from Key Vault (value not logged)"
} else {
    Write-Ok "DB password supplied via -DbPassword (value not logged)"
}

# The EF6 wrapped connection string uses bare double-quotes as the INNER delimiter
# (provider connection string="..."), so a '"' or ';' in the password corrupts the
# parsed connection string even though the XML stays well-formed. Reject fail-fast
# rather than emit a silently-broken Web.config.
if ($DbPassword -match '["'';]') {
    Stop-Deploy "DB password contains a double-quote, single-quote, or semicolon, which breaks the EF connection-string delimiter. Re-seed DB_PASSWORD without those characters."
}

# -- Parse sites.json --------------------------------------------------------
Write-Step "reading $SitesJson"
$sites = Get-Content -LiteralPath $SitesJson -Raw | ConvertFrom-Json
$publicSites   = @($sites.public)
$internalSites = @($sites.internal)
$rewrites      = @($sites.wcfEndpointRewrites)
$csProjects    = @($sites.connectionStringTiers.projects)
$allSites      = $publicSites + $internalSites
Write-Ok "sites: $($publicSites.Count) public + $($internalSites.Count) internal = $($allSites.Count) total"

# -- IIS app pool: managed runtime v4.0, integrated pipeline. ----------------
function Ensure-AppPool {
    param([string]$Name)
    $poolPath = "IIS:\AppPools\$Name"
    if (Test-Path $poolPath) {
        Write-Ok "app pool exists: $Name"
        $pool = Get-Item $poolPath
    } else {
        Write-Step "creating app pool: $Name"
        $pool = New-Item $poolPath
        Write-Ok "app pool created: $Name"
    }
    # Idempotent property set (runs every time; converges drift).
    Set-ItemProperty $poolPath -Name managedRuntimeVersion -Value 'v4.0'
    Set-ItemProperty $poolPath -Name managedPipelineMode   -Value 'Integrated'  # 0 = Integrated
    Set-ItemProperty $poolPath -Name startMode             -Value 'AlwaysRunning'
    Set-ItemProperty $poolPath -Name autoStart             -Value $true
}

# -- IIS site: ONE binding, 127.0.0.1:<port>. Loopback only, always. ---------
function Ensure-LoopbackSite {
    param([string]$SiteName, [string]$PoolName, [int]$Port, [string]$PhysicalPath)

    if (-not (Test-Path $PhysicalPath)) {
        New-Item -ItemType Directory -Path $PhysicalPath -Force | Out-Null
    }

    $loopbackBinding = "127.0.0.1:${Port}:"   # ip:port:host -- empty host header
    $sitePath = "IIS:\Sites\$SiteName"

    if (Test-Path $sitePath) {
        Write-Ok "site exists: $SiteName"
        Set-ItemProperty $sitePath -Name physicalPath     -Value $PhysicalPath
        Set-ItemProperty $sitePath -Name applicationPool  -Value $PoolName
    } else {
        Write-Step "creating site: $SiteName -> 127.0.0.1:$Port ($PhysicalPath)"
        New-Website -Name $SiteName -PhysicalPath $PhysicalPath -ApplicationPool $PoolName `
            -IPAddress '127.0.0.1' -Port $Port -HostHeader '' -Force | Out-Null
        Write-Ok "site created: $SiteName"
    }

    # Enforce the loopback-only invariant on EVERY run: the ONLY binding must be
    # 127.0.0.1:<port>. Strip anything else (a stray 0.0.0.0/* binding on an
    # internal money tier would expose it off-box). This is the load-bearing
    # safety check, so do it unconditionally, not just on create.
    $current = @(Get-WebBinding -Name $SiteName)
    foreach ($b in $current) {
        if ($b.bindingInformation -ne $loopbackBinding) {
            Write-Warn "  removing non-loopback binding on ${SiteName}: $($b.bindingInformation)"
            Remove-WebBinding -Name $SiteName -BindingInformation $b.bindingInformation -ErrorAction SilentlyContinue
        }
    }
    if (-not (Get-WebBinding -Name $SiteName | Where-Object { $_.bindingInformation -eq $loopbackBinding })) {
        New-WebBinding -Name $SiteName -IPAddress '127.0.0.1' -Port $Port -HostHeader '' -Protocol 'http' | Out-Null
    }
    Write-Ok "  binding: 127.0.0.1:$Port (loopback only)"
}

# -- NuGet restore + MSBuild publish a project to its site root. -------------
function Build-And-Publish {
    param([string]$Project, [string]$DestDir, [string]$Pool)

    $projDir  = Join-Path $RepoRoot $Project
    $csproj   = Join-Path $projDir "$Project.csproj"
    $packages = Join-Path $projDir 'packages.config'
    if (-not (Test-Path $csproj)) { Stop-Deploy "$Project`: csproj not found at $csproj" }

    if (Test-Path $packages) {
        Write-Step "$Project`: nuget restore"
        & $NuGet restore $packages -PackagesDirectory (Join-Path $RepoRoot 'packages') -NonInteractive
        if ($LASTEXITCODE -ne 0) { Stop-Deploy "$Project`: nuget restore failed (exit $LASTEXITCODE)" }
    } else {
        Write-Warn "$Project`: no packages.config -- skipping restore"
    }

    Write-Step "$Project`: msbuild publish -> $DestDir"
    # WebPublishMethod=FileSystem publishes the web app (transformed, content +
    # bin) to publishUrl. DeployOnBuild wires the publish target into the build.
    & $MSBuild $csproj `
        /p:Configuration=Release `
        /p:DeployOnBuild=true `
        /p:WebPublishMethod=FileSystem `
        /p:PublishProvider=FileSystem `
        /p:LastUsedBuildConfiguration=Release `
        /p:ExcludeApp_Data=true `
        /p:publishUrl="$DestDir" `
        /p:DeleteExistingFiles=false `
        /nologo /verbosity:minimal /m
    if ($LASTEXITCODE -ne 0) { Stop-Deploy "$Project`: msbuild publish failed (exit $LASTEXITCODE)" }

    # .NET Framework web projects with the MSDeploy package targets write the
    # published tree to obj\Release\Package\PackageTmp regardless of publishUrl
    # (the FileSystem publishUrl is only honored when the MSDeployPublish target
    # runs, which these projects don't). So copy PackageTmp -> $DestDir ourselves.
    # Mirrors runegate's deploy-iis.ps1.
    $pkgTmp = Join-Path $projDir 'obj\Release\Package\PackageTmp'
    if (Test-Path $pkgTmp) {
        # Stop the pool first so a running w3wp releases file locks on the deployed
        # assemblies (no-op if the pool is absent or already stopped); inject-secrets
        # recycles the pools afterward.
        if ($Pool) {
            try {
                $poolItem = Get-Item "IIS:\AppPools\$Pool" -ErrorAction SilentlyContinue
                if ($poolItem -and $poolItem.state -eq 'Started') { Stop-WebAppPool -Name $Pool; Start-Sleep -Seconds 1 }
            } catch { }
        }
        if (Test-Path $DestDir) {
            Get-ChildItem $DestDir -Force | Remove-Item -Recurse -Force -ErrorAction SilentlyContinue
        } else {
            New-Item -ItemType Directory -Path $DestDir -Force | Out-Null
        }
        Copy-Item "$pkgTmp\*" $DestDir -Recurse -Force
        Write-Ok "$Project`: published (PackageTmp -> $DestDir)"
    } elseif (Test-Path (Join-Path $DestDir 'Web.config')) {
        Write-Ok "$Project`: published to $DestDir"
    } else {
        Stop-Deploy "$Project`: no PackageTmp and no Web.config at $DestDir -- publish produced nothing"
    }
}

# -- WCF endpoint rewrite: string-replace fromHostPort -> toHostPort in the
# published Web.config, for the public tiers that consume the INT tier. -------
function Rewrite-WcfEndpoints {
    param([string]$Project, [string]$WebConfigPath)

    $applicable = @($rewrites | Where-Object { $_.inProjects -contains $Project })
    if ($applicable.Count -eq 0) { return }
    if (-not (Test-Path $WebConfigPath)) {
        Stop-Deploy "$Project`: Web.config not found at $WebConfigPath for WCF endpoint rewrite"
    }

    $text     = Get-Content -LiteralPath $WebConfigPath -Raw
    $original = $text
    foreach ($r in $applicable) {
        $from = $r.fromHostPort
        $to   = $r.toHostPort
        if ($text.Contains($from)) {
            $text = $text.Replace($from, $to)
            Write-Ok "  $Project`: WCF endpoint $from -> $to"
        } else {
            Write-Warn "  $Project`: WCF endpoint '$from' not found in Web.config (already rewritten or layout changed)"
        }
    }
    if ($text -ne $original) {
        [System.IO.File]::WriteAllText($WebConfigPath, $text, [System.Text.UTF8Encoding]::new($false))
    }
}

# -- EF "DBEntities" connection-string rewrite. Rebuild deterministically from
# a canonical template so the three INT tiers (which carry three DIFFERENT
# catalogs in source) all converge to the SAME on-box dev DB. -----------------
function Rewrite-ConnectionString {
    param([string]$Project, [string]$WebConfigPath, [string]$Password)

    if ($csProjects -notcontains $Project) { return }
    if (-not (Test-Path $WebConfigPath)) {
        Stop-Deploy "$Project`: Web.config not found at $WebConfigPath for connection-string rewrite"
    }

    # Inner provider connection string -> on-box dev DB.
    $providerConn =
        "data source=$DbServer;initial catalog=$DbName;user id=$DbAppLogin;" +
        "password=$Password;MultipleActiveResultSets=True;App=EntityFramework"

    [xml]$doc = Get-Content -LiteralPath $WebConfigPath -Raw
    $csElement = $doc.configuration.connectionStrings
    if (-not $csElement) {
        Stop-Deploy "$Project`: no <connectionStrings> element in $WebConfigPath"
    }
    $node = @($csElement.add | Where-Object { $_.name -eq 'DBEntities' }) | Select-Object -First 1
    if (-not $node) {
        Stop-Deploy "$Project`: no DBEntities <add> in <connectionStrings> of $WebConfigPath"
    }
    # Set the raw (unescaped) attribute value via the DOM; the XML serializer
    # re-encodes the inner quotes as &quot; on Save. This yields exactly the
    # EF6 wrapped shape (metadata=...;provider=System.Data.SqlClient;provider
    # connection string=&quot;...&quot;).
    $efValue =
        'metadata=res://*/DB.csdl|res://*/DB.ssdl|res://*/DB.msl;' +
        'provider=System.Data.SqlClient;provider connection string="' +
        $providerConn + '"'
    $node.SetAttribute('connectionString', $efValue)
    $node.SetAttribute('providerName', 'System.Data.EntityClient')
    $doc.Save($WebConfigPath)
    # Log without the password.
    Write-Ok "  $Project`: DBEntities -> server=$DbServer;catalog=$DbName;user=$DbAppLogin (password hidden)"
}

# ===========================================================================
# Solution-wide NuGet restore (once, up front).
# ===========================================================================
# Per-project restore alone is insufficient: a shared dependency (QryptoCard.Sec)
# references BouncyCastle by HintPath into packages/, but declares it in a DIFFERENT
# project's packages.config (QryptoCard.INT). Building a site that uses Sec BEFORE
# the INT project restores would miss it. Restoring the whole solution first fills
# packages/ from every packages.config, so all builds resolve their HintPaths.
$solution = Join-Path $RepoRoot 'QryptoCard.sln'
if (Test-Path $solution) {
    Write-Step "nuget restore (solution): $solution"
    & $NuGet restore $solution -PackagesDirectory (Join-Path $RepoRoot 'packages') -NonInteractive
    if ($LASTEXITCODE -ne 0) { Stop-Deploy "solution nuget restore failed (exit $LASTEXITCODE)" }
    Write-Ok "solution packages restored"
} else {
    Write-Warn "solution not found at $solution -- relying on per-project restore"
}

# ===========================================================================
# Main pass: per site, app pool -> site -> build/publish -> Web.config patch.
# ===========================================================================
foreach ($site in $allSites) {
    $project = $site.project
    $pool    = $site.appPool
    $port    = [int]$site.port
    $dest    = Join-Path $PublishRoot $project

    Write-Host ''
    Write-Host "=== $project  (pool=$pool port=$port) ==="

    Ensure-AppPool       -Name $pool
    Ensure-LoopbackSite  -SiteName $project -PoolName $pool -Port $port -PhysicalPath $dest
    Build-And-Publish    -Project $project -DestDir $dest -Pool $pool

    $webConfig = Join-Path $dest 'Web.config'
    Rewrite-WcfEndpoints      -Project $project -WebConfigPath $webConfig
    Rewrite-ConnectionString  -Project $project -WebConfigPath $webConfig -Password $DbPassword
}

# ===========================================================================
# Summary marker.
# ===========================================================================
Write-Host ''
Write-Host '==========================================================================='
Write-Host "  kash-cards IIS deploy complete: $env:COMPUTERNAME"
Write-Host '==========================================================================='
Write-Host "  Sites:       $($allSites.Count) (all bound 127.0.0.1 only)"
Write-Host "  Public:      $($publicSites.Count)  Internal: $($internalSites.Count)"
Write-Host "  Publish root: $PublishRoot"
Write-Host "  DB:          $DbServer / $DbName (single dev DB for all INT tiers)"
Write-Host ''
Write-Host '  Next: run inject-secrets.ps1 to write per-pool app secrets, then recycle.'
Write-Host ''
Write-Host "DEPLOY_RESULT: PASS ($($allSites.Count)/$($allSites.Count))"
