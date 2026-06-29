# vm-seed.ps1 -- seed the on-box dev DB from committed SQL (deploy/sql/seeds/*.sql),
# applied with sqlcmd. Replaces the QryptoCard.DevSeed C# console project: no build,
# no QryptoCard.Sec reference, no bare-`az` PATH dependency (the bug that made the old
# seeder die silently under run-command). Mirrors the sister db/seeds + sqlcmd -v pattern;
# passwords/API secrets are one-way bcrypt hashes, computed here at deploy time (via the
# app's BCrypt.Net library) and passed via -v -- never committed.
#
# Invocation (on the VM, after vm-migrate):
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
# Soft variant: returns $null instead of dying when the secret is absent. Used for the
# OPTIONAL bootstrap-admin overrides (SEED-ADMIN-*), which dev leaves unset (falls back to
# the fixed dev defaults) but a non-dev environment MUST provide (enforced below).
function Get-SecretSoft([string]$name) {
    # az exits non-zero on a missing secret; under PS7's $PSNativeCommandUseErrorActionPreference
    # + ErrorActionPreference=Stop that surfaces as a TERMINATING error despite 2>$null, defeating
    # the "soft" intent. try/catch keeps it soft so an absent SEED-ADMIN-* falls back to the default.
    try {
        $v = (& $AZ keyvault secret show --vault-name $KvName --name $name --query value -o tsv 2>$null)
    } catch {
        return $null
    }
    if ([string]::IsNullOrWhiteSpace($v)) { return $null }
    return $v.Trim()
}
# Passwords + API secrets are now one-way bcrypt hashes (no more reversible AES under
# the retired DBKEY/APPKEY). Load the bcrypt library shipped with the built app — restored
# by nuget during the build that precedes seeding — so the exact same algorithm and work
# factor (12) as QryptoCard.INT.Security.PasswordHasher is used.
$bcryptDll = Join-Path $SourceDir 'packages\BCrypt.Net-Next.4.0.3\lib\net462\BCrypt.Net-Next.dll'
if (-not (Test-Path $bcryptDll)) {
    $bcryptDll = Get-ChildItem -Path $SourceDir -Recurse -Filter 'BCrypt.Net-Next.dll' -ErrorAction SilentlyContinue |
                 Select-Object -First 1 -ExpandProperty FullName
}
if (-not $bcryptDll -or -not (Test-Path $bcryptDll)) { Die "BCrypt.Net-Next.dll not found under $SourceDir -- build (nuget restore) before seeding" }
Add-Type -Path $bcryptDll
# Bootstrap-admin credentials from Key Vault (seeded from secrets/.env.<env> +
# .vault.<env>). Resolution precedence below: explicit env var > Key Vault > dev default.
$kvAdminEmail = Get-SecretSoft 'SEED-ADMIN-EMAIL'
$kvAdminPwd   = Get-SecretSoft 'SEED-ADMIN-PASSWORD'
# Wasabi RSA keypair (XML), used to encrypt the demo cards' CVV/expiry below so the dev card-detail
# reveal decrypts them exactly like prod. Soft-fetch: if it's absent/unset we fall back to plaintext
# (the reveal page tolerates that via a decrypt try/catch), so seeding never breaks on a missing key.
$WASABIKEYXML = (& $AZ keyvault secret show --vault-name $KvName --name 'WASABICARD-PRIVATE-KEY-XML' --query value -o tsv 2>$null)
if ($WASABIKEYXML) { $WASABIKEYXML = $WASABIKEYXML.Trim() }

# One-way bcrypt hash (work factor 12), identical to QryptoCard.INT.Security.PasswordHasher.
# Used for stored passwords and API secrets; verification happens in the INT tier.
function Bcrypt([string]$plain) {
    return [BCrypt.Net.BCrypt]::HashPassword($plain, 12)
}

# RSA-encrypt a short value with the Wasabi keypair (from the XML) using PKCS1 v1.5 — the exact
# inverse of QryptoCard.Dashboard Common.decrypt, so the card-detail reveal can decrypt it. If the
# key is unavailable/malformed, return the plaintext unchanged (the reveal page tolerates it).
function RsaEncWasabi([string]$plain, [string]$xmlKey) {
    if ([string]::IsNullOrWhiteSpace($xmlKey)) { return $plain }
    try {
        $rsa = New-Object System.Security.Cryptography.RSACryptoServiceProvider
        $rsa.FromXmlString($xmlKey)
        $ct = $rsa.Encrypt([Text.Encoding]::UTF8.GetBytes($plain), $false)  # $false = PKCS1 v1.5
        $rsa.Dispose()
        return [Convert]::ToBase64String($ct)
    } catch { Write-Host "[..] CVV/expiry encryption skipped (Wasabi key issue): $($_.Exception.Message)"; return $plain }
}

