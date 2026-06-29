#Requires -Version 5.1
<#
.SYNOPSIS
    Fires the kash-cards reconciliation sweep by POSTing to the API.Callback loopback
    endpoint with the X-Scheduler-Auth shared-secret header. Designed to run from Windows
    Task Scheduler on the IIS host (SYSTEM, every ~10 min).

.DESCRIPTION
    The reconciliation sweep finds card spends stranded at 'pending provider' (balance
    debited, WasabiCard outcome ambiguous), asks the provider what actually happened, and
    finalizes / refunds / leaves each. Its trigger endpoint is:

        POST http://127.0.0.1:8084/v1/payment/reconcile/pending

    (API.Callback, port 8084 per deploy/config/sites.json). The endpoint is gated by
    SharedSecretAuth: every request must carry an X-Scheduler-Auth header whose value
    matches SCHEDULER-SHARED-SECRET in the VM's Azure Key Vault (fail-closed, constant-time
    compared). NOTE: the API.Callback host is publicly reachable via the Cloudflare tunnel,
    so this shared secret — not network isolation — is the gate; it must be strong and is
    rotated with the rest of the Key Vault material.

    Key Vault is read directly via the VM's managed identity (IMDS) rather than via the
    per-app-pool env var inject-secrets.ps1 sets, so a secret rotation in Key Vault is
    picked up on the next tick without re-running inject-secrets.ps1.

.PARAMETER VaultName
    Azure Key Vault containing SCHEDULER-SHARED-SECRET. Passed by the scheduled task action;
    not hardcoded so the same script works on dev / stg / prd.

.PARAMETER BaseUrl
    Optional. Defaults to http://127.0.0.1:8084 (the api-callback IIS site). Override for
    ad-hoc testing.

.PARAMETER SecretName
    Optional. The Key Vault secret name. Defaults to SCHEDULER-SHARED-SECRET.

.EXAMPLE
    powershell.exe -NoProfile -ExecutionPolicy Bypass `
        -File C:\src\kash-cards\deploy\scripts\scheduler-trigger.ps1 -VaultName kv-kash-dev

.NOTES
    One-time operator setup on the VM (creating an event-log source writes to HKLM, so run
    as Administrator) — vm-bootstrap.ps1 does this when it registers the task:

        if (-not [System.Diagnostics.EventLog]::SourceExists('KashCardsScheduler')) {
            New-EventLog -LogName Application -Source 'KashCardsScheduler'
        }

    Manual one-shot for verification (after the task is registered):
        Start-ScheduledTask -TaskName 'KashCards-ReconcilePending'
        Get-EventLog -LogName Application -Source KashCardsScheduler -Newest 5
#>

[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [string]$VaultName,

    [string]$BaseUrl = 'http://127.0.0.1:8084',

    [string]$SecretName = 'SCHEDULER-SHARED-SECRET'
)

$ErrorActionPreference = 'Stop'

$Path           = '/v1/payment/reconcile/pending'
$MonitorPath    = '/v1/payment/monitor/balance'
$EventSource    = 'KashCardsScheduler'
$SuccessEventId = 1000
$FailureEventId = 1001
$MonitorOkId    = 1002
$MonitorErrId   = 1003

function Write-KashEvent {
    param(
        [Parameter(Mandatory)][string]$Message,
        [Parameter(Mandatory)][ValidateSet('Information','Warning','Error')][string]$EntryType,
        [Parameter(Mandatory)][int]$EventId
    )
    # Best-effort: if the source isn't registered yet, don't mask the underlying job result.
    try {
        Write-EventLog -LogName Application -Source $EventSource `
            -EntryType $EntryType -EventId $EventId -Message $Message
    } catch {
        [Console]::Error.WriteLine("event log write failed ($EntryType/$EventId): $($_.Exception.Message)")
        [Console]::Error.WriteLine("original message: $Message")
    }
}

$url          = "$BaseUrl$Path"
$startedAtUtc = (Get-Date).ToUniversalTime()

