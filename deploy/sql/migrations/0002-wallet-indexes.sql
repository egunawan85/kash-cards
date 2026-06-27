-- =============================================================================
-- Prepaid-wallet uniqueness guards
--   (tblM_User_Crypto_Deposit, tblM_User_Balance)
--
-- The wallet row and the per-user static deposit address are created lazily and
-- idempotently (WalletService.EnsureWallet / EnsureDepositAddress). For that
-- create-if-missing to be race-safe under concurrency, the "one active row per
-- user" rule must be enforced by the DATABASE, not by a check-then-insert in app
-- code. These filtered unique indexes are that enforcement: a concurrent second
-- insert fails with a duplicate-key (2601/2627) which the ensure-helper swallows
-- and re-reads the winner's row.
--
-- They also make the deposit -> wallet credit match unambiguous: an inbound
-- deposit address resolves to EXACTLY ONE active owner.
--
--   UIX_tblM_User_Crypto_Deposit_User_Network
--       UNIQUE (UserID, NetworkID) WHERE isActive = 1
--       — one active deposit address per user per network.
--
--   UIX_tblM_User_Balance_User_Currency
--       UNIQUE (UserID, Currency)  WHERE isActive = 1
--       — one active wallet row per user per currency.
--
-- Filtered on isActive = 1 so historical soft-deleted rows (DeletedBy / isActive
-- = 0) do not collide with the live row.
--
-- Deploy: additive only, no backfill, no EDMX regeneration (the entity classes
-- already exist). Apply during the dev shakeout / launch, like create-token-
-- tables.sql.
--
-- Idempotency: guarded with IF NOT EXISTS — safe to re-run on re-deploy.
--
-- Pre-flight: each index is preceded by a duplicate probe that RAISERRORs with a
-- clear message if existing data already violates the rule, so a dirty deploy
-- fails loudly and actionably instead of on a cryptic CREATE INDEX error. Resolve
-- the reported duplicates (deactivate the stale row) and re-run.
-- =============================================================================

-- ---- tblM_User_Crypto_Deposit (UserID, NetworkID) active-row uniqueness --------

IF NOT EXISTS (SELECT 1 FROM sys.indexes
               WHERE name = 'UIX_tblM_User_Crypto_Deposit_User_Network'
                 AND object_id = OBJECT_ID('tblM_User_Crypto_Deposit'))
BEGIN
    IF EXISTS (
        SELECT UserID, NetworkID
        FROM tblM_User_Crypto_Deposit
        WHERE isActive = 1
        GROUP BY UserID, NetworkID
        HAVING COUNT(*) > 1)
    BEGIN
        RAISERROR (
            'Cannot create UIX_tblM_User_Crypto_Deposit_User_Network: duplicate active (UserID, NetworkID) rows exist. Deactivate the stale duplicates and re-run. Probe: SELECT UserID, NetworkID, COUNT(*) FROM tblM_User_Crypto_Deposit WHERE isActive = 1 GROUP BY UserID, NetworkID HAVING COUNT(*) > 1;',
            16, 1);
    END
    ELSE
    BEGIN
        CREATE UNIQUE NONCLUSTERED INDEX UIX_tblM_User_Crypto_Deposit_User_Network
            ON tblM_User_Crypto_Deposit (UserID, NetworkID)
            WHERE isActive = 1;
    END
END
GO

-- ---- tblM_User_Balance (UserID, Currency) active-row uniqueness ----------------

IF NOT EXISTS (SELECT 1 FROM sys.indexes
               WHERE name = 'UIX_tblM_User_Balance_User_Currency'
                 AND object_id = OBJECT_ID('tblM_User_Balance'))
BEGIN
    IF EXISTS (
        SELECT UserID, Currency
        FROM tblM_User_Balance
        WHERE isActive = 1
        GROUP BY UserID, Currency
        HAVING COUNT(*) > 1)
    BEGIN
        RAISERROR (
            'Cannot create UIX_tblM_User_Balance_User_Currency: duplicate active (UserID, Currency) rows exist. Deactivate the stale duplicates and re-run. Probe: SELECT UserID, Currency, COUNT(*) FROM tblM_User_Balance WHERE isActive = 1 GROUP BY UserID, Currency HAVING COUNT(*) > 1;',
            16, 1);
    END
    ELSE
    BEGIN
        CREATE UNIQUE NONCLUSTERED INDEX UIX_tblM_User_Balance_User_Currency
            ON tblM_User_Balance (UserID, Currency)
            WHERE isActive = 1;
    END
END
GO

-- ---- tblM_User_Crypto_Deposit (Address) active-row uniqueness ------------------
-- The deposit-credit branch resolves an inbound Address to its owning user, so an active
-- address must map to EXACTLY ONE user — otherwise a deposit could be credited to the
-- wrong account. Address ships as nvarchar(max) (not indexable); narrow it to an
-- address-sized width first (safe: far wider than any TRC20/crypto address).

IF EXISTS (SELECT 1 FROM sys.columns
           WHERE object_id = OBJECT_ID('tblM_User_Crypto_Deposit')
             AND name = 'Address'
             AND (max_length = -1 OR max_length > 256))
BEGIN
    ALTER TABLE tblM_User_Crypto_Deposit ALTER COLUMN Address nvarchar(128) NULL;
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes
               WHERE name = 'UIX_tblM_User_Crypto_Deposit_Address'
                 AND object_id = OBJECT_ID('tblM_User_Crypto_Deposit'))
BEGIN
    IF EXISTS (
        SELECT Address
        FROM tblM_User_Crypto_Deposit
        WHERE isActive = 1 AND Address IS NOT NULL
        GROUP BY Address
        HAVING COUNT(*) > 1)
    BEGIN
        RAISERROR (
            'Cannot create UIX_tblM_User_Crypto_Deposit_Address: duplicate active Address rows exist — a deposit could be credited to the wrong user. Investigate and de-duplicate before re-running. Probe: SELECT Address, COUNT(*) FROM tblM_User_Crypto_Deposit WHERE isActive = 1 AND Address IS NOT NULL GROUP BY Address HAVING COUNT(*) > 1;',
            16, 1);
    END
    ELSE
    BEGIN
        CREATE UNIQUE NONCLUSTERED INDEX UIX_tblM_User_Crypto_Deposit_Address
            ON tblM_User_Crypto_Deposit (Address)
            WHERE isActive = 1 AND Address IS NOT NULL;
    END
END
GO
