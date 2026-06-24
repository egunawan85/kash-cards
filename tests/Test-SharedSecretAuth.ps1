# Behaviour tests for QryptoCard.Sec.SharedSecretAuth (the INT-tier auth-gate decision).
# Usage:  pwsh -File tests/Test-SharedSecretAuth.ps1
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$dll  = Join-Path $root 'QryptoCard.Sec\bin\Debug\QryptoCard.Sec.dll'
if (-not (Test-Path $dll)) { Write-Error "Build QryptoCard.Sec first; DLL not found: $dll" }
Add-Type -Path $dll
$A = [QryptoCard.Sec.SharedSecretAuth]

$pass = 0; $fail = 0
function Check($name, [scriptblock]$block) {
  try { & $block; Write-Host "  PASS  $name"; $script:pass++ }
  catch { Write-Host "  FAIL  $name -> $($_.Exception.Message)"; $script:fail++ }
}
function Assert($cond, $msg) { if (-not $cond) { throw $msg } }

$secret = 's3cr3t-shared-value-0123456789abcdef'
[Environment]::SetEnvironmentVariable('KC_AUTH_SECRET', $secret, 'Process')

Check 'correct secret is authorized'        { Assert ($A::IsAuthorized($secret, 'KC_AUTH_SECRET')) 'should authorize' }
Check 'wrong secret rejected'               { Assert (-not $A::IsAuthorized('wrong-value', 'KC_AUTH_SECRET')) 'should reject' }
Check 'null header rejected'                { Assert (-not $A::IsAuthorized($null, 'KC_AUTH_SECRET')) 'null rejected' }
Check 'empty header rejected'               { Assert (-not $A::IsAuthorized('', 'KC_AUTH_SECRET')) 'empty rejected' }
Check 'correct prefix not accepted'         { Assert (-not $A::IsAuthorized($secret.Substring(0,10), 'KC_AUTH_SECRET')) 'prefix rejected' }
Check 'longer-than-secret rejected'         { Assert (-not $A::IsAuthorized($secret + 'x', 'KC_AUTH_SECRET')) 'longer rejected' }
Check 'missing configured secret throws'    {
  $threw = $false
  try { $A::IsAuthorized('anything', 'KC_AUTH_UNSET') } catch { $threw = $true }
  Assert $threw 'a missing configured secret must throw (fail-closed)'
}
Check 'FixedTimeEquals correctness'         {
  Assert ($A::FixedTimeEquals('abc', 'abc')) 'equal'
  Assert (-not $A::FixedTimeEquals('abc', 'abd')) 'one char diff'
  Assert (-not $A::FixedTimeEquals('abc', 'ab')) 'length diff'
  Assert (-not $A::FixedTimeEquals('abc', $null)) 'null'
}

Write-Host ""
Write-Host ("SharedSecretAuth tests: {0} passed, {1} failed" -f $pass, $fail)
if ($fail -gt 0) { exit 1 }
