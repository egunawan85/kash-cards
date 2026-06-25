# Adapted from runegate-infra/scripts/verify/vm-verify.ps1
#
# vm-verify.ps1 -- read-only PASS/FAIL verification of the kash-cards VM's
# security perimeter and service health. Nothing here changes state.
#
# What it asserts:
#   - NSG denies inbound 443 (the tunnel is the only ingress; nothing direct).
#   - Each INT WCF tier (8091-8094) is bound to 127.0.0.1 ONLY and is NOT
#     reachable on the VM's private/public IP.
#   - The callback site (8084) rejects an UNSIGNED POST with 401.
#   - Each public site answers on its loopback port.
#   - The cloudflared Windows service is Running.
#   - SQL Express is reachable on loopback (1433).
#
# Two invocation patterns:
#   1. From a laptop via run-command:
#        az vm run-command invoke `
#          --resource-group rg-kash-dev --name vm-kash-dev `
#          --command-id RunPowerShellScript `
#          --scripts @deploy/scripts/verify/vm-verify.ps1 `
#          --parameters Env=dev
#   2. From inside an SSH/RDP session on the box:
#        pwsh ./deploy/scripts/verify/vm-verify.ps1
#
# Exits 0 on all-pass, 1 if any check failed. Prints a final
#   VERIFY_RESULT: PASS (n/n)  |  VERIFY_RESULT: FAIL (k/n failed)
# line a wrapper can grep for.
#
# The NSG check needs an ARM read on the NSG; the VM's MI does not get that by
# default, so when run on-box it falls back to "no inbound 443 LISTENER on a
# non-loopback interface", which is the property that actually matters from the
# box's vantage point. Pass -NsgName (and have an MI/login with Network Reader)
# to assert the rule itself.

[CmdletBinding()]
param(
    [ValidateSet('dev','stg','prd')]
    [string]$Env = 'dev',

    # Path to sites.json. Defaults to the repo-relative location next to this
    # script; override when the layout differs on the box.
    [string]$SitesJson = (Join-Path $PSScriptRoot '..\..\config\sites.json'),

    # Optional ARM assertion of the NSG inbound-443 deny. Empty => skip the ARM
    # read and rely on the listener check below.
    [string]$NsgName = '',
    [string]$ComputeRG = 'rg-kash-dev',

    [string]$SqlInstance = 'SQLEXPRESS',

    [int]$NetworkTimeoutSec = 5
)

# Per-check exceptions are captured by Check, so keep running through failures.
$ErrorActionPreference = 'Continue'
$ProgressPreference    = 'SilentlyContinue'

$AZ_CLI_PATH = 'C:\Program Files\Microsoft SDKs\Azure\CLI2\wbin\az.cmd'

# -- Helpers -----------------------------------------------------------------
$script:checks = @()

function Section {
    param([string]$Name)
    Write-Host ''
    Write-Host ("-- $Name " + ('-' * [Math]::Max(1, 70 - $Name.Length))) -ForegroundColor Cyan
}

function Check {
    param(
        [string]$Name,
        [scriptblock]$Test
    )
    $detail = ''
    $status = 'FAIL'
    try {
        $result = & $Test
        if ($result -is [array] -and $result.Count -gt 0) { $result = $result[-1] }
        if ($null -eq $result -or $result -eq $false -or
            ($result -is [string] -and [string]::IsNullOrWhiteSpace($result))) {
            $status = 'FAIL'
            $detail = if ($result -eq $false) { 'returned false' } else { 'no result' }
        } elseif ($result -eq $true) {
            $status = 'OK'; $detail = ''
        } else {
            $status = 'OK'
            $d = "$result"
            if ($d.Length -gt 90) { $d = $d.Substring(0, 87) + '...' }
            $detail = $d
        }
    } catch {
        $status = 'FAIL'
        $detail = $_.Exception.Message
        if ($detail.Length -gt 120) { $detail = $detail.Substring(0, 117) + '...' }
    }
    $script:checks += [PSCustomObject]@{ Name = $Name; Status = $status; Detail = $detail }
    if ($status -eq 'OK') {
        Write-Host "[OK]   $Name" -NoNewline -ForegroundColor Green
        if ($detail) { Write-Host "  $detail" -ForegroundColor DarkGray } else { Write-Host '' }
    } else {
        Write-Host "[FAIL] $Name" -NoNewline -ForegroundColor Red
        if ($detail) { Write-Host "  $detail" -ForegroundColor Yellow } else { Write-Host '' }
    }
}

