# vm-bootstrap.ps1 -- script 2 of 2.
#
# Adapted from runegate-infra/scripts/provision/vm-bootstrap.ps1.
#
# Runs ON a freshly-provisioned Windows Server 2022 Azure Edition VM (created
# by azure-vm-provision.sh). Prepares the box to BUILD and HOST kash-cards
# (QryptoCard): a legacy ASP.NET Framework 4.6.2/4.7.2 + WCF + EF6 app with 12
# IIS sites (8 public, reached only via the Cloudflare tunnel; 4 loopback-only
# WCF "INT" tiers). It is .NET FRAMEWORK, not .NET Core.
#
# Installs, idempotently:
#   - IIS + the ASP.NET 4.x feature set + WCF HTTP Activation + URL Rewrite
#   - the .NET Framework 4.6.2 AND 4.7.2 targeting packs + VS Build Tools /
#     MSBuild (so all 12 projects build on-box)
#   - SQL Server Express (instance SQLEXPRESS), loopback-bound 127.0.0.1:1433,
#     mixed-mode auth, with a least-privilege SQL login (kash_app => db_owner
#     ONLY on the app DB)
#
# Invocation (no RDP -- from the operator laptop):
#
#   az vm run-command invoke `
#     --resource-group rg-kash-dev --name vm-kash-dev `
#     --command-id RunPowerShellScript `
#     --scripts @vm-bootstrap.ps1 `
#     --parameters KvName=kv-kash-dev DbName=qrypto-card DbAppLogin=kash_app
#
# ...or paste it once into an elevated PowerShell over RDP. The VM's
# system-assigned managed identity is what authorizes the Key Vault pull for
# the app login password; no stored credential is needed.
#
# Idempotent: re-running on a partially-bootstrapped VM picks up where it
# stopped. Each install step is check-then-install.

[CmdletBinding()]
param(
    # Key Vault name (e.g. kv-kash-dev). The VM MI has Key Vault Secrets User
    # (data plane only) and cannot list vaults via ARM, so the name is passed
    # explicitly. Used to pull the app DB login password (secret DB-PASSWORD).
    [Parameter(Mandatory = $true)]
    [string]$KvName,

    [ValidateSet('dev','stg','prd','prod')]
    [string]$Env = 'dev',

    # App database + least-privilege login. Mirror config/.env.provision.<env>
    # (DB_NAME, DB_APP_LOGIN). The login is granted db_owner on THIS database
    # only -- no server-level role (no sysadmin, no dbcreator).
    [string]$DbName     = 'qrypto-card',
    [string]$DbAppLogin = 'kash_app',

    # SQL Express instance name. azure-vm-provision.sh / .env.provision pin
    # this to SQLEXPRESS; the bootstrap installs that named instance.
    [string]$SqlInstance = 'SQLEXPRESS',

    # Root where the app source is deployed (vm-fetch-source.ps1 TargetDir). The reconciliation
    # scheduled task invokes $SourceRoot\deploy\scripts\scheduler-trigger.ps1.
    [string]$SourceRoot = 'C:\src\kash-cards'
)

$ErrorActionPreference = 'Stop'
$ProgressPreference    = 'SilentlyContinue'  # keeps Invoke-WebRequest fast

# Force UTF-8 for native-command stdout/stderr. The image's default codepage
# is cp1252; az CLI's Python runtime uses it for stdout, which silently drops
# non-cp1252 bytes in captured secret values. Set both layers.
[Console]::OutputEncoding = [System.Text.Encoding]::UTF8
$env:PYTHONIOENCODING     = 'utf-8'

# -- Logging helpers (match azure-vm-provision.sh style) ---------------------
function Write-Step { param([string]$m) Write-Host "[..] $m" }
function Write-Ok   { param([string]$m) Write-Host "[ok] $m" }
function Write-Warn { param([string]$m) Write-Host "[!!] $m" -ForegroundColor Yellow }
function Stop-Bootstrap {
    param([string]$m)
    Write-Host "[xx] $m" -ForegroundColor Red
    exit 1
}

# -- Pinned download sources + SHA-256 (one place to bump) -------------------
# For version-pinned URLs we hard-fail on hash mismatch (prevents CDN/cache
# tampering + silent upstream changes). For unpinned redirector URLs (aka.ms,
# fwlink) we still hash and LOG on every run so a silent bump is detectable in
# the run-command output (which lands in Azure Activity Log) even if not
# enforced. Fill the empty hashes off the operator laptop once captured.
$AZ_CLI_URL           = 'https://aka.ms/installazurecliwindowsx64'
$AZ_CLI_SHA256        = ''                                                       # unpinned: aka.ms redirector
$GO_SQLCMD_VERSION    = '1.8.0'
$GO_SQLCMD_URL        = "https://github.com/microsoft/go-sqlcmd/releases/download/v$GO_SQLCMD_VERSION/sqlcmd-windows-amd64.zip"
$GO_SQLCMD_SHA256     = 'fcfc2960426637e049d961722ad5eed6f4a824c9724163ef7f681fc568420b41'
$VS_BUILDTOOLS_URL    = 'https://aka.ms/vs/17/release/vs_BuildTools.exe'
$VS_BUILDTOOLS_SHA256 = ''                                                       # unpinned: aka.ms redirector
$SQL_SSEI_URL         = 'https://go.microsoft.com/fwlink/?linkid=2216019'        # SQL Server Express SSEI bootstrapper
$SQL_SSEI_SHA256      = ''                                                       # unpinned: fwlink redirector
$VC_REDIST_URL        = 'https://aka.ms/vs/17/release/vc_redist.x64.exe'         # VC++ 2015-2022 runtime. Marketplace
$VC_REDIST_SHA256     = ''                                                       # images stopped preinstalling it at
                                                                                 # build 20348.5139 (May 2026), which
                                                                                 # kills sqlwriter.exe (native C++) ->
                                                                                 # SQL setup service-start failures.
                                                                                 # Unpinned: aka.ms redirector.
