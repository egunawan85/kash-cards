-- =============================================================================
-- Referral-commission per-payout dedup guard
--   (tblH_Partner_Webhook_ID, Type = 'ReferralCommission')
--
-- When a referred user's card buy/top-up finalizes, WalletService.CreditReferralCommission
-- records the payout in tblH_Partner_Webhook_ID (keyed by the referee's ORDER id) and credits
-- the referrer's wallet in the SAME transaction. The replay defence is this filtered unique
-- index: a re-delivered webhook or the reconciliation sweep racing the webhook hits the
-- constraint (2601/2627), the credit transaction rolls back, and the payout is treated as an
-- already-paid no-op — so a referrer can never be paid twice for the same order.
--
-- This MUST exist for the dedup to fire: the sibling PGCrypto index is filtered to
-- Type = 'PGCrypto', so ReferralCommission rows are otherwise unconstrained and would NOT
-- raise on a duplicate insert (the double-pay hole this index closes).
--
--   UIX_tblH_Partner_Webhook_ID_ReferralCommission_TXID
--       UNIQUE (TXID) WHERE Type = 'ReferralCommission'
--
-- Key: TXID holds the referee order id ALONE (one payout per finalized order). Filtered by
-- Type so it never collides with the PGCrypto deposit dedup sharing the table.
--
-- Deploy: additive only, no backfill, no EDMX regeneration. Idempotent (IF NOT EXISTS), safe
-- to re-run. A duplicate probe RAISERRORs actionably if existing rows already violate the rule.
-- =============================================================================

-- Make TXID indexable (narrow from nvarchar(max)). Guarded so re-runs are no-ops. (Shared with
-- the PGCrypto dedup index; harmless if that script already narrowed it.)
IF EXISTS (SELECT 1 FROM sys.columns
           WHERE object_id = OBJECT_ID('tblH_Partner_Webhook_ID')
             AND name = 'TXID'
             AND (max_length = -1 OR max_length > 400))
BEGIN
    ALTER TABLE tblH_Partner_Webhook_ID ALTER COLUMN TXID nvarchar(200) NULL;
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes
               WHERE name = 'UIX_tblH_Partner_Webhook_ID_ReferralCommission_TXID'
                 AND object_id = OBJECT_ID('tblH_Partner_Webhook_ID'))
BEGIN
    IF EXISTS (
        SELECT TXID
        FROM tblH_Partner_Webhook_ID
        WHERE Type = 'ReferralCommission'
        GROUP BY TXID
        HAVING COUNT(*) > 1)
    BEGIN
        RAISERROR (
            'Cannot create UIX_tblH_Partner_Webhook_ID_ReferralCommission_TXID: duplicate ReferralCommission TXID rows exist. Investigate (possible prior double-pay) and de-duplicate before re-running. Probe: SELECT TXID, COUNT(*) FROM tblH_Partner_Webhook_ID WHERE Type = ''ReferralCommission'' GROUP BY TXID HAVING COUNT(*) > 1;',
            16, 1);
    END
    ELSE
    BEGIN
        CREATE UNIQUE NONCLUSTERED INDEX UIX_tblH_Partner_Webhook_ID_ReferralCommission_TXID
            ON tblH_Partner_Webhook_ID (TXID)
            WHERE Type = 'ReferralCommission';
    END
END
GO