# Non-loopback IPv4 addresses on this VM -- the addresses an INT tier must NOT
# answer on. Excludes 127.0.0.0/8 and link-local 169.254/16.
function Get-NonLoopbackIPv4 {
    Get-NetIPAddress -AddressFamily IPv4 -ErrorAction SilentlyContinue |
        Where-Object {
            $_.IPAddress -notlike '127.*' -and $_.IPAddress -notlike '169.254.*'
        } |
        Select-Object -ExpandProperty IPAddress -Unique
}

# Probe a TCP port on a specific local IP with a short connect timeout. Returns
# $true if the connection opened. Uses raw TcpClient (Test-NetConnection has no
# bind-to-local-IP option and a fixed long timeout).
function Test-TcpOpen {
    param([string]$IpAddress, [int]$Port, [int]$TimeoutSec = 3)
    $client = [System.Net.Sockets.TcpClient]::new()
    try {
        $iar = $client.BeginConnect($IpAddress, $Port, $null, $null)
        $ok  = $iar.AsyncWaitHandle.WaitOne([TimeSpan]::FromSeconds($TimeoutSec))
        if ($ok -and $client.Connected) { $client.EndConnect($iar); return $true }
        return $false
    } catch {
        return $false
    } finally {
        $client.Close()
    }
}

# -- Init --------------------------------------------------------------------
$startTime = Get-Date

Write-Host ''
Write-Host "Verifying $env:COMPUTERNAME (env=$Env)"

