-- =============================================================================
-- Inbound money-webhook per-event dedup guard
--   (tblH_Partner_Webhook_ID)
--
-- The PGCrypto (Runegate) deposit-credit branch records each processed event in
-- tblH_Partner_Webhook_ID and credits the wallet in the SAME transaction. The
-- replay defence is this filtered unique index: a replayed or concurrent duplicate
-- delivery hits the constraint (2601/2627), the credit transaction rolls back, and
-- the handler treats it as an already-processed no-op. This is the DB-layer dedup
-- the house pattern relies on — not a check-then-insert in app code, which races.
--
-- Key: TXID holds the provider TransactionID ALONE (one credit per transaction).
-- It deliberately does NOT include Status: the credit path is gated on isPaid==1
-- before a dedup row is ever written, so only confirmed events reach here — and
-- folding the free-form Status into the key would let the same confirmed deposit,
-- redelivered with a different status string, write a distinct key and double-credit.
-- (Do not "restore" a composite TransactionID|Status key — that reintroduces the
-- double-credit bug; see the comment in WalletService.CreditDeposit.) Type
-- discriminates the provider so the index is filtered to PGCrypto rows and never
-- collides with other webhook sources sharing the table.
--
--   UIX_tblH_Partner_Webhook_ID_PGCrypto_TXID
--       UNIQUE (TXID) WHERE Type = 'PGCrypto'
--
-- Deploy: additive only, no backfill, no EDMX regeneration. Apply during the dev
-- shakeout / launch, like the other deploy scripts.
--
-- Idempotency: guarded with IF NOT EXISTS — safe to re-run.
--
-- Pre-flight: a duplicate probe RAISERRORs with a clear message if existing rows
-- already violate the rule (e.g. a prior unguarded ad-hoc insert), so a dirty
-- deploy fails actionably instead of on a cryptic CREATE INDEX error.
--
-- Column note: TXID ships as nvarchar(max), which cannot be an index key. The dedup
-- key (the bare TransactionID) is short, so this script first narrows TXID to an
-- indexable width. Safe: the table ships unwired/empty, so there is no data to lose.
-- =============================================================================

-- Make TXID indexable (narrow from nvarchar(max)). Guarded so re-runs are no-ops.
IF EXISTS (SELECT 1 FROM sys.columns
           WHERE object_id = OBJECT_ID('tblH_Partner_Webhook_ID')
             AND name = 'TXID'
             AND (max_length = -1 OR max_length > 400))
BEGIN
    ALTER TABLE tblH_Partner_Webhook_ID ALTER COLUMN TXID nvarchar(200) NULL;
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes
               WHERE name = 'UIX_tblH_Partner_Webhook_ID_PGCrypto_TXID'
                 AND object_id = OBJECT_ID('tblH_Partner_Webhook_ID'))
BEGIN
    IF EXISTS (
        SELECT TXID
        FROM tblH_Partner_Webhook_ID
        WHERE Type = 'PGCrypto'
        GROUP BY TXID
        HAVING COUNT(*) > 1)
    BEGIN
        RAISERROR (
            'Cannot create UIX_tblH_Partner_Webhook_ID_PGCrypto_TXID: duplicate PGCrypto TXID rows exist. Investigate (possible prior double-credit) and de-duplicate before re-running. Probe: SELECT TXID, COUNT(*) FROM tblH_Partner_Webhook_ID WHERE Type = ''PGCrypto'' GROUP BY TXID HAVING COUNT(*) > 1;',
            16, 1);
    END
    ELSE
    BEGIN
        CREATE UNIQUE NONCLUSTERED INDEX UIX_tblH_Partner_Webhook_ID_PGCrypto_TXID
            ON tblH_Partner_Webhook_ID (TXID)
            WHERE Type = 'PGCrypto';
    END
END
GO
