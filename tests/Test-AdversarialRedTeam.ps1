# Adversarial red-team probes for the webhook verifiers.
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$dll  = Join-Path $root 'QryptoCard.Sec\bin\Debug\QryptoCard.Sec.dll'
Add-Type -Path $dll
$R = [QryptoCard.Sec.RunegateWebhookVerifier]
$W = [QryptoCard.Sec.WasabiSignatureVerifier]

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

$secret = 'k' * 40
$body   = [Text.Encoding]::UTF8.GetBytes('{"Symbol":"USDT","Address":"abc","Total":100}')
$ts     = [int64]1700000000
$hex    = HmacHex $secret (SignedInput $ts $body)
$goodHeader = "t=$ts,v1=$hex"

Write-Host "--- Runegate bypass probes ---"
Check 'baseline valid passes' { Assert ($R::Verify($goodHeader,$body,$secret,$ts,300)) 'x' }

# Empty v1 value -> TryParseHeader gives v1="" -> IsNullOrEmpty -> false
Check 'empty v1 value rejected' { Assert (-not $R::Verify("t=$ts,v1=",$body,$secret,$ts,300)) 'x' }

# v1 present but odd-length hex -> FromHex returns null
Check 'odd-length hex rejected' { Assert (-not $R::Verify("t=$ts,v1=abc",$body,$secret,$ts,300)) 'x' }

# Provided sig of correct hex but wrong length (not 32 bytes) -> length guard
Check 'short valid-hex sig rejected' { Assert (-not $R::Verify("t=$ts,v1=00",$body,$secret,$ts,300)) 'x' }

# Duplicate v1: last one wins. Attacker puts good then garbage -> garbage used -> fail (safe)
Check 'duplicate v1 last-wins (good,bad) fails' { Assert (-not $R::Verify("t=$ts,v1=$hex,v1=00",$body,$secret,$ts,300)) 'x' }
# Duplicate v1: garbage then good -> good used -> PASSES. Is that a forgery? No - good sig is real. Just confirm behavior.
Check 'duplicate v1 last-wins (bad,good) passes' { Assert ($R::Verify("t=$ts,v1=zz,v1=$hex",$body,$secret,$ts,300)) 'x' }

# Duplicate t: attacker supplies a fresh ts plus the real (old) ts. Last t wins.
# Forge attempt: real signed payload used ts=1700000000. Provide t=<fresh>,v1=<hash over old ts>.
# Since signed input uses the PARSED ts, the v1 must match the fresh ts. Attacker cannot recompute. Should fail.
$freshTs = [int64]1700000200
Check 'replay with fresh t but old v1 fails (ts bound into hmac)' {
  Assert (-not $R::Verify("t=$freshTs,v1=$hex",$body,$secret,$freshTs,300)) 'x'
}

# Whitespace around values (Trim is applied) - a valid sig with spaces should still pass
Check 'whitespace-trimmed header still passes' { Assert ($R::Verify(" t = $ts , v1 = $hex ",$body,$secret,$ts,300)) 'x' }

# t with leading + or whitespace: NumberStyles.None rejects sign/space
Check 'plus-signed t rejected' { Assert (-not $R::Verify("t=+$ts,v1=$hex",$body,$secret,$ts,300)) 'x' }

# Negative tolerance: Math.Abs(diff) > negativeTol is always true -> always fail (fail-closed). Good.
Check 'negative tolerance fails closed' { Assert (-not $R::Verify($goodHeader,$body,$secret,$ts,-1)) 'x' }

# Huge tolerance lets an arbitrarily old (replayed) message pass — REPLAY surface within window only.
# Confirm: same valid msg verifies again at a later now within tolerance (this IS replay within window).
Check 'REPLAY within window re-verifies (no nonce)' {
  Assert ($R::Verify($goodHeader,$body,$secret,($ts+299),300)) 'replay within window verifies'
}

# Key confusion: pass a really long secret that is all the same; not relevant. Skip.