try {
    # IMDS -- VM managed identity -> AAD token for the Key Vault audience.
    $imdsHeaders = @{ Metadata = 'true' }
    $imdsUri = 'http://169.254.169.254/metadata/identity/oauth2/token' `
        + '?api-version=2018-02-01' `
        + '&resource=https%3A%2F%2Fvault.azure.net'
    $tokenResp = Invoke-RestMethod -Method Get -Uri $imdsUri -Headers $imdsHeaders -TimeoutSec 30
    if (-not $tokenResp.access_token) {
        throw "IMDS returned no access_token (resource=vault.azure.net). Verify the VM has a system-assigned managed identity with Key Vault access."
    }

    # Key Vault -- pull the shared secret.
    $kvUri = "https://$VaultName.vault.azure.net/secrets/$SecretName`?api-version=7.4"
    $kvHeaders = @{ Authorization = "Bearer $($tokenResp.access_token)" }
    $kvResp = Invoke-RestMethod -Method Get -Uri $kvUri -Headers $kvHeaders -TimeoutSec 30
    if ([string]::IsNullOrEmpty($kvResp.value)) {
        throw "Key Vault returned an empty $SecretName from vault '$VaultName'."
    }
    $sharedSecret = $kvResp.value

    # Fire the sweep. POST (it has money-state side effects); the endpoint is HttpPost.
    $apiHeaders = @{ 'X-Scheduler-Auth' = $sharedSecret }
    $sw = [System.Diagnostics.Stopwatch]::StartNew()
    $resp = Invoke-WebRequest -Method Post -Uri $url -Headers $apiHeaders `
        -UseBasicParsing -TimeoutSec 540
    $sw.Stop()

    $msg = @"
Reconciliation sweep succeeded.
URL:        $url
StartedUtc: $($startedAtUtc.ToString('o'))
DurationMs: $($sw.ElapsedMilliseconds)
HttpStatus: $([int]$resp.StatusCode) $($resp.StatusDescription)
Body:       $($resp.Content)
"@
    Write-KashEvent -Message $msg -EntryType 'Information' -EventId $SuccessEventId

    # WasabiCard balance monitor + auto-fund tick. Same loopback host + shared secret. Best-effort
    # in its OWN try/catch so a monitor hiccup never masks the reconcile result or fails the task.
    try {
        $monUrl = "$BaseUrl$MonitorPath"
        $msw = [System.Diagnostics.Stopwatch]::StartNew()
        $monResp = Invoke-WebRequest -Method Post -Uri $monUrl -Headers $apiHeaders `
            -UseBasicParsing -TimeoutSec 120
        $msw.Stop()
        $monMsg = @"
WasabiCard monitor tick succeeded.
URL:        $monUrl
DurationMs: $($msw.ElapsedMilliseconds)
HttpStatus: $([int]$monResp.StatusCode) $($monResp.StatusDescription)
Body:       $($monResp.Content)
"@
        Write-KashEvent -Message $monMsg -EntryType 'Information' -EventId $MonitorOkId
    }
    catch {
        $monErr = @"
WasabiCard monitor tick FAILED (non-fatal; reconcile already succeeded).
URL:        $BaseUrl$MonitorPath
Error:      $($_.Exception.GetType().FullName): $($_.Exception.Message)
"@
        Write-KashEvent -Message $monErr -EntryType 'Warning' -EventId $MonitorErrId
    }
}
catch {
    $msg = @"
Reconciliation sweep FAILED.
URL:        $url
StartedUtc: $($startedAtUtc.ToString('o'))
Error:      $($_.Exception.GetType().FullName): $($_.Exception.Message)
StackTrace:
$($_.ScriptStackTrace)
"@
    Write-KashEvent -Message $msg -EntryType 'Error' -EventId $FailureEventId
    # Rethrow so Task Scheduler marks the run as failed (otherwise the history shows green
    # and the only failure signal is the event log).
    throw
}
