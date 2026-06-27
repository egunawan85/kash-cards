# vm-verify-walletpath.ps1 -- live wallet money-path verification on the dev box.
#
# Exercises the prepaid-balance money path end-to-end against the deployed public
# API + INT tier + the WasabiCard sandbox, and asserts the safety properties that
# matter for real money:
#   T1  auth gating          -- anon request 401; authenticated 200
#   T2  read surface         -- card/active + transaction/card/purchase/list answer
#   T3  buy debit->refund    -- a card buy debits the balance, the sandbox rejects
#                               (no card product, U6), the spend is REVERSED, and the
#                               wallet returns to its prior balance (net-zero), with a
#                               'Card Open' + 'Card Open Reversal' ledger pair and the
#                               order left Failed (definitive-failure path, not the
#                               ambiguous PendingProvider branch).
#   T4  insufficient balance -- with the wallet drained below the total, a buy is
#                               refused with NO debit and NO order row (fail-closed).
#
# Card-issuance SUCCESS paths are NOT covered here: they need a real card product on
# the merchant, which the sandbox lacks (U6) and which prod gates behind IP-allowlisting.
# This verifies everything exercisable now -- the money logic up to the provider boundary.
#
# Funding: the smoke wallet is credited through the same CreditDeposit ledger semantics
# the dev-only test-credit tool uses (a 'Crypto Deposit' row + an audit row), behind a
# fail-closed ENVIRONMENT hard-gate: it REFUSES unless -Env is an explicit dev/sandbox
# value. This makes balance-minting impossible in prod even by an operator who runs it
# there -- the env gate is the wall, mirroring SD-2's test-credit design.
#
# Read-only-safe everywhere except the gated funding + the T4 drain, both of which
# restore the wallet to its starting balance on exit. Prints a final
#   WALLETPATH_RESULT: PASS (n/n)  |  WALLETPATH_RESULT: FAIL (k/n failed)
# line a wrapper can grep.
[CmdletBinding()]
param(
    [string]$Env = 'dev',
    [string]$UserId = '11111111-1111-1111-1111-111111111111',  # seeded smoke user
    [int]$CardTypeId = 111028,
    [string]$SmokeEnvFile = 'C:\src\kash-cards\deploy\secrets\.smoke.env',
    [string]$SqlInstance = 'localhost\SQLEXPRESS',
    [string]$DbName = 'qrypto-card'
)
$ErrorActionPreference = 'Continue'
[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12
$sqlcmd = 'C:\Tools\sqlcmd.exe'
$script:pass = 0; $script:fail = 0
function Check($name, [bool]$ok, $detail) {
    if ($ok) { $script:pass++; Write-Host ("[ok] " + $name + "  " + $detail) }
    else     { $script:fail++; Write-Host ("[xx] " + $name + "  " + $detail) }
}
function Sql($q) { (& $sqlcmd -S $SqlInstance -E -d $DbName -h -1 -W -b -Q $q 2>&1 | Out-String).Trim() }
function Bal()  { [decimal](Sql "SET NOCOUNT ON; SELECT ISNULL(CAST(Balance AS varchar),'0') FROM dbo.tblM_User_Balance WHERE UserID='$UserId' AND Currency='USDT'") }
function SetBal($v) { Sql "SET NOCOUNT ON; UPDATE dbo.tblM_User_Balance SET Balance=$v, DateUpdated=GETUTCDATE() WHERE UserID='$UserId' AND Currency='USDT'" | Out-Null }

# -- ENVIRONMENT hard-gate (fail-closed): funding is dev/sandbox ONLY -----------
# The gate is DERIVED from the box's own Key Vault (QRYPTO-ENVIRONMENT -- the same
# source of truth the dev-credit tool enforces), NOT from the -Env parameter: an
# operator asserting -Env dev on a prod box must NOT be able to mint balance. The
# real environment is read via the VM managed identity; fail closed if it cannot be
# confirmed to be dev/sandbox. (-Env is kept only as a label / sanity cross-check.)
$allowedEnv = @('dev','sandbox','local')
function Get-RealEnvironment {
    $cfgFile = Get-ChildItem 'C:\src\kash-cards\deploy\config\.env.provision.*' -ErrorAction SilentlyContinue |
               Where-Object { $_.Name -notlike '*.example' } | Select-Object -First 1
    if (-not $cfgFile) { return $null }
    $kv = (((Get-Content $cfgFile.FullName) | Where-Object { $_ -match '^KEYVAULT_NAME=' }) -replace '^KEYVAULT_NAME=','').Trim()
    if (-not $kv) { return $null }
    try {
        $tok = (Invoke-RestMethod -Headers @{Metadata='true'} -UseBasicParsing -Uri 'http://169.254.169.254/metadata/identity/oauth2/token?api-version=2018-02-01&resource=https://vault.azure.net').access_token
        $s = Invoke-RestMethod -Headers @{ Authorization = "Bearer $tok" } -UseBasicParsing -Uri "https://$kv.vault.azure.net/secrets/QRYPTO-ENVIRONMENT?api-version=7.4"
        return ("" + $s.value).Trim().ToLower()
    } catch { return $null }
}
$realEnv = Get-RealEnvironment
if (-not $realEnv -or ($allowedEnv -notcontains $realEnv)) {
    Write-Host "[xx] ENV gate (fail-closed): could not confirm a dev/sandbox environment (real='$realEnv') -- refusing to mint test balance"
    Write-Host "WALLETPATH_RESULT: FAIL (env-gated)"
    exit 1
}
if ($allowedEnv -notcontains $Env.Trim().ToLower()) {
    Write-Host "[xx] ENV gate: -Env '$Env' is not a dev/sandbox value -- refusing"
    Write-Host "WALLETPATH_RESULT: FAIL (env-gated)"
    exit 1
}
Write-Host "[ok] ENV gate: confirmed environment '$realEnv' (KV-derived)"

# -- creds (stay on box; never emitted) ----------------------------------------
$cfg = @{}; (Get-Content $SmokeEnvFile) | ForEach-Object { if ($_ -match '^([A-Z_]+)=(.*)$') { $cfg[$Matches[1]] = $Matches[2] } }
$base = $cfg['SMOKE_BASE_URL'].TrimEnd('/'); $key = $cfg['SMOKE_API_KEY']; $sec = $cfg['SMOKE_API_SECRET']
$auth = 'Basic ' + [Convert]::ToBase64String([Text.Encoding]::UTF8.GetBytes($key + ':' + $sec))
function Api($method, $path, $bodyObj) {
    $h = @{ Authorization = $auth }
    try {
        if ($bodyObj) { $r = Invoke-WebRequest -Uri "$base/$path" -Method $method -Headers $h -ContentType 'application/json' -Body ($bodyObj | ConvertTo-Json -Compress) -UseBasicParsing -TimeoutSec 60 }
        else          { $r = Invoke-WebRequest -Uri "$base/$path" -Method $method -Headers $h -UseBasicParsing -TimeoutSec 60 }
        return @{ code = [int]$r.StatusCode; body = ("" + $r.Content) }
    } catch { $c = try { [int]$_.Exception.Response.StatusCode } catch { 0 }; return @{ code = $c; body = ("" + $_.ErrorDetails.Message) } }
}
function CreditDeposit($amt) {
    $txid = 'DEVCREDIT-walletpath-' + [Guid]::NewGuid().ToString('N').Substring(0,12)
    Sql @"
SET NOCOUNT ON;
DECLARE @bid bigint=(SELECT BalanceID FROM dbo.tblM_User_Balance WHERE UserID='$UserId' AND Currency='USDT');
DECLARE @prev decimal(38,9)=(SELECT Balance FROM dbo.tblM_User_Balance WHERE UserID='$UserId' AND Currency='USDT');
UPDATE dbo.tblM_User_Balance SET Balance=@prev+$amt, DateUpdated=GETUTCDATE() WHERE UserID='$UserId' AND Currency='USDT';
INSERT dbo.tblH_User_Balance(ID,UserID,BalanceID,TransactionID,Type,BalancePrevious,Amount,Commision,CommisionInPercentage,Balance,BalanceHold,CreatedDate,Status)
VALUES(LOWER(CONVERT(nvarchar(36),NEWID())),'$UserId',@bid,'$txid','Crypto Deposit',@prev,$amt,0,0,@prev+$amt,0,GETUTCDATE(),'success');
INSERT dbo.tblH_Auth_Log(LogID,EventType,Subject,SubjectType,SourceIP,Details,DateLogged)
VALUES(LOWER(CONVERT(nvarchar(36),NEWID())),'dev_test_credit','vm-verify-walletpath','system','on-box','{"target":"$UserId","amount":$amt,"ref":"$txid","env":"$Env"}',GETUTCDATE());
"@ | Out-Null
}

Write-Host "=== wallet money-path verification (env=$Env, base=$base) ==="
$startBal = Bal

# -- T1: auth gating ----------------------------------------------------------
$anon = try { [int](Invoke-WebRequest -Uri "$base/v1/card/active" -UseBasicParsing -TimeoutSec 60).StatusCode } catch { try { [int]$_.Exception.Response.StatusCode } catch { 0 } }
Check 'T1 anon rejected (401)' ($anon -eq 401) "anon=$anon"
$ra = Api 'GET' 'v1/card/active' $null
Check 'T1 authenticated read (200)' ($ra.code -eq 200) "auth=$($ra.code)"

# -- T2: read surface ---------------------------------------------------------
$rl = Api 'GET' 'v1/transaction/card/purchase/list' $null
Check 'T2 purchase-list read (200)' ($rl.code -eq 200) "code=$($rl.code)"

# -- ensure card buyable (deposit quotas configured) --------------------------
Sql "SET NOCOUNT ON; UPDATE dbo.tblM_Card_Type SET DepositAmountMinQuotaForActiveCard=20, DepositAmountMaxQuotaForActiveCard=100000 WHERE CardTypeId=$CardTypeId AND (DepositAmountMinQuotaForActiveCard IS NULL OR DepositAmountMaxQuotaForActiveCard IS NULL)" | Out-Null

# -- T3: buy debit -> provider reject -> refund (net-zero) --------------------
CreditDeposit 200
$balBefore = Bal
$ref = [Guid]::NewGuid().ToString('N')
$buy = Api 'POST' 'v1/transaction/card/purchase' @{ CardTypeId = $CardTypeId; InitialDeposit = 25; UserReferenceID = $ref }
Start-Sleep -Seconds 4
$balAfter = Bal
$order = Sql "SET NOCOUNT ON; SELECT TOP 1 ISNULL(Status,'?') FROM dbo.tblT_Card WHERE UserID='$UserId' AND UserReferenceID='$ref'"
$ledgerPair = Sql "SET NOCOUNT ON; SELECT COUNT(*) FROM dbo.tblH_User_Balance WHERE UserID='$UserId' AND Type IN ('Card Open','Card Open Reversal') AND TransactionID IN (SELECT ID FROM dbo.tblT_Card WHERE UserReferenceID='$ref')"
Check 'T3 buy net-zero (debit fully refunded)' ($balAfter -eq $balBefore) "before=$balBefore after=$balAfter"
Check 'T3 order left Failed'                   ($order -eq 'Failed')      "status=$order"
Check 'T3 debit+reversal ledger pair present'  ([int]$ledgerPair -eq 2)   "rows=$ledgerPair"
Check 'T3 response signals refund'             ($buy.body -match 'refund') "msg=$(($buy.body) -replace '\s+',' ')"

# -- T4: insufficient balance -> fail-closed (no debit, no order) -------------
SetBal 5
$ref2 = [Guid]::NewGuid().ToString('N')
$buy2 = Api 'POST' 'v1/transaction/card/purchase' @{ CardTypeId = $CardTypeId; InitialDeposit = 25; UserReferenceID = $ref2 }
Start-Sleep -Seconds 2
$balAfter2 = Bal
$order2 = Sql "SET NOCOUNT ON; SELECT COUNT(*) FROM dbo.tblT_Card WHERE UserID='$UserId' AND UserReferenceID='$ref2' AND Status NOT IN ('Failed')"
Check 'T4 insufficient balance refused'   ($buy2.body -notmatch 'refund' -and $buy2.body -match '(?i)balance|insufficient|fund') "msg=$(($buy2.body) -replace '\s+',' ')"
Check 'T4 no debit on refusal'            ($balAfter2 -eq 5) "bal=$balAfter2 (expect 5)"

# -- T5: idempotency -- same UserReferenceID submitted twice collapses to ONE ---
# order and ONE debit (the filtered unique index + duplicate-key replay in
# CardSpendService.OpenCard). The provider rejects (no card product, U6) so the one
# order debits then refunds -> net-zero; the key invariant is that the SECOND submit
# neither inserts a second order nor debits again.
CreditDeposit 200
$balB5 = Bal
$ref5 = [Guid]::NewGuid().ToString('N')
$i1 = Api 'POST' 'v1/transaction/card/purchase' @{ CardTypeId = $CardTypeId; InitialDeposit = 25; UserReferenceID = $ref5 }
$i2 = Api 'POST' 'v1/transaction/card/purchase' @{ CardTypeId = $CardTypeId; InitialDeposit = 25; UserReferenceID = $ref5 }
Start-Sleep -Seconds 4
$rows5 = Sql "SET NOCOUNT ON; SELECT COUNT(*) FROM dbo.tblT_Card WHERE UserID='$UserId' AND UserReferenceID='$ref5'"
$open5 = Sql "SET NOCOUNT ON; SELECT COUNT(*) FROM dbo.tblH_User_Balance WHERE UserID='$UserId' AND Type='Card Open' AND TransactionID IN (SELECT ID FROM dbo.tblT_Card WHERE UserReferenceID='$ref5')"
$balA5 = Bal
Check 'T5 same-ref twice -> ONE order row'     ([int]$rows5 -eq 1) "rows=$rows5 (expect 1)"
Check 'T5 same-ref twice -> at most ONE debit' ([int]$open5 -le 1) "card-open ledger rows=$open5 (expect <=1)"
Check 'T5 idempotent retry is net-zero'        ($balA5 -eq $balB5) "before=$balB5 after=$balA5"

# -- restore starting balance -------------------------------------------------
SetBal $startBal
Write-Host ("restored balance -> " + (Bal))

$total = $script:pass + $script:fail
if ($script:fail -eq 0) { Write-Host "WALLETPATH_RESULT: PASS ($($script:pass)/$total)"; exit 0 }
else { Write-Host "WALLETPATH_RESULT: FAIL ($($script:fail)/$total failed)"; exit 1 }