Write-Host "--- Wasabi bypass probes ---"
$rsa  = [System.Security.Cryptography.RSA]::Create(2048)
$pub  = [Convert]::ToBase64String($rsa.ExportSubjectPublicKeyInfo())
$wbody = [Text.Encoding]::UTF8.GetBytes('{"category":"card_transaction","type":"deposit","amount":"100.00"}')
$wsig = [Convert]::ToBase64String($rsa.SignData($wbody,[System.Security.Cryptography.HashAlgorithmName]::SHA256,[System.Security.Cryptography.RSASignaturePadding]::Pkcs1))

Check 'wasabi baseline passes' { Assert ($W::Verify($wsig,$wbody,$pub)) 'x' }

# Empty signature string -> rejected
Check 'empty sig rejected' { Assert (-not $W::Verify('',$wbody,$pub)) 'x' }
# Whitespace sig -> Trim -> "" -> FromBase64 of "" is empty array -> VerifySignature(empty) false
Check 'whitespace sig rejected' { Assert (-not $W::Verify('   ',$wbody,$pub)) 'x' }
# Zero-length signature bytes (valid base64 of empty) -> should fail
Check 'base64-empty sig rejected' { Assert (-not $W::Verify([Convert]::ToBase64String(@()),$wbody,$pub)) 'x' }

# PSS-signed signature against PKCS1 verifier -> should fail (algorithm fixed to PKCS1)
$pssSig = [Convert]::ToBase64String($rsa.SignData($wbody,[System.Security.Cryptography.HashAlgorithmName]::SHA256,[System.Security.Cryptography.RSASignaturePadding]::Pss))
Check 'PSS sig against PKCS1 verifier rejected' { Assert (-not $W::Verify($pssSig,$wbody,$pub)) 'x' }

# SHA1 signature -> should fail (verifier fixed SHA-256)
$sha1Sig = [Convert]::ToBase64String($rsa.SignData($wbody,[System.Security.Cryptography.HashAlgorithmName]::SHA1,[System.Security.Cryptography.RSASignaturePadding]::Pkcs1))
Check 'SHA1 sig against SHA256 verifier rejected' { Assert (-not $W::Verify($sha1Sig,$wbody,$pub)) 'x' }

# EC key supplied as "public key" -> RsaKeyParameters cast fails -> false
$ec = [System.Security.Cryptography.ECDsa]::Create()
$ecPub = [Convert]::ToBase64String($ec.ExportSubjectPublicKeyInfo())
Check 'EC key as platform key rejected' { Assert (-not $W::Verify($wsig,$wbody,$ecPub)) 'x' }

# Empty body path: body=[] is signed as "{}". Attacker who can produce a sig over "{}" passes with empty body.
# This requires the platform private key to have signed "{}", so not a forgery. Confirm consistency.
$braceSig = [Convert]::ToBase64String($rsa.SignData([Text.Encoding]::UTF8.GetBytes('{}'),[System.Security.Cryptography.HashAlgorithmName]::SHA256,[System.Security.Cryptography.RSASignaturePadding]::Pkcs1))
Check 'empty body verifies vs {} sig' { Assert ($W::Verify($braceSig,(New-Object byte[] 0),$pub)) 'x' }
# But a literal "{}" body ALSO verifies vs the same sig -> two distinct raw bodies share one signature.
Check 'literal {} body also verifies vs same sig (collision)' { Assert ($W::Verify($braceSig,[Text.Encoding]::UTF8.GetBytes('{}'),$pub)) 'x' }

# REPLAY: Wasabi has no timestamp/nonce. Same sig+body verifies unlimited times.
Check 'WASABI replay re-verifies (no nonce/timestamp)' {
  Assert ($W::Verify($wsig,$wbody,$pub)) 'replay 1'
  Assert ($W::Verify($wsig,$wbody,$pub)) 'replay 2'
}

Write-Host ""
Write-Host ("Adversarial probes: {0} passed, {1} failed" -f $pass, $fail)
if ($fail -gt 0) { exit 1 }
