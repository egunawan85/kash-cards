# Behaviour tests for QryptoCard.Sec.SecretsConfig, run against the built DLL.
# (The legacy solution has no test-project infrastructure yet; this is a runnable,
# repeatable verification that can be promoted to a formal xUnit/MSTest project with CI.)
# Usage:  pwsh -File tests/Test-SecretsConfig.ps1
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$dll  = Join-Path $root 'QryptoCard.Sec\bin\Debug\QryptoCard.Sec.dll'
if (-not (Test-Path $dll)) { Write-Error "Build QryptoCard.Sec first; DLL not found: $dll" }
Add-Type -Path $dll
$T = [QryptoCard.Sec.SecretsConfig]

$pass = 0; $fail = 0
function Check($name, [scriptblock]$body) {
  try { & $body; Write-Host "  PASS  $name"; $script:pass++ }
  catch { Write-Host "  FAIL  $name -> $($_.Exception.Message)"; $script:fail++ }
}
function Assert($cond, $msg) { if (-not $cond) { throw $msg } }
function Set-Var($n,$v) { [Environment]::SetEnvironmentVariable($n,$v,'Process') }

# 1) Require throws when the variable is missing/blank (no silent default — fail closed)
Check 'Require throws on missing' {
  Set-Var 'KC_TEST_MISSING' $null
  $threw = $false
  try { $T::Require('KC_TEST_MISSING') } catch { $threw = $true }
  Assert $threw 'Require should have thrown for a missing variable'
}

# 2) Require returns the value when set
Check 'Require returns value when set' {
  Set-Var 'KC_TEST_PRESENT' 'hello-secret'
  Assert ($T::Require('KC_TEST_PRESENT') -eq 'hello-secret') 'Require returned wrong value'
}

# 3) Preload aggregates ALL missing names into one error
Check 'Preload aggregates missing names' {
  Set-Var 'KC_MISS_A' $null; Set-Var 'KC_MISS_B' $null
  $msg = $null
  # NB: cast to [string[]] — PowerShell mis-marshals an untyped @() into a params string[].
  try { $T::Preload([string[]]@('KC_MISS_A','KC_MISS_B')) } catch { $msg = $_.Exception.Message }
  Assert ($msg -ne $null) 'Preload should have thrown'
  Assert ($msg -match 'KC_MISS_A' -and $msg -match 'KC_MISS_B') 'Preload error should list both missing names'
}

# 4) Preload passes when all present
Check 'Preload passes when all present' {
  Set-Var 'KC_OK_1' 'x'; Set-Var 'KC_OK_2' 'y'
  $T::Preload([string[]]@('KC_OK_1','KC_OK_2'))   # should not throw
}

# 5) RequireHexBytes validates length + hex
Check 'RequireHexBytes validates' {
  Set-Var 'KC_HEX_OK' ('ab' * 32)          # 32 bytes
  $bytes = $T::RequireHexBytes('KC_HEX_OK', 32)
  Assert ($bytes.Length -eq 32) 'RequireHexBytes returned wrong length'
  Set-Var 'KC_HEX_BAD' 'zz'
  $threw = $false
  try { $T::RequireHexBytes('KC_HEX_BAD', 32) } catch { $threw = $true }
  Assert $threw 'RequireHexBytes should reject invalid hex/length'
}

# 6) GetOptional returns fallback when unset, value when set
Check 'GetOptional fallback + value' {
  Set-Var 'KC_OPT' $null
  Assert ($T::GetOptional('KC_OPT','def') -eq 'def') 'GetOptional should return fallback'
  Set-Var 'KC_OPT' 'real'
  Assert ($T::GetOptional('KC_OPT','def') -eq 'real') 'GetOptional should return the set value'
}

Write-Host ""
Write-Host ("SecretsConfig tests: {0} passed, {1} failed" -f $pass, $fail)
if ($fail -gt 0) { exit 1 }