$URL_REWRITE_URL      = 'https://download.microsoft.com/download/1/2/8/128E2E22-C1B9-44A4-BE2A-5859ED1D4592/rewrite_amd64_en-US.msi'
$URL_REWRITE_SHA256   = '37342ff2f585f263f34f48e9de59eb1051d61015a8e967dbde4075716230a32a'  # URL Rewrite Module 2.1, x64, en-US
                                                                                 # (download.iis.net DNS is unreliable from
                                                                                 # the VM subnet; use download.microsoft.com)

# Standard install paths -- used as both detection targets and PATH adds.
$AZ_CLI_PATH = 'C:\Program Files\Microsoft SDKs\Azure\CLI2\wbin\az.cmd'

# Cache directory for downloaded installers. ACL-locked on creation so an
# attacker who later gains low-priv write to C:\ can't poison a cached MSI and
# wait for the next idempotent re-run to execute it as SYSTEM.
$CACHE_DIR = 'C:\bootstrap-cache'
New-Item -ItemType Directory -Path $CACHE_DIR -Force | Out-Null
& icacls.exe $CACHE_DIR /inheritance:r | Out-Null
& icacls.exe $CACHE_DIR /grant:r 'BUILTIN\Administrators:(OI)(CI)F' 'NT AUTHORITY\SYSTEM:(OI)(CI)F' | Out-Null

# Download + verify helper.
#   - $ExpectedSha256 non-empty: hard-fail on mismatch.
#   - empty: compute + log the hash anyway so silent bumps are detectable.
# Re-uses a cached file but RE-VERIFIES every time (catches a poisoned cache
# even on idempotent re-runs).
function Get-VerifiedDownload {
    param(
        [Parameter(Mandatory = $true)][string]$Url,
        [Parameter(Mandatory = $true)][string]$OutFile,
        [string]$ExpectedSha256 = '',
        [string]$Label = '<download>'
    )
    if (-not (Test-Path $OutFile)) {
        Write-Step "${Label}: downloading"
        Invoke-WebRequest -Uri $Url -OutFile $OutFile -UseBasicParsing
    }
    $actual = (Get-FileHash -Path $OutFile -Algorithm SHA256).Hash.ToLower()
    if ($ExpectedSha256) {
        if ($actual -ne $ExpectedSha256.ToLower()) {
            Remove-Item $OutFile -Force -ErrorAction SilentlyContinue
            Stop-Bootstrap "$Label SHA-256 mismatch: expected $ExpectedSha256, got $actual (file removed; re-run to redownload)"
        }
        Write-Ok "$Label hash verified: $actual"
    } else {
        Write-Ok "$Label downloaded (unpinned URL); recorded hash: $actual"
    }
}

# -- Preflight: must be running with admin rights ----------------------------
$identity  = [Security.Principal.WindowsIdentity]::GetCurrent()
$principal = [Security.Principal.WindowsPrincipal]::new($identity)
if (-not $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) {
    Stop-Bootstrap "must run as Administrator (or as SYSTEM via 'az vm run-command invoke')"
}
Write-Ok "running as: $($identity.Name)"

# Validate the SQL identifiers we will interpolate into T-SQL before doing any
# work. SQL Server identifiers: start with a letter/underscore, then
# letters/digits/underscores, max 128 chars (we cap at 63 -- longer is almost
# always a programming error rather than a real name).
if ($DbAppLogin -notmatch '^[A-Za-z_][A-Za-z0-9_]{0,62}$') {
    Stop-Bootstrap "DbAppLogin '$DbAppLogin' fails validator ^[A-Za-z_][A-Za-z0-9_]{0,62}`$ -- refusing to interpolate into T-SQL"
}
# DbName may contain hyphens (e.g. qrypto-card), so it is always quoted as a
# bracketed identifier ([$DbName]) in T-SQL and never bare. Reject only the
# characters that could break out of bracket-quoting (']' and the statement
# terminators) -- everything else is safe inside [...].
if ($DbName -match '[\]\[;'']' -or $DbName.Length -gt 63 -or [string]::IsNullOrWhiteSpace($DbName)) {
    Stop-Bootstrap "DbName '$DbName' is empty, too long, or contains ']' '[' ';' '' -- refusing to interpolate into T-SQL"
}

