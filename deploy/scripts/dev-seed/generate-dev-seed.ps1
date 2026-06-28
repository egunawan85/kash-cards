# generate-dev-seed.ps1 -- author-time generator for the synthetic DEV display dataset.
#
# Emits deploy/sql/seeds/seed-dev-synthetic.sql: ~25 fabricated users with wallets, a
# chained wallet ledger, cards, and card transactions, so the cardholder UI and admin
# lists look realistic on the dev box. We run this on our own machine when we want to
# refresh/extend the dataset; its OUTPUT (the .sql) is reviewed and committed -- the deploy
# itself stays pure-sqlcmd (vm-seed.ps1), no build, no runtime generation. This mirrors the
# sister "generate -> commit idempotent SQL" pattern (qrypto-omni generate-bank-seed.ps1).
#
# PROPERTIES THIS GENERATOR GUARANTEES:
#   * Deterministic -- every value is derived from the row index (NO RNG), so re-running
#     produces a byte-identical file (no diff churn) and the data is fully reviewable.
#   * Namespaced + idempotent -- every synthetic row lives in the '5eed' UserID namespace;
#     the emitted SQL deletes that namespace (children->parents, one transaction) before
#     re-inserting, so applying it repeatedly converges and never touches real/smoke/admin rows.
#   * Synthetic-only sensitive fields -- fake test-BIN card numbers, fabricated CVV/expiry,
#     TRC20-shaped (but fake) deposit addresses, fake @synthetic.kash.local emails. NO prod
#     data is read or copied. Wallet balances are generated INTERNALLY CONSISTENT: each ledger
#     row satisfies BalancePrevious + Amount = Balance, and tblM_User_Balance.Balance equals
#     the final chained balance.
#   * One loginable demo cardholder -- user #1 emits the sqlcmd placeholders $(DEMO_EMAIL) and
#     $(DEMO_USER_PWD_DB); vm-seed.ps1 splices the real inbox + AES password at deploy time
#     (same EncryptDB scheme as the smoke user). The rest are display-only with a non-login
#     sentinel password.
#
# FUTURE HOOK (NOT implemented now, by design): to make the distributions mirror production,
# replace the plain index-derived ranges in New-Amount / the per-user event counts with values
# sampled from a committed, k-anonymised prod-aggregate stats file. Statistics only, never rows;
# coarse buckets (min count >= k) + winsorised tails; sensitive columns stay format-synthesised.
#
# Usage:  pwsh -NoProfile -File deploy/scripts/dev-seed/generate-dev-seed.ps1 [-UserCount 25]
[CmdletBinding()]
param(
    [int]$UserCount = 25,
    [string]$OutFile = (Join-Path $PSScriptRoot '..\..\sql\seeds\seed-dev-synthetic.sql')
)
$ErrorActionPreference = 'Stop'

# -- Deterministic value pools (index-selected; no randomness) ----------------
$First = @('Ava','Liam','Mia','Noah','Emma','Ethan','Sofia','Lucas','Aria','Mateo',
           'Zoe','Kai','Nina','Omar','Lea','Ivan','Yuki','Hana','Diego','Priya',
           'Lukas','Chloe','Arun','Maya','Theo','Ines','Rafa','Sara','Niko','Tara')
$Last  = @('Reyes','Khan','Silva','Novak','Okafor','Tan','Patel','Haas','Costa','Mori',
           'Adeyemi','Ivanov','Cruz','Bauer','Singh','Lopez','Kim','Diaz','Weber','Nair',
           'Rossi','Park','Dubois','Mensah','Yilmaz','Vargas','Chen','Sato','Moreno','Lund')