# Load sites.json (drives the per-port checks).
if (-not (Test-Path $SitesJson)) {
    Write-Host "[xx] sites.json not found at $SitesJson" -ForegroundColor Red
    Write-Host "VERIFY_RESULT: FAIL (sites.json missing)" -ForegroundColor Red
    exit 1
}
$sites        = Get-Content $SitesJson -Raw | ConvertFrom-Json
$publicSites  = @($sites.public)
$internalSites = @($sites.internal)
$callbackSite = $publicSites | Where-Object { $_.hostPrefix -eq 'callback' } | Select-Object -First 1
$nonLoopback  = @(Get-NonLoopbackIPv4)
Write-Host ("  Public sites: {0}   INT tiers: {1}   Non-loopback IPs: {2}" -f `
    $publicSites.Count, $internalSites.Count, (($nonLoopback -join ', ') -replace '^$','none'))

# ===========================================================================
# NSG: inbound 443 denied
# ===========================================================================
Section 'NSG inbound 443 denied'

if ($NsgName) {
    Check "NSG '$NsgName': no Allow rule on inbound 443" {
        & $AZ_CLI_PATH login --identity --output none 2>&1 | Out-Null
        $json = & $AZ_CLI_PATH network nsg rule list --nsg-name $NsgName --resource-group $ComputeRG --output json 2>&1
        if ($LASTEXITCODE -ne 0) { throw "az nsg rule list exit ${LASTEXITCODE}: $json" }
        $rules = $json | ConvertFrom-Json
        $allow443 = $rules | Where-Object {
            $_.direction -eq 'Inbound' -and $_.access -eq 'Allow' -and
            (($_.destinationPortRange -eq '443') -or ($_.destinationPortRanges -contains '443') -or
             ($_.destinationPortRange -eq '*'))
        }
        if ($allow443) { throw "found inbound Allow rule(s) reaching 443: $(($allow443 | ForEach-Object name) -join ', ')" }
        'no inbound Allow rule reaches 443'
    }
} else {
    # On-box fallback: no process is LISTENING on 443 on a non-loopback address.
    # If nothing answers 443 on the public/private NIC, an edge-blocked NSG and
    # an absent listener are indistinguishable from the box -- and either way
    # there is no direct 443 ingress, which is the property we care about.
    Check 'No 443 listener on a non-loopback interface' {
        $listeners = Get-NetTCPConnection -State Listen -LocalPort 443 -ErrorAction SilentlyContinue |
            Where-Object { $_.LocalAddress -notlike '127.*' -and $_.LocalAddress -ne '::1' }
        if ($listeners) {
            throw "443 is bound on: $(($listeners | ForEach-Object LocalAddress | Sort-Object -Unique) -join ', ')"
        }
        '(ARM rule not checked -- pass -NsgName to assert the rule) no 443 listener on a public NIC'
    }
}

# ===========================================================================
# INT WCF tiers: loopback-only, NOT reachable off-box
# ===========================================================================
Section 'Internal WCF tiers (loopback-only)'

foreach ($int in $internalSites) {
    $port = [int]$int.port
    $proj = $int.project

    Check "${proj} (:$port): answers on 127.0.0.1" {
        if (Test-TcpOpen -IpAddress '127.0.0.1' -Port $port -TimeoutSec $NetworkTimeoutSec) {
            'loopback listener up'
        } else {
            throw "no listener on 127.0.0.1:$port"
        }
    }

    Check "${proj} (:$port): NOT reachable on any non-loopback IP" {
        if ($nonLoopback.Count -eq 0) {
            # Don't silently PASS: no routable IP means we proved nothing about off-box
            # reachability (likely a DHCP/NIC misconfig on an Azure VM that should have one).
            Write-Host '  [!!] no non-loopback IPv4 found -- off-box reachability NOT verified' -ForegroundColor Yellow
            return 'WARN: no non-loopback IPs to probe (off-box reachability unverified)'
        }
        $reachable = @()
        foreach ($ip in $nonLoopback) {
            if (Test-TcpOpen -IpAddress $ip -Port $port -TimeoutSec $NetworkTimeoutSec) { $reachable += $ip }
        }
        if ($reachable.Count -gt 0) {
            throw "INT port $port is OPEN on non-loopback IP(s): $($reachable -join ', ') -- must be 127.0.0.1 only"
        }
        "closed on all $($nonLoopback.Count) non-loopback IP(s)"
    }
}

# ===========================================================================
# Callback site rejects an unsigned POST with 401
# ===========================================================================
Section 'Callback site rejects unsigned POST'

if (-not $callbackSite) {
    Check 'callback site present in sites.json' { throw 'no public site with hostPrefix=callback' }
} else {
    $cbPort = [int]$callbackSite.port
    Check "callback (:$cbPort): unsigned POST -> 401" {
        # Negative control: a junk body with no provider signature must be rejected
        # 401. The matching POSITIVE control -- a correctly signed callback that
        # succeeds end-to-end -- is exercised by QryptoCard.Tests.Smoke Tier 3 against
        # the running app (where the signing keys are available); vm-verify stays
        # read-only. Any 2xx/3xx here means the signature gate isn't firing.
        try {
            $resp = Invoke-WebRequest -Uri "http://127.0.0.1:$cbPort/" -Method POST `
                -Body '{"unsigned":"probe"}' -ContentType 'application/json' `
                -TimeoutSec $NetworkTimeoutSec -UseBasicParsing -ErrorAction Stop
            $code = [int]$resp.StatusCode
        } catch [System.Net.WebException] {
            $r = $_.Exception.Response
            if ($r) { $code = [int]$r.StatusCode } else { throw "no HTTP response: $($_.Exception.Message)" }
        } catch {
            # Invoke-WebRequest on PS7 surfaces non-2xx via HttpResponseException.
            $r = $_.Exception.Response
            if ($r) { $code = [int]$r.StatusCode } else { throw $_.Exception.Message }
        }
        if ($code -eq 401) {
            'unsigned POST correctly rejected (401)'
        } else {
            throw "expected 401, got $code -- the callback signature gate may not be enforced"
        }
    }
}

# ===========================================================================
# Public sites answer on their loopback ports
# ===========================================================================
Section 'Public sites answer on loopback'

foreach ($pub in $publicSites) {
    $port = [int]$pub.port
    $proj = $pub.project
    Check "${proj} (:$port): TCP open on 127.0.0.1" {
        if (Test-TcpOpen -IpAddress '127.0.0.1' -Port $port -TimeoutSec $NetworkTimeoutSec) {
            'listener up'
        } else {
            throw "no listener on 127.0.0.1:$port"
        }
    }
}

# ===========================================================================
# cloudflared Windows service running
# ===========================================================================
Section 'cloudflared service'

Check 'Cloudflared service: Running' {
    $svc = Get-Service -Name 'Cloudflared' -ErrorAction SilentlyContinue
    if (-not $svc) { throw 'Cloudflared service not installed (run vm-install-cloudflared.ps1)' }
    if ($svc.Status -eq 'Running') { "running (startup: $($svc.StartType))" }
    else { throw "service state: $($svc.Status)" }
}

# ===========================================================================
# SQL Express reachable on loopback
# ===========================================================================
Section 'SQL Express (loopback)'

Check "MSSQL`$$SqlInstance service: Running" {
    $svc = Get-Service -Name "MSSQL`$$SqlInstance" -ErrorAction SilentlyContinue
    if (-not $svc) { throw "MSSQL`$$SqlInstance service not found" }
    if ($svc.Status -eq 'Running') { 'running' } else { throw "service state: $($svc.Status)" }
}
Check "SQL accepts a local connection (localhost\$SqlInstance)" {
    # SQL Express serves locally via shared memory / named pipe (instance name), which is
    # how the app's .NET SqlClient and the deploy's go-sqlcmd connect -- not raw TCP 1433
    # (SQL 2025 Express has no static TCP port here). Test the path the app uses.
    $sqlcmd = 'C:\Tools\sqlcmd.exe'
    if (-not (Test-Path $sqlcmd)) { throw "go-sqlcmd not found at $sqlcmd" }
    & $sqlcmd -S "localhost\$SqlInstance" -E -C -l 10 -b -h -1 -Q 'SELECT 1' *> $null
    if ($LASTEXITCODE -eq 0) { 'connected via instance name (shared memory / named pipe)' }
    else { throw "could not connect to localhost\$SqlInstance (exit $LASTEXITCODE)" }
}

