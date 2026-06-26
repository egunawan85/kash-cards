# vm-seed.ps1 -- seed the on-box dev DB from committed SQL (deploy/sql/seeds/*.sql),
# applied with sqlcmd. Replaces the QryptoCard.DevSeed C# console project: no build,
# no QryptoCard.Sec reference, no bare-`az` PATH dependency (the bug that made the old
# seeder die silently under run-command). Mirrors the sister db/seeds + sqlcmd -v pattern;
# the kash twist is the reversible AES (Secure.cs), reproduced here in PowerShell so the
# password/secret ciphertext is computed at deploy time and passed via -v -- never committed.
#
# Invocation (on the VM, after vm-publish-schema):
#   az vm run-command invoke -g rg-kash-dev -n vm-kash-dev --command-id RunPowerShellScript `
#     --scripts @deploy/scripts/deploy/vm-seed.ps1 --parameters KvName=kv-kash-dev DbName=qrypto-card
[CmdletBinding()]
param(
    # NOT [Mandatory]: a missing mandatory parameter makes PowerShell prompt and hang
    # forever under run-command -- fail fast instead.
    [string]$KvName    = '',
    [string]$DbName    = 'qrypto-card',
    [ValidateSet('dev','stg','prd')][string]$Env = 'dev',
    [string]$SourceDir = 'C:\src\kash-cards',
    [string]$SmokeOut  = 'C:\src\kash-cards\deploy\secrets\.smoke.env',
    [string]$DbServer  = 'localhost\SQLEXPRESS'
)
$ErrorActionPreference = 'Stop'
[Console]::OutputEncoding = [System.Text.Encoding]::UTF8
function Step($m) { Write-Host "[..] $m" }
function Ok($m)   { Write-Host "[ok] $m" }
function Die($m)  { Write-Host "[xx] $m" -ForegroundColor Red; exit 1 }
if (-not $KvName) { Die 'KvName is required (pass --parameters KvName=<vault> DbName=<db>)' }

# az via FULL path: bare `az` is NOT on the SYSTEM PATH under run-command.
$AZ = 'C:\Program Files\Microsoft SDKs\Azure\CLI2\wbin\az.cmd'
if (-not (Test-Path $AZ)) {
    $c = Get-Command az -ErrorAction SilentlyContinue
    if ($c) { $AZ = $c.Source } else { Die "az CLI not found at $AZ" }
}
$sqlcmd = if (Test-Path 'C:\Tools\sqlcmd.exe') { 'C:\Tools\sqlcmd.exe' } else { (Get-Command sqlcmd -ErrorAction SilentlyContinue).Source }
if (-not $sqlcmd) { Die 'sqlcmd not found (expected C:\Tools\sqlcmd.exe from vm-bootstrap)' }

Step 'az login --identity'
& $AZ login --identity --output none 2>&1 | Out-Null
if ($LASTEXITCODE -ne 0) { Die 'az login --identity failed (managed identity / az CLI?)' }

function Get-Secret([string]$name) {
    $v = (& $AZ keyvault secret show --vault-name $KvName --name $name --query value -o tsv 2>$null)
    if ([string]::IsNullOrWhiteSpace($v)) { Die "$name not found in Key Vault $KvName -- seed it (seed-kv-secrets.sh) and re-run" }
    return $v.Trim()
}
$DBKEY  = Get-Secret 'DBKEY'
$APPKEY = Get-Secret 'APPKEY'

# AES exactly as QryptoCard.Sec/Secure.cs: AES-128-CBC-PKCS7, Key = IV = first 16
# bytes of UTF8(key) (zero-padded). EncryptDB uses DBKEY (stored form); EncryptAPP
# uses APPKEY (wire form the client sends).
function Enc([string]$plain, [string]$key) {
    $kb  = New-Object byte[] 16
    $src = [Text.Encoding]::UTF8.GetBytes($key)
    [Array]::Copy($src, $kb, [Math]::Min($src.Length, 16))
    $aes = [Security.Cryptography.Aes]::Create()
    $aes.Mode = [Security.Cryptography.CipherMode]::CBC
    $aes.Padding = [Security.Cryptography.PaddingMode]::PKCS7
    $aes.KeySize = 128; $aes.BlockSize = 128
    $aes.Key = $kb; $aes.IV = $kb
    $enc = $aes.CreateEncryptor()
    $pt  = [Text.Encoding]::UTF8.GetBytes($plain)
    $ct  = $enc.TransformFinalBlock($pt, 0, $pt.Length)
    $enc.Dispose(); $aes.Dispose()
    return [Convert]::ToBase64String($ct)
}