# Raw card-network merchant descriptors (synthetic, but mirroring the messy real shape:
# UPPERCASE name + city + country, sometimes a *-prefix) plus the currency the merchant
# bills in. Foreign-currency rows get a USD AuthorizedAmount via the fixed synthetic FX
# rate below (the card always settles in USD).
$Merchants = @(
    @{ N='ANTHROPIC* CLAUDE SUB  SAN FRANCISCOCAUS'; Cur='USD' },
    @{ N='OPENAI         *CHATGPT SAN FRANCISCOCAUS'; Cur='USD' },
    @{ N='GITHUB, INC.           SAN FRANCISCOCAUS'; Cur='USD' },
    @{ N='AMAZON WEB SERVICES    SEATTLE      WAUS'; Cur='USD' },
    @{ N='GOOGLE *WORKSPACE      Dublin         IE'; Cur='USD' },
    @{ N='DNH*GODADDY.COM        AMSTERDAM      NL'; Cur='USD' },
    @{ N='NAME-CHEAP.COM* 7K2Q9X PHOENIX      AZUS'; Cur='USD' },
    @{ N='HOSTINGER* HOSTINGER.C WILMINGTON   DEUS'; Cur='USD' },
    @{ N='CLOUDFLARE             SAN FRANCISCOCAUS'; Cur='USD' },
    @{ N='AGODA.COM              Berlin         DE'; Cur='IDR' },
    @{ N='DLOCAL *Starlink Phili Manila         PH'; Cur='PHP' },
    @{ N='LAYERSTACK             CHEUNG SHA WA  HK'; Cur='HKD' }
)
# Fixed synthetic FX rates (local units per 1 USD) for the USD-equivalent (AuthorizedAmount).
$FxPerUsd = @{ USD = 1.0; IDR = 16500.0; PHP = 58.0; HKD = 7.8 }

$sb = New-Object System.Text.StringBuilder
function W([string]$s) { [void]$sb.AppendLine($s) }

# Format money for a decimal(18,9)/float column: invariant culture, 2 dp is plenty.
function M([double]$v) { return $v.ToString('0.00', [Globalization.CultureInfo]::InvariantCulture) }

# Padded synthetic GUID-shaped id within the 5eed namespace.
function Sid([string]$tag4, [int]$i) { return ('5eed{0}-0000-0000-0000-{1:D12}' -f $tag4, $i) }

# -- Header + namespace cleanup ----------------------------------------------
W '-- ============================================================================='
W '-- seed-dev-synthetic.sql -- GENERATED by deploy/scripts/dev-seed/generate-dev-seed.ps1.'
W '-- DO NOT EDIT BY HAND: re-run the generator and commit its output instead.'
W '--'
W '-- DEV/TEST display dataset: synthetic users + wallets + ledger + cards + card'
W '-- transactions so the cardholder UI and admin lists look realistic. DEV ONLY --'
W '-- vm-seed.ps1 applies this only when Env=dev. Entirely fabricated; no prod data.'
W '-- Idempotent: deletes the ''5eed'' UserID namespace (children->parents) then re-inserts,'
W '-- so re-applying converges and never touches real/smoke/admin rows.'
W '-- Run AFTER seed-reference.sql (needs ''role-owner'', card type 111028, the TRC20 network)'
W '-- and seed-smoke-user.sql. DEMO_EMAIL / DEMO_USER_PWD_DB arrive via sqlcmd -v.'
W '-- ============================================================================='
W 'SET NOCOUNT ON;'
W 'SET XACT_ABORT ON;'
W 'BEGIN TRANSACTION;'
W ''
W '-- 1. Clear the synthetic namespace (children first to respect FKs). Card-keyed child'
W '--    rows are removed via the parent card set before the cards themselves.'
W "DELETE FROM dbo.tblT_Card_Transaction WHERE CardNo IN (SELECT CardNo FROM dbo.tblT_Card WHERE UserID LIKE '5eed%');"
W "DELETE FROM dbo.tblT_Card_Balance     WHERE CardNo IN (SELECT CardNo FROM dbo.tblT_Card WHERE UserID LIKE '5eed%');"
W "DELETE FROM dbo.tblT_Card_Deposit        WHERE UserID LIKE '5eed%';"
W "DELETE FROM dbo.tblT_Card                WHERE UserID LIKE '5eed%';"
W "DELETE FROM dbo.tblH_User_Balance        WHERE UserID LIKE '5eed%';"
W "DELETE FROM dbo.tblT_Commission          WHERE UserID LIKE '5eed%';"
W "DELETE FROM dbo.tblM_User_Balance        WHERE UserID LIKE '5eed%';"
W "DELETE FROM dbo.tblM_User_Commission     WHERE UserID LIKE '5eed%';"
W "DELETE FROM dbo.tblM_User_Referral       WHERE UserID LIKE '5eed%';"
W "DELETE FROM dbo.tblM_User_Crypto_Deposit WHERE UserID LIKE '5eed%';"
W "DELETE FROM dbo.tblM_User                WHERE UserID LIKE '5eed%';"
W ''

