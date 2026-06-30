-- =============================================================================
-- preflight-checks.sql  --  legacy KashNow import, PRE-migration verification.
--
-- Run this against the freshly-imported legacy database (the bacpac restored onto the
-- target SQL Express instance) BEFORE running vm-migrate.ps1. It does two jobs:
--   1. Proves the ordered migrations will apply cleanly -- the same length/duplicate
--      conditions the money-safety migrations (0002/0003/0004/0007/0010) RAISERROR on,
--      checked up front so a dirty import fails here, actionably, not mid-migrate.
--   2. Captures the row-count + money baseline. Re-run postflight-checks.sql AFTER the
--      migrate (and credential scrub) and diff: the user/card/money numbers must be
--      IDENTICAL (the migrations are additive schema-only; they touch no row values).
--
-- Read-only. Run:  sqlcmd -S <server> -d <db> -E -b -i preflight-checks.sql
--
-- Verified against the 2026-06-30 production export (kashnow): all length probes fit,
-- all duplicate probes returned 0, so the migrations applied 10/10 clean. Re-verify on
-- the actual import -- the live DB may have moved on.
-- =============================================================================
SET NOCOUNT ON;

PRINT '===== ROW COUNTS (baseline -- must match postflight) =====';
SELECT t.name AS TableName, SUM(p.rows) AS [Rows]
FROM sys.tables t JOIN sys.partitions p ON p.object_id = t.object_id AND p.index_id IN (0,1)
GROUP BY t.name ORDER BY t.name;

PRINT '';
PRINT '===== MONEY BASELINE (anchor -- must match postflight) =====';
SELECT 'users' AS Metric, COUNT(*) AS n, CAST(NULL AS decimal(20,4)) AS Amount FROM tblM_User
UNION ALL SELECT 'cardholders', COUNT(*), NULL FROM tblM_Cardholder
UNION ALL SELECT 'cards', COUNT(*), NULL FROM tblT_Card
UNION ALL SELECT 'active wallet balances', COUNT(*), CAST(SUM(Balance) AS decimal(20,4)) FROM tblM_User_Balance WHERE isActive=1
UNION ALL SELECT 'wallet ledger rows', COUNT(*), CAST(SUM(Amount) AS decimal(20,4)) FROM tblH_User_Balance
UNION ALL SELECT 'card deposits (success)', COUNT(*), CAST(SUM(CAST(Amount AS decimal(20,4))) AS decimal(20,4)) FROM tblT_Card_Deposit WHERE Status='success'
UNION ALL SELECT 'card balance (loaded)', COUNT(*), CAST(SUM(CAST(Amount AS decimal(20,4))) AS decimal(20,4)) FROM tblT_Card_Balance
UNION ALL SELECT 'card transactions (Amount checksum, mixed ccy)', COUNT(*), CAST(SUM(CAST(Amount AS decimal(20,4))) AS decimal(20,4)) FROM tblT_Card_Transaction;

PRINT '';
PRINT '===== PRE-FLIGHT: column-length probes (MaxLen must fit the narrowed width) =====';
SELECT 'tblM_User_Crypto_Deposit.Address' AS Probe, 128 AS MaxAllowed, MAX(LEN(Address)) AS MaxLen FROM tblM_User_Crypto_Deposit WHERE Address IS NOT NULL;
SELECT 'tblH_Partner_Webhook_ID.TXID'      AS Probe, 200 AS MaxAllowed, MAX(LEN(TXID)) AS MaxLen FROM tblH_Partner_Webhook_ID WHERE TXID IS NOT NULL;
SELECT 'tblT_Card.UserReferenceID'         AS Probe, 100 AS MaxAllowed, MAX(LEN(UserReferenceID)) AS MaxLen FROM tblT_Card WHERE UserReferenceID IS NOT NULL;

PRINT '';
PRINT '===== PRE-FLIGHT: duplicate probes (ViolatingGroups must all be 0) =====';
SELECT 'UIX Crypto_Deposit (UserID,NetworkID) active' AS Probe, COUNT(*) AS ViolatingGroups FROM
  (SELECT UserID,NetworkID FROM tblM_User_Crypto_Deposit WHERE isActive=1 GROUP BY UserID,NetworkID HAVING COUNT(*)>1) x;
SELECT 'UIX User_Balance (UserID,Currency) active' AS Probe, COUNT(*) FROM
  (SELECT UserID,Currency FROM tblM_User_Balance WHERE isActive=1 GROUP BY UserID,Currency HAVING COUNT(*)>1) x;
SELECT 'UIX Crypto_Deposit (Address) active' AS Probe, COUNT(*) FROM
  (SELECT Address FROM tblM_User_Crypto_Deposit WHERE isActive=1 AND Address IS NOT NULL GROUP BY Address HAVING COUNT(*)>1) x;
SELECT 'UIX Webhook PGCrypto TXID' AS Probe, COUNT(*) FROM
  (SELECT TXID FROM tblH_Partner_Webhook_ID WHERE Type='PGCrypto' GROUP BY TXID HAVING COUNT(*)>1) x;
SELECT 'UIX Webhook ReferralCommission TXID' AS Probe, COUNT(*) FROM
  (SELECT TXID FROM tblH_Partner_Webhook_ID WHERE Type='ReferralCommission' GROUP BY TXID HAVING COUNT(*)>1) x;
SELECT 'UIX Webhook CardRefund TXID' AS Probe, COUNT(*) FROM
  (SELECT TXID FROM tblH_Partner_Webhook_ID WHERE Type='CardRefund' GROUP BY TXID HAVING COUNT(*)>1) x;
SELECT 'UIX Webhook ReferralCommissionReversal TXID' AS Probe, COUNT(*) FROM
  (SELECT TXID FROM tblH_Partner_Webhook_ID WHERE Type='ReferralCommissionReversal' GROUP BY TXID HAVING COUNT(*)>1) x;
SELECT 'UIX Card (UserID,UserReferenceID) keyed' AS Probe, COUNT(*) FROM
  (SELECT UserID,UserReferenceID FROM tblT_Card WHERE UserReferenceID IS NOT NULL AND UserReferenceID<>'' GROUP BY UserID,UserReferenceID HAVING COUNT(*)>1) x;
