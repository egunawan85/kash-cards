# Behaviour/attack tests for QryptoCard.Sec.WasabiSignatureVerifier (run against the built DLL).
# Signs a body with .NET RSA SHA256/PKCS1 (== SHA256withRSA) and verifies via the BouncyCastle path.
# Usage:  pwsh -File tests/Test-WasabiSignatureVerifier.ps1
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$dll  = Join-Path $root 'QryptoCard.Sec\bin\Debug\QryptoCard.Sec.dll'
if (-not (Test-Path $dll)) { Write-Error "Build QryptoCard.Sec first; DLL not found: $dll" }
Add-Type -Path $dll
$V = [QryptoCard.Sec.WasabiSignatureVerifier]

$pass = 0; $fail = 0
function Check($name, [scriptblock]$block) {
  try { & $block; Write-Host "  PASS  $name"; $script:pass++ }
  catch { Write-Host "  FAIL  $name -> $($_.Exception.Message)"; $script:fail++ }
}
function Assert($cond, $msg) { if (-not $cond) { throw $msg } }

function New-RsaKey([int]$bits) { [System.Security.Cryptography.RSA]::Create($bits) }
function Sign([System.Security.Cryptography.RSA]$rsa, [byte[]]$body) {
  [Convert]::ToBase64String($rsa.SignData($body, [System.Security.Cryptography.HashAlgorithmName]::SHA256, [System.Security.Cryptography.RSASignaturePadding]::Pkcs1))
}
function SpkiB64([System.Security.Cryptography.RSA]$rsa) { [Convert]::ToBase64String($rsa.ExportSubjectPublicKeyInfo()) }

$rsa  = New-RsaKey 2048
$pub  = SpkiB64 $rsa
$body = [Text.Encoding]::UTF8.GetBytes('{"category":"card_transaction","type":"deposit","amount":"100.00"}')
$sig  = Sign $rsa $body

Check 'valid signature passes'         { Assert ($V::Verify($sig, $body, $pub)) 'should pass' }
Check 'tampered body fails'            {
  $b2 = [Text.Encoding]::UTF8.GetBytes('{"category":"card_transaction","type":"deposit","amount":"9999.00"}')
  Assert (-not $V::Verify($sig, $b2, $pub)) 'tamper should fail'
}
Check 'wrong key fails'                {
  $other = New-RsaKey 2048
  Assert (-not $V::Verify($sig, $body, (SpkiB64 $other))) 'wrong key should fail'
}
Check 'weak (1024-bit) key rejected'   {
  $weak = New-RsaKey 1024
  $wsig = Sign $weak $body
  Assert (-not $V::Verify($wsig, $body, (SpkiB64 $weak))) 'weak key should be rejected'
}
Check 'garbage signature fails'        { Assert (-not $V::Verify('not-base64-@@@', $body, $pub)) 'garbage sig should fail' }
Check 'garbage key fails'              { Assert (-not $V::Verify($sig, $body, 'not-a-key')) 'garbage key should fail' }
Check 'empty body signed as {} '       {
  $empty = New-Object byte[] 0
  $brace = [Text.Encoding]::UTF8.GetBytes('{}')
  $sigEmpty = Sign $rsa $brace
  Assert ($V::Verify($sigEmpty, $empty, $pub)) 'empty body should verify against {} signature'
}

Write-Host ""
Write-Host ("WasabiSignatureVerifier tests: {0} passed, {1} failed" -f $pass, $fail)
if ($fail -gt 0) { exit 1 }