$totUsers = 0; $totCards = 0; $totTxns = 0; $globalCardSeq = 0
$CardTypeId = 111028   # the 'Virtual Card' type from seed-reference.sql
$NetworkId  = 'F580A411-0E37-4287-B975-408172A2B4BF'  # TRC20/USDT, from seed-reference.sql

# Demo referrals: user #1 (the loginable demo) referred users #2-#5 (InvitedBy). #2-#4 then bought a
# card, so the demo earns a commission on each (tblT_Commission rows keyed on the referee's card id,
# emitted after the loop); #5 is invited but never converted (no card -> no commission).
$demoUid      = Sid 'aaaa' 1
$DemoReferees = @(2, 3, 4, 5)

for ($i = 1; $i -le $UserCount; $i++) {
    $isDemo = ($i -eq 1)
    $uid = Sid 'aaaa' $i
    $fn  = $First[($i - 1) % $First.Count]
    $ln  = $Last[($i * 7) % $Last.Count]
    if ($isDemo) {
        $emailSql = "N'`$(DEMO_EMAIL)'"
        $pwdSql   = "N'`$(DEMO_USER_PWD_DB)'"
        $fn = 'Demo'; $ln = 'Cardholder'
    } else {
        $emailSql = ("N'dev.user{0:D2}@synthetic.kash.local'" -f $i)
        $pwdSql   = "N'DEV-SYNTHETIC-NO-LOGIN'"   # sentinel: cannot decrypt to a valid password
    }
    $phone   = '+1500{0:D7}' -f (5550000 + $i)
    $joinAgo = 5 + ($i * 11) % 360
    $totUsers++

    $invitedBySql = if ($DemoReferees -contains $i) { "N'$demoUid'" } else { 'NULL' }
    W "-- ---- user #$i ($fn $ln)$(if($isDemo){' [LOGINABLE DEMO]'}) ----"
    W ("INSERT INTO dbo.tblM_User (UserID, Email, FirstName, LastName, Password, RoleID, Phone, isActive, isVerified, isBanned, InvitedBy, DateJoin) VALUES ({0}, {1}, N'{2}', N'{3}', {4}, 'role-owner', '{5}', 1, 1, 0, {6}, DATEADD(DAY, -{7}, GETUTCDATE()));" -f "N'$uid'", $emailSql, $fn, $ln, $pwdSql, $phone, $invitedBySql, $joinAgo)
    W ("INSERT INTO dbo.tblM_User_Commission     (CommissionID, UserID, Commission, DateCreated) VALUES (N'{0}', N'{1}', 0.1, DATEADD(DAY, -{2}, GETUTCDATE()));" -f (Sid 'c011' $i), $uid, $joinAgo)
    W ("INSERT INTO dbo.tblM_User_Referral       (UserID, Code, DateCreated) VALUES (N'{0}', '{1}', DATEADD(DAY, -{2}, GETUTCDATE()));" -f $uid, ('DEV{0:D5}' -f $i), $joinAgo)
    $addr = 'T' + ('DEVSEED' + ('{0:D27}' -f $i))   # 34-char TRC20-shaped (fake) address
    W ("INSERT INTO dbo.tblM_User_Crypto_Deposit (ID, UserID, NetworkID, Address, isActive, DateCreated) VALUES (N'{0}', N'{1}', '{2}', '{3}', 1, DATEADD(DAY, -{4}, GETUTCDATE()));" -f (Sid 'cd22' $i), $uid, $NetworkId, $addr, $joinAgo)

    # -- Decide this user's cards, then build a consistent wallet ledger -------
    $cardCount = 0
    if ($isDemo) { $cardCount = 3 }
    elseif ($i % 5 -ne 0) {   # idx %5==0 users stay wallet-empty (realistic spread)
        if ($i % 2 -eq 0) { $cardCount = 1 }
        if ($i % 3 -eq 0) { $cardCount++ }
    }
    $hasWallet = ($isDemo -or ($i % 5 -ne 0))

    # Ledger events: deposits, one 'Card Open' debit per card, plus a top-up + refund for demo.
    $events = New-Object System.Collections.Generic.List[object]
    if ($hasWallet) {
        $events.Add(@{ Type='Crypto Deposit'; Amt = 200.0 + (($i * 37) % 800); Day = $joinAgo - 1 })
        if ($isDemo) { $events.Add(@{ Type='Crypto Deposit'; Amt = 300.0; Day = 40 }) }
        elseif ($i % 4 -eq 0) { $events.Add(@{ Type='Crypto Deposit'; Amt = 150.0 + (($i * 13) % 300); Day = 20 }) }
        for ($c = 0; $c -lt $cardCount; $c++) { $events.Add(@{ Type='Card Open'; Amt = -10.0; Day = ($joinAgo - 2 - $c * 3) }) }
        if ($isDemo) {
            $events.Add(@{ Type='Card Topup';      Amt = -50.0; Day = 10 })
            $events.Add(@{ Type='Deposit Refund';  Amt =  50.0; Day = 9 })
        }
    }

    # Chain the ledger so BalancePrevious + Amount = Balance at every step.
    $bal = 0.0; $ev = 0
    foreach ($e in $events) {
        $prev = $bal; $bal = [Math]::Round($bal + [double]$e.Amt, 2); $ev++
        $day = [Math]::Max(0, [int]$e.Day)
        $txid = '5EED-{0:D3}-{1:D2}-{2}' -f $i, $ev, ($e.Type -replace '[^A-Za-z]','').Substring(0,3).ToUpper()
        W ("INSERT INTO dbo.tblH_User_Balance (UserID, BalanceID, TransactionID, Type, BalancePrevious, Amount, Commision, CommisionInPercentage, Balance, CreatedDate, Status) VALUES (N'{0}', N'{1}', N'{2}', '{3}', {4}, {5}, 0, 0, {6}, DATEADD(DAY, -{7}, GETUTCDATE()), 'Completed');" -f $uid, (Sid 'ba33' $i), $txid, $e.Type, (M $prev), (M ([double]$e.Amt)), (M $bal), $day)
    }
    # The cached balance row reflects the final chained balance.
    W ("INSERT INTO dbo.tblM_User_Balance (BalanceID, UserID, Currency, Balance, isActive, DateCreated) VALUES (N'{0}', N'{1}', 'USDT', {2}, 1, DATEADD(DAY, -{3}, GETUTCDATE()));" -f (Sid 'ba33' $i), $uid, (M $bal), $joinAgo)

    # -- Cards + card transactions -------------------------------------------
    for ($c = 1; $c -le $cardCount; $c++) {
        $globalCardSeq++; $totCards++
        $cardId = '5eedca{0:D2}-0000-0000-0000-{1:D12}' -f $c, $i
        $cardNo = '402400{0:D10}' -f (1000000000 + $globalCardSeq)   # fake 16-digit, test BIN 402400
        $last4  = $cardNo.Substring($cardNo.Length - 4)
        $masked = '4024 00** **** ' + $last4
        $cvv    = '{0:D3}' -f (100 + ($globalCardSeq * 7) % 899)
        $expY   = 28 + ($c % 4)
        $expM   = '{0:D2}' -f (1 + ($globalCardSeq % 12))
        $status = if ($c -eq $cardCount -and $i % 6 -eq 0) { 'cancelled' } else { 'active' }
        $isAct  = if ($status -eq 'active') { 1 } else { 0 }
        $cardAgo = [Math]::Max(1, $joinAgo - 2 - $c * 3)
        W ("INSERT INTO dbo.tblT_Card (ID, UserID, CardTypeId, CardNo, CardNumber, CVV, ValidPeriod, Currency, Price, Total, Status, isActive, DateCreated) VALUES (N'{0}', N'{1}', {2}, '{3}', N'{4}', '{5}', '{6}/{7}', 'USD', 10.00, 10.00, '{8}', {9}, DATEADD(DAY, -{10}, GETUTCDATE()));" -f $cardId, $uid, $CardTypeId, $cardNo, $masked, $cvv, $expM, $expY, $status, $isAct, $cardAgo)
        $spent = 0.0
        $load  = 100.0 + ($globalCardSeq % 3) * 50.0   # 100/150/200 notional card load
        $g = $globalCardSeq

        # This card's activity mirrors the real type/status/currency mix: mostly authorised
        # spends (one declined), a $0 verification, a maintenance fee, and -- on some cards --
        # a refund and a void. Foreign-currency rows carry a USD AuthorizedAmount (card settles
        # in USD). Only successful spends draw down the card balance.
        $specs = New-Object System.Collections.Generic.List[object]
        $nSpend = 4 + ($g % 3)
        for ($t = 1; $t -le $nSpend; $t++) {
            $m   = $Merchants[(($g * 3) + $t) % $Merchants.Count]
            $cur = [string]$m.Cur
            switch ($cur) {
                'IDR'   { $amt = [double]((($g * 7  + $t * 131) % 380000) + 20000) }
                'PHP'   { $amt = [double]((($g * 11 + $t * 53)  % 5500)   + 200) }
                'HKD'   { $amt = [double]((($g * 5  + $t * 29)  % 480)    + 20) }
                default { $amt = [Math]::Round(1.99 + (($g * 13 + $t * 7) % 120), 2) }
            }
            $auth = [Math]::Round($amt / [double]$FxPerUsd[$cur], 2)
            if ($t -eq 2) {
                $st = 'failed'; $ss = 'Fail'           # one declined spend per card
            } else {
                $st = 'authorized'; $ss = 'Authorized'
                $spent = [Math]::Round($spent + $auth, 2)
            }
            [void]$specs.Add(@{ Cur=$cur; Amt=$amt; Auth=$auth; Type='auth'; TypeStr='Authorization'; Status=$st; StatusStr=$ss; Merch=[string]$m.N })
        }
        $vm = [string]$Merchants[$g % $Merchants.Count].N
        [void]$specs.Add(@{ Cur='USD'; Amt=0.0;  Auth=0.0;  Type='verification'; TypeStr='Verification'; Status='succeed'; StatusStr='Succeed'; Merch=$vm })
        [void]$specs.Add(@{ Cur='USD'; Amt=0.50; Auth=0.50; Type='maintain_fee'; TypeStr='Card fee';     Status='succeed'; StatusStr='Succeed'; Merch=$vm })
        if ($g % 3 -eq 0) { [void]$specs.Add(@{ Cur='USD'; Amt=[Math]::Round(5.0+($g%20),2); Auth=[Math]::Round(5.0+($g%20),2); Type='refund'; TypeStr='Refund'; Status='succeed'; StatusStr='Success'; Merch=[string]$Merchants[($g+1)%$Merchants.Count].N }) }
        if ($g % 4 -eq 0) { [void]$specs.Add(@{ Cur='USD'; Amt=[Math]::Round(9.0+($g%15),2); Auth=[Math]::Round(9.0+($g%15),2); Type='Void'; TypeStr='Reversal'; Status='succeed'; StatusStr='Succeed'; Merch=[string]$Merchants[($g+2)%$Merchants.Count].N }) }

        $k = 0
        foreach ($s in $specs) {
            $k++
            $txDay = [Math]::Max(0, $cardAgo - $k)
            $trade = '5EEDTXN{0:D4}{1:D2}' -f $g, $k
            $merchSql = ([string]$s.Merch -replace "'", "''")
            $totTxns++
            W ("INSERT INTO dbo.tblT_Card_Transaction (CardNo, TradeNo, OriginTradeNo, Currency, Amount, AuthorizedCurrency, AuthorizedAmount, Fee, FeeCurrency, Type, TypeStr, Status, StatusStr, MerchantName, Description, TransactionTime) VALUES ('{0}', '{1}', '{1}', '{2}', {3}, 'USD', {4}, 0.00, 'USD', '{5}', '{6}', '{7}', '{8}', N'{9}', N'{10}', DATEADD(DAY, -{11}, GETUTCDATE()));" -f $cardNo, $trade, $s.Cur, (M ([double]$s.Amt)), (M ([double]$s.Auth)), $s.Type, $s.TypeStr, $s.Status, $s.StatusStr, $merchSql, ('Card ' + $s.Type), $txDay)
        }

        # On-chain top-ups (credits) for this card so the unified feed shows money in.
        $nDep = 1 + ($g % 2)
        for ($d = 1; $d -le $nDep; $d++) {
            $depAmt = 50.0 + (($g + $d) % 3) * 50.0
            $depFee = [Math]::Round($depAmt * 0.03, 2)
            $depTot = [Math]::Round($depAmt + $depFee, 2)
            $depStatus = if ($g % 7 -eq 0 -and $d -eq $nDep) { 'expired' } else { 'success' }
            $depDay = [Math]::Max(0, $cardAgo - 1 - ($d - 1) * 4)
            $depId  = '5eedcd55-0000-0000-{0:D4}-{1:D12}' -f $d, $g
            $txhash = ('5eeddep' + ('{0:D4}{1:D2}' -f $g, $d)).PadRight(64, '0')
            $depAddr = 'T' + ('DEVSEEDDEP' + ('{0:D23}' -f ($g * 10 + $d)))
            $hashSql = if ($depStatus -eq 'success') { "'$txhash'" } else { 'NULL' }
            W ("INSERT INTO dbo.tblT_Card_Deposit (ID, UserID, OrderNo, CardNo, Network, Symbol, Currency, Amount, Fee, Total, ReceivedAmount, Type, Status, DateTransaction, Address, Txhash) VALUES (N'{0}', N'{1}', '{2}', '{3}', 'TRC20', 'USDT', 'USD', {4}, {5}, {6}, {7}, 'Crypto', '{8}', DATEADD(DAY, -{9}, GETUTCDATE()), '{10}', {11});" -f $depId, $uid, ('5EEDDEPO{0:D4}{1:D2}' -f $g, $d), $cardNo, (M $depAmt), (M $depFee), (M $depTot), (M $depAmt), $depStatus, $depDay, $depAddr, $hashSql)
        }

        $cardBal = [Math]::Max(0.0, [Math]::Round($load - $spent, 2))
        W ("INSERT INTO dbo.tblT_Card_Balance (ID, CardNo, Amount, UsedAmount, Currency) VALUES (N'{0}', '{1}', {2}, {3}, 'USD');" -f (Sid 'cb44' $globalCardSeq), $cardNo, (M $cardBal), (M $spent))
    }
    W ''
}