# ===========================================================================
# Step 0. Azure CLI (prereq for the Key Vault pull of the app login password).
# ===========================================================================
if (Test-Path $AZ_CLI_PATH) {
    Write-Ok "Azure CLI already installed"
} else {
    $msi = Join-Path $CACHE_DIR 'az-cli.msi'
    Get-VerifiedDownload -Url $AZ_CLI_URL -OutFile $msi -ExpectedSha256 $AZ_CLI_SHA256 -Label 'Azure CLI MSI'
    Write-Step "installing Azure CLI (silent)"
    $p = Start-Process -FilePath msiexec.exe -ArgumentList "/i `"$msi`" /qn /norestart" -Wait -NoNewWindow -PassThru
    if ($p.ExitCode -ne 0 -and $p.ExitCode -ne 3010) {
        Stop-Bootstrap "Azure CLI install failed (exit $($p.ExitCode))"
    }
    if (-not (Test-Path $AZ_CLI_PATH)) {
        Stop-Bootstrap "Azure CLI install reported success but $AZ_CLI_PATH missing"
    }
    Write-Ok "Azure CLI installed"
}

# Wrapper that fails fast on non-zero exit. Pass long-form az flags only
# (--output, --query): short forms like `-o tsv` collide with PS
# CommonParameters (-OutVariable) and ambiguity-error before reaching az.
function Invoke-Az {
    [CmdletBinding()]
    param([Parameter(ValueFromRemainingArguments = $true)][string[]]$AzArgs)
    $tmp = New-TemporaryFile
    try {
        $out = & $AZ_CLI_PATH @AzArgs 2>$tmp
        $rc = $LASTEXITCODE
        if ($rc -ne 0) {
            $err = (Get-Content $tmp -Raw) -replace '\s+$',''
            Stop-Bootstrap "az $($AzArgs -join ' ') failed (exit $rc): $err"
        }
        $out
    } finally {
        Remove-Item $tmp -ErrorAction SilentlyContinue
    }
}

# ===========================================================================
# Step 1. az login --identity (auth as the VM's system-assigned MI).
# ===========================================================================
Write-Step "az login --identity"
Invoke-Az login --identity --output none | Out-Null
Write-Ok "logged in as VM managed identity"
Write-Ok "Key Vault: $KvName"

# ===========================================================================
# Step 2. IIS + ASP.NET 4.x feature set + WCF HTTP Activation.
# kash-cards is ASP.NET Framework 4.x with WCF service tiers (the INT
# projects), so we need the ASP.NET 4.5 web stack, ISAPI, and WCF HTTP
# Activation. WCF-Non-HTTP-Activation covers net.tcp/net.pipe bindings should
# any INT endpoint use them.
# ===========================================================================
$features = @(
    'Web-Server',
    'Web-Asp-Net45',          # ASP.NET 4.x runtime under IIS
    'Web-Net-Ext45',          # .NET 4.x extensibility
    'Web-ISAPI-Ext',
    'Web-ISAPI-Filter',
    'Web-Static-Content',
    'Web-Default-Doc',
    'Web-Http-Logging',
    'Web-Mgmt-Console',
    # WCF over HTTP for the INT .svc money tiers. This MUST be the Server-Manager
    # feature name ('NET-' prefix): Get-WindowsFeature does not recognize the DISM
    # name 'WCF-HTTP-Activation45', so the old value silently warned "not on this
    # image" and skipped -> no *.svc handler -> IIS served the .svc as static ->
    # 404/405 -> login + every API call failed. (kash's INT WCF is HTTP-only, so the
    # net.tcp/net.pipe Non-HTTP activation features are intentionally not installed.)
    'NET-WCF-HTTP-Activation45',
    'NET-Framework-45-Features'
)
foreach ($feat in $features) {
    $f = Get-WindowsFeature -Name $feat -ErrorAction SilentlyContinue
    if (-not $f) {
        Write-Warn "feature not on this image: $feat (skipping)"
        continue
    }
    if ($f.Installed) {
        Write-Ok "feature already installed: $feat"
    } else {
        Write-Step "installing feature: $feat"
        Install-WindowsFeature -Name $feat -ErrorAction Stop | Out-Null
        Write-Ok "installed: $feat"
    }
}

# ===========================================================================
# Step 2b. IIS URL Rewrite Module 2.1. Per-app sites whose web.config carries
# a <system.webServer><rewrite> section fail request-time with HTTP 500.19
# (0x8007000d) when the module/schema is not registered. Detection target is
# rewrite.dll (the runtime); rewrite_schema.xml lands alongside it.
# ===========================================================================
$rewriteDll = Join-Path $env:windir 'system32\inetsrv\rewrite.dll'
if (Test-Path $rewriteDll) {
    Write-Ok "IIS URL Rewrite Module already installed"
} else {
    $msi = Join-Path $CACHE_DIR 'urlrewrite.msi'
    Get-VerifiedDownload -Url $URL_REWRITE_URL -OutFile $msi -ExpectedSha256 $URL_REWRITE_SHA256 -Label 'IIS URL Rewrite Module 2.1 MSI'
    Write-Step "installing IIS URL Rewrite Module 2.1 (silent)"
    $p = Start-Process -FilePath msiexec.exe -ArgumentList "/i `"$msi`" /qn /norestart" -Wait -NoNewWindow -PassThru
    if ($p.ExitCode -ne 0 -and $p.ExitCode -ne 3010) {
        Stop-Bootstrap "IIS URL Rewrite Module install failed (exit $($p.ExitCode))"
    }
    if (-not (Test-Path $rewriteDll)) {
        Stop-Bootstrap "IIS URL Rewrite Module install reported success but $rewriteDll missing"
    }
    Write-Ok "IIS URL Rewrite Module installed: $rewriteDll"
}

# ===========================================================================
# Step 2c. Stop the IIS Default Web Site. The NSG denies inbound 443 and there
# is no app behind the default site; per-app deploy creates its own sites bound
# to localhost ports. An unconfigured default site answering on :80 has no
# business on this box.
# ===========================================================================
Import-Module WebAdministration -ErrorAction SilentlyContinue
$defaultSite = Get-Website -Name 'Default Web Site' -ErrorAction SilentlyContinue
if ($defaultSite) {
    if ($defaultSite.State -eq 'Started') {
        Stop-Website -Name 'Default Web Site' -ErrorAction SilentlyContinue
        Write-Ok "stopped IIS Default Web Site (port 80 closed)"
    } else {
        Write-Ok "IIS Default Web Site already stopped"
    }
} else {
    Write-Ok "IIS Default Web Site not present (skipping stop)"
}

