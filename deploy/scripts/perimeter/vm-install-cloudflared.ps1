# Adapted from runegate-infra/scripts/perimeter/vm-install-cloudflared.ps1
#
# vm-install-cloudflared.ps1 -- install cloudflared as a pinned Windows service
# on the kash-cards VM. Pulls the named-tunnel connector token from Key Vault
# (KvName) via the VM's managed identity, then registers + starts the service.
#
# The tunnel is the ONLY public ingress: the NSG denies inbound 443, so the box
# reaches the internet only by this outbound connector. Without a Running
# service every public hostname returns CF 530 (no origin).
#
# Invocation (from operator's laptop):
#   az vm run-command invoke `
#     --resource-group rg-kash-dev --name vm-kash-dev `
#     --command-id RunPowerShellScript `
#     --scripts @deploy/scripts/perimeter/vm-install-cloudflared.ps1 `
#     --parameters KvName=kv-kash-dev Env=dev
#
# Prereq: cloudflare-setup.sh (named mode, CLOUDFLARE_QUICK_TUNNEL=false) must
# have run first so the connector token is in KV under
# CLOUDFLARED-TUNNEL-TOKEN-<ENV-UPPER>. (Quick-tunnel dev mode does not use this
# installer -- there `cloudflared tunnel --url ...` runs in the foreground.)
#
# Idempotent: re-running is safe. Skips when the binary is already installed AND
# the Windows service is already Running.

[CmdletBinding()]
param(
    # KV name MUST be passed explicitly -- the VM's MI has Key Vault Secrets User
    # (data plane only) and cannot list vaults via Resource Manager.
    [Parameter(Mandatory = $true)]
    [string]$KvName,

    [ValidateSet('dev','stg','prd')]
    [string]$Env = 'dev'
)

$ErrorActionPreference = 'Stop'
$ProgressPreference    = 'SilentlyContinue'
[Console]::OutputEncoding = [System.Text.Encoding]::UTF8
$env:PYTHONIOENCODING     = 'utf-8'

function Write-Step { param([string]$m) Write-Host "[..] $m" }
function Write-Ok   { param([string]$m) Write-Host "[ok] $m" }
function Stop-Install {
    param([string]$m)
    Write-Host "[xx] $m" -ForegroundColor Red
    exit 1
}

$AZ_CLI_PATH = 'C:\Program Files\Microsoft SDKs\Azure\CLI2\wbin\az.cmd'

function Invoke-Az {
    [CmdletBinding()]
    param([Parameter(ValueFromRemainingArguments = $true)][string[]]$AzArgs)
    $tmp = New-TemporaryFile
    try {
        $out = & $AZ_CLI_PATH @AzArgs 2>$tmp
        $rc = $LASTEXITCODE
        if ($rc -ne 0) {
            $err = (Get-Content $tmp -Raw) -replace '\s+$',''
            Stop-Install "az $($AzArgs -join ' ') failed (exit $rc): $err"
        }
        $out
    } finally {
        Remove-Item $tmp -ErrorAction SilentlyContinue
    }
}

# Pinned cloudflared version + MSI SHA-256. Bump both together when updating.
$CLOUDFLARED_VERSION = '2026.3.0'
$CLOUDFLARED_URL     = "https://github.com/cloudflare/cloudflared/releases/download/$CLOUDFLARED_VERSION/cloudflared-windows-amd64.msi"
$CLOUDFLARED_SHA256  = '57c0fc3ffd003bb13fa12a83aa5d32c83ec72f61d0135c57a32b6ad60ccde5bd'

$CACHE_DIR = 'C:\bootstrap-cache'
New-Item -ItemType Directory -Path $CACHE_DIR -Force | Out-Null

function Get-VerifiedDownload {
    param(
        [Parameter(Mandatory = $true)][string]$Url,
        [Parameter(Mandatory = $true)][string]$OutFile,
        [string]$ExpectedSha256 = '',
        [string]$Label = 'download'
    )
    if (-not (Test-Path $OutFile)) {
        Write-Step "downloading $Label"
        Invoke-WebRequest -Uri $Url -OutFile $OutFile -UseBasicParsing
    }
    $actual = (Get-FileHash $OutFile -Algorithm SHA256).Hash.ToLower()
    if ($ExpectedSha256) {
        if ($actual -ne $ExpectedSha256.ToLower()) {
            Stop-Install "$Label hash mismatch: expected $ExpectedSha256, got $actual"
        }
        Write-Ok "$Label hash OK ($actual)"
    } else {
        Write-Ok "$Label hash $actual (unpinned)"
    }
}

