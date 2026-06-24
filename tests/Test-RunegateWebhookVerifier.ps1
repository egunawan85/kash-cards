# Behaviour/attack tests for QryptoCard.Sec.RunegateWebhookVerifier (run against the built DLL).
# Usage:  pwsh -File tests/Test-RunegateWebhookVerifier.ps1
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$dll  = Join-Path $root 'QryptoCard.Sec\bin\Debug\QryptoCard.Sec.dll'
if (-not (Test-Path $dll)) { Write-Error "Build QryptoCard.Sec first; DLL not found: $dll" }
Add-Type -Path $dll
$V = [QryptoCard.Sec.RunegateWebhookVerifier]

$pass = 0; $fail = 0
function Check($name, [scriptblock]$block) {
  try { & $block; Write-Host "  PASS  $name"; $script:pass++ }
  catch { Write-Host "  FAIL  $name -> $($_.Exception.Message)"; $script:fail++ }
}
function Assert($cond, $msg) { if (-not $cond) { throw $msg } }

function HmacHex([string]$secret, [byte[]]$signedInput) {
  $h = [System.Security.Cryptography.HMACSHA256]::new([Text.Encoding]::UTF8.GetBytes($secret))
  -join ($h.ComputeHash($signedInput) | ForEach-Object { $_.ToString('x2') })
}
function SignedInput([long]$ts, [byte[]]$body) {
  $prefix = [Text.Encoding]::UTF8.GetBytes("$ts.")
  $buf = New-Object byte[] ($prefix.Length + $body.Length)
  [Array]::Copy($prefix, 0, $buf, 0, $prefix.Length)
  [Array]::Copy($body, 0, $buf, $prefix.Length, $body.Length)
  ,$buf
}

$secret = 'k' * 40                  # >= 32 bytes
$body   = [Text.Encoding]::UTF8.GetBytes('{"id":"dep_1","amount":"100.00","status":"paid"}')
$ts     = [int64]1700000000
$hex    = HmacHex $secret (SignedInput $ts $body)
$header = "t=$ts,v1=$hex"

Check 'valid signature passes'            { Assert ($V::Verify($header, $body, $secret, $ts, 300)) 'should pass' }
Check 'tampered body fails'               {
  $b2 = [Text.Encoding]::UTF8.GetBytes('{"id":"dep_1","amount":"9999.00","status":"paid"}')
  Assert (-not $V::Verify($header, $b2, $secret, $ts, 300)) 'tamper should fail'
}
Check 'expired timestamp fails'           { Assert (-not $V::Verify($header, $body, $secret, ($ts + 1000), 300)) 'stale should fail' }
Check 'future timestamp fails'            { Assert (-not $V::Verify($header, $body, $secret, ($ts - 1000), 300)) 'future should fail' }
Check 'wrong secret fails'                { Assert (-not $V::Verify($header, $body, ('z' * 40), $ts, 300)) 'wrong secret should fail' }
Check 'weak secret (<32 bytes) fails'     { Assert (-not $V::Verify($header, $body, 'short', $ts, 300)) 'weak secret should fail' }
Check 'malformed header fails'            { Assert (-not $V::Verify('garbage', $body, $secret, $ts, 300)) 'malformed should fail' }
Check 'missing v1 fails'                  { Assert (-not $V::Verify("t=$ts", $body, $secret, $ts, 300)) 'missing v1 should fail' }
Check 'non-hex v1 fails'                  { Assert (-not $V::Verify("t=$ts,v1=zzzz", $body, $secret, $ts, 300)) 'non-hex should fail' }
Check 'null body fails'                   { Assert (-not $V::Verify($header, $null, $secret, $ts, 300)) 'null body should fail' }

Write-Host ""
Write-Host ("RunegateWebhookVerifier tests: {0} passed, {1} failed" -f $pass, $fail)
if ($fail -gt 0) { exit 1 }