# Credentials: explicit env var wins, else Key Vault (SEED-ADMIN-*), else dev default.
$adminEmail = if ($env:SEED_ADMIN_EMAIL)    { $env:SEED_ADMIN_EMAIL }    elseif ($kvAdminEmail) { $kvAdminEmail } else { 'edward@s16.ventures' }
$adminPwd   = if ($env:SEED_ADMIN_PASSWORD) { $env:SEED_ADMIN_PASSWORD } elseif ($kvAdminPwd)   { $kvAdminPwd }   else { 'KashAdmin!dev1' }
# Fail closed off dev: a non-dev environment must supply BOTH the admin email and password
# (via Key Vault SEED-ADMIN-EMAIL / SEED-ADMIN-PASSWORD, or an explicit env override). Never
# seed the well-known dev default admin into stg/prd. seed-admin.sql is INSERT-ONLY, so a
# weak first password could not be corrected by a later re-seed -- block it before it lands.
if ($Env -ne 'dev') {
    $haveEmail = $env:SEED_ADMIN_EMAIL -or $kvAdminEmail
    $havePwd   = $env:SEED_ADMIN_PASSWORD -or $kvAdminPwd
    if (-not ($haveEmail -and $havePwd)) {
        Die "Env=$Env requires SEED-ADMIN-EMAIL and SEED-ADMIN-PASSWORD in Key Vault $KvName (refusing to seed the dev-default admin in a non-dev environment). Set them in secrets/.env.$Env + secrets/.vault.$Env and run seed-kv-secrets.sh, then re-run."
    }
}
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

# Stored forms are one-way bcrypt hashes. The "wire" forms are now PLAINTEXT: dashboard/API
# clients send the raw password/secret over the internal channel and the INT tier verifies it
# against the stored hash (the old EncryptAPP wire encryption has been removed).
$adminPwdDb    = Bcrypt $adminPwd
$userPwdDb     = Bcrypt $userPwd
$apiSecretDb   = Bcrypt $apiSecret
$apiSecretWire = $apiSecret
$adminPwdWire  = $adminPwd
$demoPwdDb     = Bcrypt $demoPwd

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
    # real bcrypt-hashed password spliced here (same as the smoke user); the rest are
    # display-only with a non-login sentinel password.
    Step "seed-dev-synthetic.sql (synthetic users/cards/txns; demo login $demoEmail)"
    $synthFile = Join-Path $seedsDir 'seed-dev-synthetic.sql'
    if (-not (Test-Path $synthFile)) { Die "seed file not found: $synthFile (run vm-fetch-source first)" }
    # Encrypt the demo (loginable) user's 3 card CVV/expiry with the Wasabi key so the card-detail
    # reveal decrypts them like prod. The other display-only synthetic users are never revealed.
    $cvv1 = RsaEncWasabi '107' $WASABIKEYXML; $exp1 = RsaEncWasabi '02/29' $WASABIKEYXML
    $cvv2 = RsaEncWasabi '114' $WASABIKEYXML; $exp2 = RsaEncWasabi '03/30' $WASABIKEYXML
    $cvv3 = RsaEncWasabi '121' $WASABIKEYXML; $exp3 = RsaEncWasabi '04/31' $WASABIKEYXML
    & $sqlcmd @base -i $synthFile -v DEMO_EMAIL="$demoEmail" -v DEMO_USER_PWD_DB="$demoPwdDb" `
        -v DEMO_CVV1="$cvv1" -v DEMO_EXP1="$exp1" -v DEMO_CVV2="$cvv2" -v DEMO_EXP2="$exp2" -v DEMO_CVV3="$cvv3" -v DEMO_EXP3="$exp3"
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
      '# SMOKE_API_SECRET is the plaintext secret, used as the Basic-auth password.'
      "SMOKE_API_SECRET=$apiSecretWire"
      "SMOKE_ADMIN_EMAIL=$adminEmail"
      '# SMOKE_ADMIN_PASSWORD is the plaintext password the admin client sends (this run).'
      "SMOKE_ADMIN_PASSWORD=$adminPwdWire"
      ''
      '# Demo cardholder for INTERACTIVE web login at app-dev.s16.xyz (synthetic dev dataset).'
      '# DEMO_EMAIL is a real inbox -- the login OTP is emailed there. DEMO_PASSWORD is the'
      '# PLAINTEXT to type into the web login form (sent as-is over the internal channel).'
      "DEMO_EMAIL=$demoEmail"
      "DEMO_PASSWORD=$demoPwd"
    ) | Set-Content -Path $SmokeOut -Encoding UTF8
    Ok "seed complete; admin=$adminEmail; smoke creds -> $SmokeOut"
} else {
    Step "seed-smoke-user + .smoke.env SKIPPED (Env=$Env, not dev) -- the smoke user/API key is dev-only"
    Ok "seed complete; admin=$adminEmail (reference + admin only; non-dev env)"
}