# ===========================================================================
# Step 3. go-sqlcmd -- single static binary, no ODBC dependency. Used below to
# create the app login and (later) by app-side deploy to apply schema. CLI
# surface is compatible with classic sqlcmd (sqlcmd -S -E -Q ...).
# ===========================================================================
$sqlcmdPath = 'C:\Tools\sqlcmd.exe'
if (Test-Path $sqlcmdPath) {
    Write-Ok "go-sqlcmd already installed: $sqlcmdPath"
} else {
    $zipPath = Join-Path $CACHE_DIR 'go-sqlcmd.zip'
    Get-VerifiedDownload -Url $GO_SQLCMD_URL -OutFile $zipPath -ExpectedSha256 $GO_SQLCMD_SHA256 -Label "go-sqlcmd $GO_SQLCMD_VERSION"
    Write-Step "extracting go-sqlcmd to C:\Tools"
    New-Item -ItemType Directory -Path 'C:\Tools' -Force | Out-Null
    Expand-Archive -Path $zipPath -DestinationPath 'C:\Tools' -Force
    if (-not (Test-Path $sqlcmdPath)) {
        Stop-Bootstrap "go-sqlcmd extraction did not produce $sqlcmdPath"
    }
    $sysPath = [Environment]::GetEnvironmentVariable('PATH', 'Machine')
    if ($sysPath -notlike '*C:\Tools*') {
        [Environment]::SetEnvironmentVariable('PATH', "$sysPath;C:\Tools", 'Machine')
    }
    Write-Ok "go-sqlcmd installed: $sqlcmdPath"
}

# ===========================================================================
# Step 3b. NuGet CLI -- deploy-iis.ps1 restores each project's packages.config
# with it (it looks for C:\Tools\nuget.exe). Mirrors runegate-infra's bootstrap.
# ===========================================================================
$nugetPath = 'C:\Tools\nuget.exe'
if (Test-Path $nugetPath) {
    Write-Ok "nuget already installed: $nugetPath"
} else {
    Write-Step "downloading NuGet CLI -> $nugetPath"
    New-Item -ItemType Directory -Path 'C:\Tools' -Force | Out-Null
    [System.Net.ServicePointManager]::SecurityProtocol = [System.Net.SecurityProtocolType]::Tls12
    Invoke-WebRequest -Uri 'https://dist.nuget.org/win-x86-commandline/latest/nuget.exe' -OutFile $nugetPath -UseBasicParsing
    if (-not (Test-Path $nugetPath)) { Stop-Bootstrap "nuget download did not produce $nugetPath" }
    Write-Ok "nuget installed: $nugetPath"
}

# ===========================================================================
# Step 4. VS 2022 Build Tools -- Web workload + .NET FX 4.6.2 AND 4.7.2
# targeting packs. kash-cards' 12 projects target the 4.6.2/4.7.2 reference
# assemblies; we build on-box (no Docker), so MSBuild + both targeting packs
# must be present. The 4.6.2/4.7.2 RUNTIME ships with Server 2022 already; we
# add the reference assemblies for build-time targeting.
# ===========================================================================
$vsBuildToolsDir = 'C:\Program Files (x86)\Microsoft Visual Studio\2022\BuildTools'
$fx462RefDir     = 'C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.6.2'
$fx472RefDir     = 'C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.7.2'
if ((Test-Path $vsBuildToolsDir) -and (Test-Path $fx462RefDir) -and (Test-Path $fx472RefDir)) {
    Write-Ok "VS 2022 Build Tools + .NET FX 4.6.2 + 4.7.2 targeting packs already installed"
} else {
    $local = Join-Path $CACHE_DIR 'vs_BuildTools.exe'
    Get-VerifiedDownload -Url $VS_BUILDTOOLS_URL -OutFile $local -ExpectedSha256 $VS_BUILDTOOLS_SHA256 -Label 'VS 2022 Build Tools bootstrapper'
    Write-Step "installing VS 2022 Build Tools + WebBuildTools + .NET FX 4.6.2 & 4.7.2 targeting packs (10+ min)"
    $p = Start-Process -FilePath $local -ArgumentList @(
        '--quiet','--wait','--norestart','--nocache',
        '--add','Microsoft.VisualStudio.Workload.WebBuildTools',
        '--add','Microsoft.Net.Component.4.6.2.TargetingPack',
        '--add','Microsoft.Net.Component.4.7.2.TargetingPack',
        '--includeRecommended'
    ) -Wait -NoNewWindow -PassThru
    if ($p.ExitCode -ne 0 -and $p.ExitCode -ne 3010) {
        Stop-Bootstrap "VS 2022 Build Tools install failed (exit $($p.ExitCode))"
    }
    if (-not (Test-Path $vsBuildToolsDir)) {
        Stop-Bootstrap "VS 2022 Build Tools install reported success but $vsBuildToolsDir missing"
    }
    if (-not (Test-Path $fx462RefDir)) {
        Stop-Bootstrap "VS install succeeded but .NET FX 4.6.2 reference assemblies missing at $fx462RefDir"
    }
    if (-not (Test-Path $fx472RefDir)) {
        Stop-Bootstrap "VS install succeeded but .NET FX 4.7.2 reference assemblies missing at $fx472RefDir"
    }
    Write-Ok "VS 2022 Build Tools + .NET FX 4.6.2 & 4.7.2 targeting packs installed"
}