# Credentials: overridable via env, else deterministic dev defaults.
$adminEmail = if ($env:SEED_ADMIN_EMAIL)    { $env:SEED_ADMIN_EMAIL }    else { 'edward@s16.ventures' }
$adminPwd   = if ($env:SEED_ADMIN_PASSWORD) { $env:SEED_ADMIN_PASSWORD } else { 'KashAdmin!dev1' }
$userPwd    = if ($env:SEED_USER_PASSWORD)  { $env:SEED_USER_PASSWORD }  else { 'KashUser!dev1' }
# Demo cardholder for the synthetic dataset (seed-dev-synthetic.sql): the ONE loginable
# synthetic user. Its email must be a REAL inbox so the interactive web login's emailed OTP
# arrives (the synthetic filler users have fake emails and never log in). Defaults to the
# owner's address; override with SEED_DEMO_EMAIL / SEED_DEMO_PASSWORD.
$demoEmail  = if ($env:SEED_DEMO_EMAIL)     { $env:SEED_DEMO_EMAIL }     else { 'egunawan85@gmail.com' }
$demoPwd    = if ($env:SEED_DEMO_PASSWORD)  { $env:SEED_DEMO_PASSWORD }  else { 'KashDemo!dev1' }
$smokeEmail = 'smoke-user@kash.cards'
$apiKey     = 'smoke-' + [Guid]::NewGuid().ToString('N')
$apiSecret  = [Guid]::NewGuid().ToString('N') + [Guid]::NewGuid().ToString('N')

# Internal RT: $adminEmail is spliced into seed-admin.sql via sqlcmd -v (textual
# substitution). The base64 ciphertext + GUID values are inherently quote-free, but an
# operator-supplied SEED_ADMIN_EMAIL is not -- validate its shape so it cannot break out
# of the N'...' string literal (defense-in-depth; the default is the fixed dev admin).
if ($adminEmail -notmatch '^[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Za-z]{2,}$') {
    Die "SEED_ADMIN_EMAIL '$adminEmail' has an unexpected shape -- refusing to splice into seed SQL"
}
# Same shape-guard for the demo cardholder email (spliced into seed-dev-synthetic.sql via -v).
if ($demoEmail -notmatch '^[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Za-z]{2,}$') {
    Die "SEED_DEMO_EMAIL '$demoEmail' has an unexpected shape -- refusing to splice into seed SQL"
}

$adminPwdDb    = Enc $adminPwd  $DBKEY
$userPwdDb     = Enc $userPwd   $DBKEY
$apiSecretDb   = Enc $apiSecret $DBKEY
$apiSecretWire = Enc $apiSecret $APPKEY
$adminPwdWire  = Enc $adminPwd  $APPKEY
$demoPwdDb     = Enc $demoPwd   $DBKEY

$seedsDir = Join-Path $SourceDir 'deploy\sql\seeds'
foreach ($f in 'seed-reference.sql','seed-admin.sql','seed-smoke-user.sql') {
    if (-not (Test-Path (Join-Path $seedsDir $f))) { Die "seed file not found: $(Join-Path $seedsDir $f) (run vm-fetch-source first)" }
}
$base = @('-S', $DbServer, '-E', '-C', '-l', '15', '-b', '-d', $DbName)

Step 'seed-reference.sql (roles, settings, counters, card type, network)'
& $sqlcmd @base -i (Join-Path $seedsDir 'seed-reference.sql')
if ($LASTEXITCODE -ne 0) { Die "seed-reference failed (exit $LASTEXITCODE)" }