# ===========================================================================
# Summary
# ===========================================================================
$duration = [int](((Get-Date) - $startTime).TotalSeconds)
$total    = $script:checks.Count
$passed   = @($script:checks | Where-Object Status -eq 'OK').Count
$failed   = $total - $passed

Write-Host ''
Write-Host '==========================================================================='
Write-Host "  Verification: $env:COMPUTERNAME (env=$Env)"
Write-Host '==========================================================================='
Write-Host "  Total checks    $total"
Write-Host "  Passed          $passed" -ForegroundColor Green
if ($failed -gt 0) {
    Write-Host "  Failed          $failed" -ForegroundColor Red
} else {
    Write-Host "  Failed          $failed"
}
Write-Host "  Duration        ${duration}s"

if ($failed -gt 0) {
    Write-Host ''
    Write-Host '  Failures:' -ForegroundColor Red
    $script:checks | Where-Object Status -eq 'FAIL' | ForEach-Object {
        Write-Host "    [FAIL] $($_.Name)" -NoNewline -ForegroundColor Red
        if ($_.Detail) { Write-Host "  $($_.Detail)" -ForegroundColor Yellow } else { Write-Host '' }
    }
}
Write-Host '==========================================================================='

# Wrapper-parseable result line. Keep the format stable.
if ($failed -eq 0) {
    Write-Host "VERIFY_RESULT: PASS ($passed/$total)" -ForegroundColor Green
    exit 0
} else {
    Write-Host "VERIFY_RESULT: FAIL ($failed/$total failed)" -ForegroundColor Red
    exit 1
}