W ''
W '-- Demo referral commissions (referrer = user #1; TransactionID = the referee''s card order, so'
W '-- the same rows also attribute per-referee for the breakdown view). Referee #5 never converted.'
$DemoCommissions = @(
    @{ Referee = 2; Amt = 1.00 },
    @{ Referee = 3; Amt = 1.50 },
    @{ Referee = 4; Amt = 2.00 }
)
$rc = 0
foreach ($r in $DemoCommissions) {
    $rc++
    $refCardId = '5eedca01-0000-0000-0000-{0:D12}' -f [int]$r.Referee
    W ("INSERT INTO dbo.tblT_Commission (CommisionID, UserID, TransactionID, Commission, DateCreated) VALUES (N'{0}', N'{1}', N'{2}', {3}, DATEADD(DAY, -{4}, GETUTCDATE()));" -f (Sid 'c0aa' $rc), $demoUid, $refCardId, (M ([double]$r.Amt)), (3 + $rc * 3))
}
W ''
W "COMMIT TRANSACTION;"
W ("PRINT 'seed-dev-synthetic: applied {0} users, {1} cards, {2} card transactions (synthetic, dev-only).';" -f $totUsers, $totCards, $totTxns)

$outResolved = [System.IO.Path]::GetFullPath($OutFile)
[System.IO.File]::WriteAllText($outResolved, $sb.ToString(), (New-Object System.Text.UTF8Encoding($false)))
Write-Host "[ok] wrote $outResolved ($totUsers users, $totCards cards, $totTxns txns)"
