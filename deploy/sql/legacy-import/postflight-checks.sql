-- =============================================================================
-- postflight-checks.sql  --  legacy KashNow import, POST-migration verification.
--
-- Run AFTER vm-migrate.ps1 (ledger + 0001..0010) AND after the credential scrub
-- (deploy/sql/oneoff/crypto-at-rest-scrub.sql). Confirms the import landed intact:
--   1. Row counts + money baseline -- diff against preflight-checks.sql output; the
--      user/card/money numbers MUST be identical (migrations are additive schema-only).
--   2. Migration ledger holds 0000-baseline-dacpac + 0001..0010.
--   3. Every migration-created object exists (the gap closed by the migrations).
--   4. No untrusted FK/CHECK constraints (a silent post-migration trap).
--   5. Credential scrub took: all passwords are the reset sentinel, 2FA cleared,
--      API secrets neutralised.
--
-- Schema PARITY against a freshly-built current DB is verified separately with
-- SqlPackage /Action:DeployReport (see docs/runbooks/legacy-import-cutover.md);
-- the only expected residual is the harmless legacy-extra tblM_Card_Type.NotSupport
-- column and cosmetic view/diagram metadata.
--
-- Read-only. Run:  sqlcmd -S <server> -d <db> -E -b -i postflight-checks.sql
-- =============================================================================
SET NOCOUNT ON;

PRINT '===== ROW COUNTS (USER-DATA tables must match preflight) =====';
-- Expected deltas vs preflight (NOT errors): dbo.SchemaMigrations appears (11 rows);
-- the four migration-created tables appear empty (tblT_AuthToken, tblT_RefreshToken,
-- tblH_Auth_Log, tblH_WasabiCard_Refill); tblM_Setting grows by the seeded settings
-- (0008/0009 -> +13: CardPrice, CardDepositFeeRate, WasabiCard*). Every other table
-- must be byte-for-byte identical to preflight.
SELECT t.name AS TableName, SUM(p.rows) AS [Rows]
FROM sys.tables t JOIN sys.partitions p ON p.object_id = t.object_id AND p.index_id IN (0,1)
GROUP BY t.name ORDER BY t.name;

PRINT '';
PRINT '===== MONEY BASELINE (must match preflight) =====';
SELECT 'users' AS Metric, COUNT(*) AS n, CAST(NULL AS decimal(20,4)) AS Amount FROM tblM_User
UNION ALL SELECT 'cardholders', COUNT(*), NULL FROM tblM_Cardholder
UNION ALL SELECT 'cards', COUNT(*), NULL FROM tblT_Card
UNION ALL SELECT 'active wallet balances', COUNT(*), CAST(SUM(Balance) AS decimal(20,4)) FROM tblM_User_Balance WHERE isActive=1
UNION ALL SELECT 'wallet ledger rows', COUNT(*), CAST(SUM(Amount) AS decimal(20,4)) FROM tblH_User_Balance
UNION ALL SELECT 'card deposits (success)', COUNT(*), CAST(SUM(CAST(Amount AS decimal(20,4))) AS decimal(20,4)) FROM tblT_Card_Deposit WHERE Status='success'
UNION ALL SELECT 'card balance (loaded)', COUNT(*), CAST(SUM(CAST(Amount AS decimal(20,4))) AS decimal(20,4)) FROM tblT_Card_Balance
UNION ALL SELECT 'card transactions (Amount checksum, mixed ccy)', COUNT(*), CAST(SUM(CAST(Amount AS decimal(20,4))) AS decimal(20,4)) FROM tblT_Card_Transaction;

PRINT '';
PRINT '===== MIGRATION LEDGER (expect 0000 + 0001..0010) =====';
SELECT MigrationId, AppliedUtc FROM dbo.SchemaMigrations ORDER BY MigrationId;

PRINT '';
PRINT '===== MIGRATION-CREATED OBJECTS (Present must all = 1) =====';
SELECT 'table tblT_AuthToken'        AS Obj, IIF(OBJECT_ID('dbo.tblT_AuthToken')        IS NULL,0,1) AS Present
UNION ALL SELECT 'table tblT_RefreshToken',     IIF(OBJECT_ID('dbo.tblT_RefreshToken')     IS NULL,0,1)
UNION ALL SELECT 'table tblH_Auth_Log',         IIF(OBJECT_ID('dbo.tblH_Auth_Log')         IS NULL,0,1)
UNION ALL SELECT 'table tblH_WasabiCard_Refill',IIF(OBJECT_ID('dbo.tblH_WasabiCard_Refill')IS NULL,0,1)
UNION ALL SELECT 'table dbo.SchemaMigrations',  IIF(OBJECT_ID('dbo.SchemaMigrations')       IS NULL,0,1)
UNION ALL SELECT 'col tblM_User.FailureCount',  IIF(COL_LENGTH('dbo.tblM_User','FailureCount') IS NULL,0,1)
UNION ALL SELECT 'col tblM_User.LockoutEnd',    IIF(COL_LENGTH('dbo.tblM_User','LockoutEnd')    IS NULL,0,1)
UNION ALL SELECT 'idx UIX_tblM_User_Crypto_Deposit_Address', IIF(EXISTS(SELECT 1 FROM sys.indexes WHERE name='UIX_tblM_User_Crypto_Deposit_Address'),1,0)
UNION ALL SELECT 'idx UIX_tblH_Partner_Webhook_ID_PGCrypto_TXID', IIF(EXISTS(SELECT 1 FROM sys.indexes WHERE name='UIX_tblH_Partner_Webhook_ID_PGCrypto_TXID'),1,0)
UNION ALL SELECT 'idx UIX_tblT_Card_User_Ref', IIF(EXISTS(SELECT 1 FROM sys.indexes WHERE name='UIX_tblT_Card_User_Ref'),1,0);

PRINT '';
PRINT '===== UNTRUSTED CONSTRAINTS (expect no rows) =====';
SELECT 'FK' AS Kind, name FROM sys.foreign_keys   WHERE is_not_trusted = 1
UNION ALL
SELECT 'CHECK', name FROM sys.check_constraints WHERE is_not_trusted = 1;

PRINT '';
PRINT '===== CREDENTIAL SCRUB VERIFICATION =====';
-- Hard checks (must be 0):
SELECT 'users NOT reset to sentinel (must be 0)' AS Check_, COUNT(*) AS Count_ FROM tblM_User  WHERE Password <> N'!RESET-REQUIRED-CRYPTO-MIGRATION!';
SELECT '2FA rows remaining (must be 0)'          AS Check_, COUNT(*) FROM tblM_User_2FA;
SELECT 'API secrets still live (must be 0)'      AS Check_, COUNT(*) FROM tblM_User_API WHERE isActive = 1 OR SecretKey <> N'!REISSUE-REQUIRED-CRYPTO-MIGRATION!';
-- Informational: legacy has no admins, so the only non-sentinel admin should be the
-- re-seeded bootstrap admin (runbook step "restore bootstrap admin"). Expect ~1, not 0.
SELECT 'admins with real (non-sentinel) password [expect re-seeded bootstrap]' AS Check_, COUNT(*) FROM tblM_Admin WHERE Password <> N'!RESET-REQUIRED-CRYPTO-MIGRATION!';