Write-Step "az login --identity"
Invoke-Az login --identity --output none | Out-Null
Write-Ok "logged in as VM managed identity"

$cloudflaredExe   = 'C:\Program Files (x86)\cloudflared\cloudflared.exe'
$cloudflaredSvcNm = 'Cloudflared'
$envUpper         = $Env.ToUpper()
$cfTokenSecretNm  = "CLOUDFLARED-TUNNEL-TOKEN-$envUpper"
$cloudflaredSvc   = Get-Service -Name $cloudflaredSvcNm -ErrorAction SilentlyContinue

if ((Test-Path $cloudflaredExe) -and $cloudflaredSvc -and $cloudflaredSvc.Status -eq 'Running') {
    Write-Ok "cloudflared $CLOUDFLARED_VERSION already installed and service running"
} else {
    if (-not (Test-Path $cloudflaredExe)) {
        $msi = Join-Path $CACHE_DIR 'cloudflared.msi'
        Get-VerifiedDownload -Url $CLOUDFLARED_URL -OutFile $msi -ExpectedSha256 $CLOUDFLARED_SHA256 -Label "cloudflared $CLOUDFLARED_VERSION MSI"
        Write-Step "installing cloudflared $CLOUDFLARED_VERSION (silent)"
        $p = Start-Process -FilePath msiexec.exe -ArgumentList "/i `"$msi`" /qn /norestart" -Wait -NoNewWindow -PassThru
        if ($p.ExitCode -ne 0 -and $p.ExitCode -ne 3010) {
            Stop-Install "cloudflared MSI install failed (exit $($p.ExitCode))"
        }
        if (-not (Test-Path $cloudflaredExe)) {
            Stop-Install "cloudflared MSI install reported success but $cloudflaredExe missing"
        }
        Write-Ok "cloudflared binary installed: $cloudflaredExe"
    }

    Write-Step "pulling $cfTokenSecretNm from KV $KvName"
    $tokenLines  = @(Invoke-Az keyvault secret show --vault-name $KvName --name $cfTokenSecretNm --query value --output tsv)
    $tunnelToken = ($tokenLines -join '').Trim()
    if (-not $tunnelToken -or $tunnelToken.Length -lt 50) {
        Stop-Install "secret '$cfTokenSecretNm' not in KV $KvName (or too short -- $($tunnelToken.Length) chars). Run cloudflare-setup.sh (named mode) first."
    }
    Write-Ok "got connector token ($($tunnelToken.Length) chars)"

    if (-not $cloudflaredSvc) {
        # `cloudflared service install <token>` registers the Windows service AND
        # starts it. The token encodes account/tunnel/secret -- no other config.
        # Pipe stdout to $null because cloudflared echoes the token back; keeps it
        # out of the run-command stdout (which lands in the Activity Log).
        Write-Step "registering Cloudflared as Windows service"
        & $cloudflaredExe service install $tunnelToken | Out-Null
        if ($LASTEXITCODE -ne 0) {
            Stop-Install "cloudflared service install failed (exit $LASTEXITCODE)"
        }
        Write-Ok "Cloudflared service registered"
    } else {
        Write-Ok "Cloudflared service already registered (state: $($cloudflaredSvc.Status))"
    }

    # Pin the service to Automatic start so it survives a reboot, then ensure it
    # is running -- a stopped service here means the tunnel won't connect and
    # every public URL stays CF 530.
    Set-Service -Name $cloudflaredSvcNm -StartupType Automatic -ErrorAction SilentlyContinue
    Start-Service -Name $cloudflaredSvcNm -ErrorAction SilentlyContinue
    Start-Sleep -Seconds 3
    $cloudflaredSvc = Get-Service -Name $cloudflaredSvcNm -ErrorAction SilentlyContinue
    if ($cloudflaredSvc -and $cloudflaredSvc.Status -eq 'Running') {
        Write-Ok "Cloudflared service running (startup: $($cloudflaredSvc.StartType))"
    } else {
        Stop-Install "Cloudflared service failed to start (state: $($cloudflaredSvc.Status))"
    }
}

Write-Host ''
Write-Ok 'cloudflared install complete'