Step "seed-admin.sql ($adminEmail)"
& $sqlcmd @base -i (Join-Path $seedsDir 'seed-admin.sql') `
    -v ADMIN_EMAIL="$adminEmail" -v ADMIN_PWD_DB="$adminPwdDb" -v ADMIN_FIRST="Edward" -v ADMIN_LAST="Admin"
if ($LASTEXITCODE -ne 0) { Die "seed-admin failed (exit $LASTEXITCODE)" }

# External RT (Opus, MEDIUM): the smoke user + a live API key is DEV/TEST ONLY -- never
# seed it into stg/prd. Gate on $Env (previously declared but unused). The .smoke.env
# artifact (smoke + admin login creds) is likewise a dev convenience, gated the same way.
Write-Host ''
if ($Env -eq 'dev') {
    Step 'seed-smoke-user.sql'
    & $sqlcmd @base -i (Join-Path $seedsDir 'seed-smoke-user.sql') `
        -v SMOKE_EMAIL="$smokeEmail" -v SMOKE_USER_PWD_DB="$userPwdDb" -v SMOKE_API_KEY="$apiKey" -v SMOKE_API_SECRET_DB="$apiSecretDb"
    if ($LASTEXITCODE -ne 0) { Die "seed-smoke-user failed (exit $LASTEXITCODE)" }

    # Synthetic display dataset (DEV ONLY): ~25 users with wallets/ledger/cards/transactions so
    # the cardholder UI + admin lists look realistic. Committed, idempotent (re-applies cleanly --
    # it deletes its own '5eed%' namespace first), and entirely fabricated (fake test-BIN cards,
    # TRC20-shaped addresses, no prod data). The ONE loginable demo cardholder ($demoEmail) gets a
    # real AES password spliced here (the same EncryptDB scheme as the smoke user); the rest are
    # display-only with a non-login sentinel password.
    Step "seed-dev-synthetic.sql (synthetic users/cards/txns; demo login $demoEmail)"
    $synthFile = Join-Path $seedsDir 'seed-dev-synthetic.sql'
    if (-not (Test-Path $synthFile)) { Die "seed file not found: $synthFile (run vm-fetch-source first)" }
    & $sqlcmd @base -i $synthFile -v DEMO_EMAIL="$demoEmail" -v DEMO_USER_PWD_DB="$demoPwdDb"
    if ($LASTEXITCODE -ne 0) { Die "seed-dev-synthetic failed (exit $LASTEXITCODE)" }

    # .smoke.env -- wire forms only (gitignored). Not echoed to stdout (run-command output
    # is retained in the Azure control plane). The admin line reflects THIS run's password;
    # seed-admin is insert-only, so on a re-run with a *changed* SEED_ADMIN_PASSWORD the DB
    # keeps the first password (delete the tblM_Admin row to re-seed a new one).
    $dir = Split-Path $SmokeOut -Parent
    if ($dir -and -not (Test-Path $dir)) { New-Item -ItemType Directory -Path $dir -Force | Out-Null }
    @(
      '# Generated by vm-seed.ps1. GITIGNORED. Credentials for the smoke E2E + admin login.'
      '# SMOKE_BASE_URL: the deployed programmatic API base (QryptoCard.API.Public).'
      'SMOKE_BASE_URL=https://api-dev.s16.xyz'
      "SMOKE_API_KEY=$apiKey"
      '# SMOKE_API_SECRET is the wire form (EncryptAPP), used as the Basic-auth password.'
      "SMOKE_API_SECRET=$apiSecretWire"
      "SMOKE_ADMIN_EMAIL=$adminEmail"
      '# SMOKE_ADMIN_PASSWORD is the wire form (EncryptAPP) the admin client sends (this run).'
      "SMOKE_ADMIN_PASSWORD=$adminPwdWire"
      ''
      '# Demo cardholder for INTERACTIVE web login at app-dev.s16.xyz (synthetic dev dataset).'
      '# DEMO_EMAIL is a real inbox -- the login OTP is emailed there. DEMO_PASSWORD is the'
      '# PLAINTEXT to type into the web login form (the browser encrypts it; this is NOT a wire form).'
      "DEMO_EMAIL=$demoEmail"
      "DEMO_PASSWORD=$demoPwd"
    ) | Set-Content -Path $SmokeOut -Encoding UTF8
    Ok "seed complete; admin=$adminEmail; smoke creds -> $SmokeOut"
} else {
    Step "seed-smoke-user + .smoke.env SKIPPED (Env=$Env, not dev) -- the smoke user/API key is dev-only"
    Ok "seed complete; admin=$adminEmail (reference + admin only; non-dev env)"
}