# ===========================================================================
# Step 5-pre. VC++ 2015-2022 runtime. Server 2022 marketplace images shipped
# this until build 20348.5020 (Apr 2026); 20348.5139 (May 2026) dropped it.
# Without it, sqlwriter.exe (native C++, part of SQL setup) dies instantly with
# STATUS_DLL_NOT_FOUND and SQL's service starts fail. Idempotent: skipped when
# vcruntime140.dll is already present.
# ===========================================================================
if (Test-Path 'C:\Windows\System32\vcruntime140.dll') {
    Write-Ok "VC++ runtime already present"
} else {
    $vcRedist = Join-Path $CACHE_DIR 'vc_redist.x64.exe'
    Get-VerifiedDownload -Url $VC_REDIST_URL -OutFile $vcRedist -ExpectedSha256 $VC_REDIST_SHA256 -Label 'VC++ 2015-2022 redistributable'
    Write-Step "installing VC++ redistributable"
    $p = Start-Process -FilePath $vcRedist -ArgumentList '/install','/quiet','/norestart' -Wait -NoNewWindow -PassThru
    if ($p.ExitCode -ne 0 -and $p.ExitCode -ne 3010) {
        Stop-Bootstrap "VC++ redist install failed (exit $($p.ExitCode))"
    }
    if (-not (Test-Path 'C:\Windows\System32\vcruntime140.dll')) {
        Stop-Bootstrap "VC++ redist installed but vcruntime140.dll still missing"
    }
    Write-Ok "VC++ runtime installed"
}

# ===========================================================================
# Step 5. SQL Server Express (instance SQLEXPRESS), installed in Windows-auth
# mode (BUILTIN\Administrators + NT AUTHORITY\SYSTEM as sysadmin). Step 5b
# below flips it to mixed-mode and creates the kash_app app login. SQL Express
# is bound to LOOPBACK only (Step 5a) -- the app reaches it via 127.0.0.1:1433,
# never the network; the NSG keeps the box dark regardless.
# ===========================================================================
$sqlSvcName = "MSSQL`$$SqlInstance"
$sqlSvc = Get-Service -Name $sqlSvcName -ErrorAction SilentlyContinue
if ($sqlSvc) {
    Write-Ok "SQL Server Express service exists ($sqlSvcName, $($sqlSvc.Status))"
} else {
    $ssei = Join-Path $CACHE_DIR 'SQL-SSEI-Expr.exe'
    Get-VerifiedDownload -Url $SQL_SSEI_URL -OutFile $ssei -ExpectedSha256 $SQL_SSEI_SHA256 -Label 'SQL Server Express SSEI bootstrapper'
    $sqlMediaDir = Join-Path $CACHE_DIR 'sql-media'
    New-Item -ItemType Directory -Path $sqlMediaDir -Force | Out-Null

    $sqlExprMedia = Get-ChildItem -Path $sqlMediaDir -Filter 'SQLEXPR_x64_*.exe' -ErrorAction SilentlyContinue | Select-Object -First 1
    if (-not $sqlExprMedia) {
        Write-Step "downloading SQL Express media (~250 MB)"
        $p = Start-Process -FilePath $ssei `
            -ArgumentList '/Action=Download','/Language=en-US',"/MediaPath=$sqlMediaDir",'/MediaType=Core','/Quiet' `
            -Wait -NoNewWindow -PassThru
        if ($p.ExitCode -ne 0) {
            Stop-Bootstrap "SQL Express media download failed (exit $($p.ExitCode))"
        }
        $sqlExprMedia = Get-ChildItem -Path $sqlMediaDir -Filter 'SQLEXPR_x64_*.exe' | Select-Object -First 1
        if (-not $sqlExprMedia) {
            Stop-Bootstrap "SQL Express media download produced no SQLEXPR_x64_*.exe"
        }
    } else {
        Write-Ok "SQL Express media already cached: $($sqlExprMedia.Name)"
    }

    $sqlExtractedDir = Join-Path $sqlMediaDir 'extracted'
    $sqlSetupExe = Join-Path $sqlExtractedDir 'SETUP.EXE'
    if (-not (Test-Path $sqlSetupExe)) {
        Write-Step "extracting SQL Express setup files"
        $p = Start-Process -FilePath $sqlExprMedia.FullName -ArgumentList '/Q',"/X:$sqlExtractedDir" -Wait -NoNewWindow -PassThru
        if ($p.ExitCode -ne 0) {
            Stop-Bootstrap "SQL Express extraction failed (exit $($p.ExitCode))"
        }
        if (-not (Test-Path $sqlSetupExe)) {
            Stop-Bootstrap "SQL Express extraction produced no SETUP.EXE at $sqlSetupExe"
        }
    }

    Write-Step "installing SQL Server Express ($SqlInstance, Windows auth at install -- Step 5b switches to mixed-mode, SQLEngine, TCP, ~5-10 min)"
    # Args with embedded spaces (NT Service\MSSQL$SQLEXPRESS) need explicit
    # quotes -- Start-Process joins ArgumentList with spaces without auto-quoting.
    # The license flag is /IACCEPTSQLSERVERLICENSETERMS (with the TERMS suffix);
    # /IACCEPTSQLSERVERLICENSE alone is wrong for SQL 2019+.
    $sqlArgs = @(
        '/Q',
        '/IACCEPTSQLSERVERLICENSETERMS',
        '/ACTION=Install',
        '/FEATURES=SQLEngine',
        "/INSTANCENAME=$SqlInstance",
        '/SQLSVCSTARTUPTYPE=Automatic',
        '/AGTSVCSTARTUPTYPE=Disabled',
        '/BROWSERSVCSTARTUPTYPE=Disabled',
        '/TCPENABLED=1',
        "/SQLSVCACCOUNT=`"NT Service\MSSQL`$$SqlInstance`"",
        # Both BUILTIN\Administrators (operator can sqlcmd -E from an admin
        # session) AND NT AUTHORITY\SYSTEM (this script, running as SYSTEM via
        # az vm run-command, needs sysadmin to CREATE the app login in 5b).
        '/SQLSYSADMINACCOUNTS="BUILTIN\Administrators" "NT AUTHORITY\SYSTEM"',
        '/UPDATEENABLED=False',
        '/SUPPRESSPRIVACYSTATEMENTNOTICE=True'
    )
    $p = Start-Process -FilePath $sqlSetupExe -ArgumentList $sqlArgs -Wait -NoNewWindow -PassThru
    if ($p.ExitCode -ne 0 -and $p.ExitCode -ne 3010) {
        Stop-Bootstrap "SQL Express install failed (exit $($p.ExitCode); see the Setup Bootstrap\Log\Summary.txt under C:\Program Files\Microsoft SQL Server\<NNN>)"
    }
    Write-Ok "SQL Server Express installed"
}

