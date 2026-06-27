# vm-build-detached.ps1 -- run the all-tiers IIS build as a DETACHED on-box job, so a long
# build is NOT bound by the `az vm run-command` execution window (Option B). The box is
# NSG-dark: we cannot RDP in to run the build locally like the runegate sister, so instead one
# run-command LAUNCHES the build as a Scheduled Task (runs as SYSTEM, survives the launching
# run-command returning) and writes its progress to a log + an exit-code "done" marker; the
# operator's deploy.sh then POLLS those via cheap run-commands until the marker appears.
#
#   -Action launch : create + start the build task, return immediately. Single-flight: refuses
#                    if a build task is already Running (the task state IS the lock -- no stale
#                    lock-file to clear).
#   -Action run    : the body the Scheduled Task executes -- runs deploy-iis.ps1 (all tiers) as a
#                    child powershell, capturing all output to the log and the child's exit code
#                    to the done marker. (Child process so deploy-iis's `exit N` can't kill us.)
#   -Action poll   : print log lines after -SinceLine, then a BUILD_LINES:<n> and
#                    BUILD_STATUS:(running|done exit=<code>|no-task|state=<s>) sentinel.
#   -Smoke         : run a trivial ~12s job instead of the real build -- to validate the
#                    launch/detach/log/done/poll/lock plumbing without a full compile.
[CmdletBinding()]
param(
    [ValidateSet('launch', 'run', 'poll')][string]$Action = 'launch',
    [ValidateSet('dev', 'stg', 'prd')][string]$Env = 'dev',
    [int]$SinceLine = 0,
    [switch]$Smoke
)
$ErrorActionPreference = 'Continue'

$RepoRoot  = 'C:\src\kash-cards'
$TmpDir    = Join-Path $RepoRoot 'tmp'
$Log       = Join-Path $TmpDir 'deploy-build.log'
$Done      = Join-Path $TmpDir 'deploy-build.done'
$DeployIis = Join-Path $RepoRoot 'deploy\scripts\deploy\deploy-iis.ps1'
$SelfPath  = Join-Path $RepoRoot 'deploy\scripts\deploy\vm-build-detached.ps1'
$TaskName  = 'kash-deploy-build'
if (-not (Test-Path $TmpDir)) { New-Item -ItemType Directory -Path $TmpDir -Force | Out-Null }

function Get-TaskState {
    $t = Get-ScheduledTask -TaskName $TaskName -ErrorAction SilentlyContinue
    if ($t) { return $t.State } else { return $null }
}

# ---- run: executed BY the scheduled task (detached). Build -> log; exit code -> done marker. ----
if ($Action -eq 'run') {
    if ($Smoke) {
        # Trivial body to exercise the plumbing only -- NOT a real build.
        1..4 | ForEach-Object {
            ("smoke build line $_  ($(Get-Date -Format o))") | Out-File -FilePath $Log -Encoding utf8 -Append
            Start-Sleep -Seconds 3
        }
        Set-Content -Path $Done -Value '0'
        exit 0
    }
    # Real build: run deploy-iis.ps1 (all tiers) as a CHILD powershell so its `exit N`
    # (Stop-Deploy) sets our $LASTEXITCODE instead of terminating this runner.
    & powershell.exe -NoProfile -ExecutionPolicy Bypass -File $DeployIis -Env $Env 2>&1 |
        Out-File -FilePath $Log -Encoding utf8
    $code = $LASTEXITCODE
    if ($null -eq $code) { $code = 0 }
    Set-Content -Path $Done -Value ([string]$code)
    exit 0
}

# ---- launch: create + start the detached task, return immediately. ----
if ($Action -eq 'launch') {
    if (-not $Smoke -and -not (Test-Path $DeployIis)) { Write-Host "[xx] deploy-iis.ps1 not found at $DeployIis"; exit 1 }
    # Single-flight lock = the task state. A build already Running -> refuse.
    if ((Get-TaskState) -eq 'Running') { Write-Host "[xx] a detached build is already RUNNING (task '$TaskName') -- refusing a concurrent build"; exit 1 }

    Remove-Item $Log, $Done -ErrorAction SilentlyContinue
    Unregister-ScheduledTask -TaskName $TaskName -Confirm:$false -ErrorAction SilentlyContinue

    $smokeArg = if ($Smoke) { ' -Smoke' } else { '' }
    $arg = ('-NoProfile -ExecutionPolicy Bypass -File "{0}" -Action run -Env {1}{2}' -f $SelfPath, $Env, $smokeArg)
    $taskAction = New-ScheduledTaskAction -Execute 'powershell.exe' -Argument $arg
    $principal  = New-ScheduledTaskPrincipal -UserId 'SYSTEM' -LogonType ServiceAccount -RunLevel Highest
    $settings   = New-ScheduledTaskSettingsSet -AllowStartIfOnBatteries -DontStopIfGoingOnBatteries `
                    -ExecutionTimeLimit (New-TimeSpan -Hours 2) -MultipleInstances IgnoreNew
    try {
        Register-ScheduledTask -TaskName $TaskName -Action $taskAction -Principal $principal -Settings $settings -Force -ErrorAction Stop | Out-Null
        ("build launched (detached task '$TaskName', Env=$Env$(if($Smoke){' SMOKE'}), $(Get-Date -Format o))") | Out-File -FilePath $Log -Encoding utf8
        Start-ScheduledTask -TaskName $TaskName -ErrorAction Stop
    } catch {
        Write-Host "[xx] failed to register/start the detached build task: $($_.Exception.Message)"
        exit 1
    }
    Start-Sleep -Seconds 1
    Write-Host "[ok] detached build launched (task=$TaskName, state=$(Get-TaskState), Env=$Env$(if($Smoke){' SMOKE'}))"
    exit 0
}

# ---- poll: emit new log lines + a status sentinel. ----
if ($Action -eq 'poll') {
    $lines = @()
    if (Test-Path $Log) { $lines = @(Get-Content -LiteralPath $Log -ErrorAction SilentlyContinue) }
    $total = $lines.Count
    if ($SinceLine -lt $total) { $lines[$SinceLine..($total - 1)] | ForEach-Object { Write-Host $_ } }
    Write-Host "BUILD_LINES: $total"
    if (Test-Path $Done) {
        $code = (Get-Content -LiteralPath $Done -ErrorAction SilentlyContinue | Select-Object -First 1)
        if ($code) { $code = ([string]$code).Trim() } else { $code = '0' }
        Write-Host "BUILD_STATUS: done exit=$code"
    }
    elseif ((Get-TaskState) -eq 'Running') { Write-Host "BUILD_STATUS: running" }
    elseif ($null -eq (Get-TaskState))     { Write-Host "BUILD_STATUS: no-task" }
    else                                   { Write-Host "BUILD_STATUS: state=$(Get-TaskState)" }
    exit 0
}