# Discover the MSSQLnn.<instance> registry hive dynamically -- Microsoft's SSEI
# fwlink occasionally bumps the major version (15=2019, 16=2022, 17=2025...),
# each using a different MSSQLnn prefix.
$mssqlRoot = 'HKLM:\SOFTWARE\Microsoft\Microsoft SQL Server'
$mssqlInstance = Get-ChildItem $mssqlRoot -ErrorAction SilentlyContinue |
    Where-Object { $_.PSChildName -match "^MSSQL\d+\.$([regex]::Escape($SqlInstance))$" } |
    Sort-Object { [int](($_.PSChildName -replace '^MSSQL(\d+)\..*$','$1')) } -Descending |
    Select-Object -First 1

# ===========================================================================
# Step 5a. Bind SQL Express to LOOPBACK on TCP 1433. The app connects via
# 127.0.0.1:1433; the DB never needs to answer on the LAN. Enable TCP, pin the
# static port 1433 on the loopback IP (IP1 = 127.0.0.1), and DISABLE listening
# on every other interface so a future NSG slip can't expose SQL.
# ===========================================================================
$tcpRegBase = if ($mssqlInstance) {
    Join-Path $mssqlInstance.PSPath 'MSSQLServer\SuperSocketNetLib\Tcp'
} else { $null }
if ($tcpRegBase -and (Test-Path $tcpRegBase)) {
    Write-Step "configuring SQL Express for TCP 1433 (runegate-infra pattern: static port on IPAll)"
    Set-ItemProperty -Path $tcpRegBase -Name Enabled -Value 1 -Type DWord
    # Static port 1433 on IPAll; clear the per-IP keys so they fall through to IPAll.
    # (A per-IP loopback-only config left IPAll portless and produced SQL error 26058
    # "no TCP listening ports" on SQL 2025 Express. Mirror runegate-infra's vm-bootstrap,
    # which sets the port on IPAll.) The DB is kept off the network by the NSG (all
    # inbound denied), not by binding loopback-only.
    $ipAll = Join-Path $tcpRegBase 'IPAll'
    if (Test-Path $ipAll) {
        Set-ItemProperty -Path $ipAll -Name TcpDynamicPorts -Value ''     -Type String
        Set-ItemProperty -Path $ipAll -Name TcpPort         -Value '1433' -Type String
    }
    Get-ChildItem $tcpRegBase | Where-Object { $_.PSChildName -ne 'IPAll' } | ForEach-Object {
        Set-ItemProperty -Path $_.PSPath -Name TcpDynamicPorts -Value '' -Type String -ErrorAction SilentlyContinue
        Set-ItemProperty -Path $_.PSPath -Name TcpPort         -Value '' -Type String -ErrorAction SilentlyContinue
    }
    if ((Get-Service $sqlSvcName).Status -eq 'Running') {
        Write-Step "restarting $sqlSvcName to apply port 1433"
        Restart-Service -Name $sqlSvcName -Force
    }
    Write-Ok "SQL Server Express configured for TCP 1433 (NSG-dark; not exposed on the LAN)"
} else {
    Write-Warn "no MSSQLnn.$SqlInstance hive found under $mssqlRoot (skipping loopback:1433 reconfigure)"
}

Set-Service -Name $sqlSvcName -StartupType Automatic
if ((Get-Service $sqlSvcName).Status -ne 'Running') {
    Start-Service $sqlSvcName
}
Write-Ok "${sqlSvcName}: Automatic, Running"

# ===========================================================================
# Step 5b. Switch SQL Express to mixed-mode auth and create the least-privilege
# app login (kash_app). The login is granted db_owner on the app DB ONLY -- no
# server-level role. It can fully manage its own database (EF6 migrations,
# schema apply) but cannot touch master/other databases or the instance.
#
# Password source: KV secret DB-PASSWORD, pulled via the VM MI. The password
# never reaches the sqlcmd command line -- it is staged in a temp .sql file
# under an Admins+SYSTEM ACL and deleted in finally{}.
# ===========================================================================
if ($mssqlInstance) {
    $loginModeKey = Join-Path $mssqlInstance.PSPath 'MSSQLServer'
    $currentMode  = (Get-ItemProperty -Path $loginModeKey -Name LoginMode -ErrorAction SilentlyContinue).LoginMode
    if ($currentMode -ne 2) {
        Write-Step "switching SQL Express to mixed-mode auth (LoginMode -> 2)"
        Set-ItemProperty -Path $loginModeKey -Name LoginMode -Value 2 -Type DWord
    } else {
        Write-Ok "SQL Express LoginMode already = 2; restarting anyway to converge"
    }
    # Always restart: a prior run that flipped the registry but failed before
    # the restart would leave the live service still rejecting SQL-auth logins,
    # and the next run would see "already 2" and skip -- never converging.
    Restart-Service -Name $sqlSvcName -Force
    Write-Ok "$sqlSvcName restarted; mixed-mode auth live"

    # SQL 2025 Express comes up named-pipe + shared-memory only (the TCP provider is
    # enabled but gets no static port -> logged error 26058; harmless here because every
    # client connects LOCALLY by instance name, not over TCP). Enable SQL Browser so
    # instance-name resolution (localhost\SQLEXPRESS) works for go-sqlcmd; the app's .NET
    # SqlClient reaches the same instance via shared memory.
    Write-Step "enabling SQL Browser (instance-name resolution)"
    Set-Service SQLBrowser -StartupType Automatic
    Start-Service SQLBrowser -ErrorAction SilentlyContinue

    # Readiness: poll a real local login via the instance name (resolves to the named
    # pipe). Proves SQL is up AND accepting connections -- exactly how the app connects.
    Write-Step "waiting for SQL Express to accept connections (localhost\$SqlInstance)"
    $sqlReadyDeadline = (Get-Date).AddSeconds(180)
    $sqlReady         = $false
    while ((Get-Date) -lt $sqlReadyDeadline) {
        & $sqlcmdPath -S "localhost\$SqlInstance" -E -C -l 5 -b -h -1 -Q 'SELECT 1' *> $null
        if ($LASTEXITCODE -eq 0) { $sqlReady = $true; break }
        Start-Sleep -Seconds 3
    }
    if (-not $sqlReady) {
        Stop-Bootstrap "SQL Express not accepting connections on localhost\$SqlInstance within 180s -- check Get-EventLog -LogName Application -Source $sqlSvcName on the VM"
    }
    Write-Ok "SQL Express accepting TCP connections"

    Write-Step "fetching DB-PASSWORD from KV $KvName"
    $dbPwLines  = @(Invoke-Az keyvault secret show --vault-name $KvName --name 'DB-PASSWORD' --query value --output tsv)
    $dbPassword = ($dbPwLines -join '').Trim()
    if ([string]::IsNullOrEmpty($dbPassword)) {
        Stop-Bootstrap "DB-PASSWORD not in KV $KvName -- seed it (the app .vault DB_PASSWORD -> KV secret DB-PASSWORD) and re-run."
    }
    Write-Ok "DB-PASSWORD fetched ($($dbPassword.Length) chars)"

    # Escape single quotes for the T-SQL string literal. (PS here-string
    # interpolation is single-pass, so $dbPassword content is not re-parsed --
    # only the literal ' needs doubling.)
    $escapedPw = $dbPassword -replace "'", "''"

    # Pre-ACL'd staging directory for the temp .sql file handed to sqlcmd.
    # Strict ACL set on the directory is inherited by the child file at
    # WriteAllText time (no TOCTOU window); random filename removes any
    # symlink-pre-creation vector.
    $sqlStageDir = Join-Path $env:ProgramData 'kash\sql-stage'
    if (-not (Test-Path $sqlStageDir)) {
        New-Item -ItemType Directory -Path $sqlStageDir -Force | Out-Null
    }
    & icacls.exe $sqlStageDir /inheritance:r /grant:r `
        'BUILTIN\Administrators:(OI)(CI)F' `
        'NT AUTHORITY\SYSTEM:(OI)(CI)F' | Out-Null

    # T-SQL:
    #   1. CREATE/ALTER the SQL login with the KV password (no server role).
    #   2. CREATE the app database if missing.
    #   3. CREATE the database user mapped to the login and add it to db_owner
    #      in THAT database only -- least privilege (no sysadmin/dbcreator).
    # $DbAppLogin is validated above; $DbName is bracket-quoted everywhere and
    # screened for ']' '[' ';' so it cannot break out of [...]. QUOTENAME() is
    # used in the dynamic USE so the DB context switch is also injection-safe.
    $sql = @"
SET NOCOUNT ON;
IF NOT EXISTS (SELECT 1 FROM sys.server_principals WHERE name = '$DbAppLogin')
BEGIN
    CREATE LOGIN [$DbAppLogin] WITH PASSWORD = '$escapedPw', CHECK_POLICY = OFF;
    PRINT 'Created login: $DbAppLogin';
END
ELSE
BEGIN
    ALTER LOGIN [$DbAppLogin] WITH PASSWORD = '$escapedPw';
    PRINT 'Updated password for: $DbAppLogin';
END
GO
IF DB_ID('$DbName') IS NULL
BEGIN
    CREATE DATABASE [$DbName];
    PRINT 'Created database: $DbName';
END
ELSE
    PRINT 'Database already exists: $DbName';
GO
DECLARE @sql nvarchar(max) = N'
USE ' + QUOTENAME('$DbName') + N';
IF NOT EXISTS (SELECT 1 FROM sys.database_principals WHERE name = ''$DbAppLogin'')
BEGIN
    CREATE USER ' + QUOTENAME('$DbAppLogin') + N' FOR LOGIN ' + QUOTENAME('$DbAppLogin') + N';
    PRINT ''Created DB user: $DbAppLogin in $DbName'';
END
ALTER ROLE [db_owner] ADD MEMBER ' + QUOTENAME('$DbAppLogin') + N';
PRINT ''Ensured db_owner: $DbAppLogin on $DbName'';';
EXEC sys.sp_executesql @sql;
GO
"@
    $sqlTmp = Join-Path $sqlStageDir ("$([System.IO.Path]::GetRandomFileName()).sql")
    [System.IO.File]::WriteAllText($sqlTmp, $sql, [System.Text.UTF8Encoding]::new($false))
    try {
        $sqlOut = & $sqlcmdPath -S "localhost\$SqlInstance" -E -i $sqlTmp -b -h -1 2>&1
        $rc     = $LASTEXITCODE
        if ($rc -ne 0) {
            $msg = ($sqlOut | Out-String).Trim()
            # Redact the password defensively in case sqlcmd echoes file content.
            if ($dbPassword -and $msg.Contains($dbPassword)) {
                $msg = $msg.Replace($dbPassword, '<REDACTED>')
            }
            if ($msg -match '18456|Login failed') {
                Stop-Bootstrap "sqlcmd -E failed (exit $rc) -- SYSTEM is not in SQL sysadmins on this instance. One-time fix: in an admin session run:`n  sqlcmd -S (local)\$SqlInstance -E -Q `"CREATE LOGIN [NT AUTHORITY\SYSTEM] FROM WINDOWS; ALTER SERVER ROLE [sysadmin] ADD MEMBER [NT AUTHORITY\SYSTEM];`"`nThen re-run. (Fresh installs don't need this; SQLSYSADMINACCOUNTS already includes SYSTEM.)"
            }
            Stop-Bootstrap "sqlcmd failed creating login/database (exit $rc): $msg"
        }
    } finally {
        Remove-Item $sqlTmp -Force -ErrorAction SilentlyContinue
    }
    foreach ($line in $sqlOut) {
        if ($line -and "$line".Trim()) { Write-Ok "  sql: $line" }
    }
} else {
    Write-Warn "skipping SQL login creation -- no MSSQLnn.$SqlInstance hive found"
}

# ===========================================================================
# Step 6. Register the reconciliation-sweep scheduled task.
# ===========================================================================
# The sweep recovers card spends stranded at 'pending provider'. It runs via an on-box
# scheduled task that POSTs to the API.Callback loopback endpoint with the Key-Vault-backed
# X-Scheduler-Auth secret (deploy/scripts/scheduler-trigger.ps1). Registered idempotently
# (-Force; infra is the source of truth, so re-running reverts any operator-side tweak). The
# trigger script lands with the app source at $SourceRoot, so the task fails harmlessly until
# the first deploy puts it there. Non-fatal: a registration hiccup must not abort the box
# bootstrap (it can be re-registered later).
Write-Step "registering reconciliation-sweep scheduled task"
try {
    if (-not [System.Diagnostics.EventLog]::SourceExists('KashCardsScheduler')) {
        New-EventLog -LogName Application -Source 'KashCardsScheduler'
        Write-Ok "  event-log source 'KashCardsScheduler' created"
    }

    $triggerScript = Join-Path $SourceRoot 'deploy\scripts\scheduler-trigger.ps1'
    $taskName = 'KashCards-ReconcilePending'
    $action = New-ScheduledTaskAction -Execute 'powershell.exe' `
        -Argument ("-NoProfile -ExecutionPolicy Bypass -File `"$triggerScript`" -VaultName $KvName")
    # -RepetitionDuration [TimeSpan]::MaxValue overflows Task Scheduler's xs:duration bounds;
    # 9999 days (~27 yr) is the canonical "effectively forever" within them.
    $trigger = New-ScheduledTaskTrigger -Once -At (Get-Date) `
        -RepetitionInterval (New-TimeSpan -Minutes 10) `
        -RepetitionDuration (New-TimeSpan -Days 9999)
    $principal = New-ScheduledTaskPrincipal -UserId 'SYSTEM' -RunLevel Highest
    # -MultipleInstances IgnoreNew prevents a slow tick from overlapping the next (belt; the
    # sweep is also idempotent server-side via the claim-gated mutations).
    $settings = New-ScheduledTaskSettingsSet `
        -StartWhenAvailable -DontStopOnIdleEnd -MultipleInstances IgnoreNew `
        -ExecutionTimeLimit (New-TimeSpan -Minutes 10)
    Register-ScheduledTask -TaskName $taskName -Action $action `
        -Trigger $trigger -Principal $principal -Settings $settings -Force | Out-Null
    Write-Ok "  scheduled task '$taskName' registered (every 10 min, SYSTEM) -> $triggerScript"
} catch {
    Write-Warn "scheduled-task registration failed: $($_.Exception.Message) -- register manually (see deploy/README.md)"
}

# ===========================================================================
# Step 7. Summary.
# ===========================================================================
Write-Host ''
Write-Host '==========================================================================='
Write-Host "  VM bootstrap complete: $env:COMPUTERNAME"
Write-Host '==========================================================================='
Write-Host ''
Write-Host "  Hostname        $env:COMPUTERNAME"
Write-Host "  Environment     $Env"
Write-Host "  Key Vault       $KvName"
Write-Host ''
Write-Host '  Installed:'
Write-Host '    IIS + ASP.NET 4.x + WCF HTTP/Non-HTTP Activation   hosts the 12 kash-cards sites'
Write-Host '    IIS URL Rewrite Module 2.1                         per-app web.config <rewrite> sections'
Write-Host '    go-sqlcmd                                          C:\Tools\sqlcmd.exe'
Write-Host '    VS 2022 Build Tools                                WebBuildTools + .NET FX 4.6.2 AND 4.7.2 targeting packs (build on-box)'
Write-Host "    SQL Server Express ($SqlInstance)                     127.0.0.1:1433 loopback-only, mixed-mode auth"
Write-Host "    SQL login $DbAppLogin                              db_owner on [$DbName] ONLY (least privilege)"
Write-Host ''
Write-Host '  Next steps'
Write-Host '  -------------------------------------------------------------------------'
Write-Host '  1. Deploy the app: build the 12 projects and bind the IIS sites per'
Write-Host '     deploy/config/sites.json (deploy-iis.ps1, run separately).'
Write-Host "  2. The app connects to SQL via 127.0.0.1:1433 as $DbAppLogin against [$DbName]."
Write-Host '  3. Install/register the cloudflared tunnel (perimeter, outbound-only) so'
Write-Host '     the public sites are reachable without any inbound NSG port.'
Write-Host ''
Write-Host '==========================================================================='
