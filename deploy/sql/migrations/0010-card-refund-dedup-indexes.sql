-- =============================================================================
-- Card-refund + commission-clawback per-event dedup guards
--   (tblH_Partner_Webhook_ID, Type = 'CardRefund' and 'ReferralCommissionReversal')
--
-- The admin card refund (CardRefundService) returns a cancelled card's balance to the buyer
-- and claws back any referral commission, recording each in tblH_Partner_Webhook_ID in the SAME
-- transaction as the wallet mutation (WalletService.CreditCardRefund / ReverseReferralCommission).
-- The replay defence is these filtered unique indexes: a re-triggered refund hits the constraint
-- (2601/2627), the mutation rolls back, and it is treated as an already-done no-op — so a card can
-- never be refunded twice, nor a commission clawed back twice.
--
-- These MUST exist for the dedup to fire: the sibling indexes are filtered to their own Type
-- ('PGCrypto', 'ReferralCommission'), so 'CardRefund' / 'ReferralCommissionReversal' rows are
-- otherwise unconstrained and would NOT raise on a duplicate insert (the double-refund hole these
-- indexes close). CardRefundService also fail-closes when the CardRefund index is absent.
--
--   UIX_tblH_Partner_Webhook_ID_CardRefund_TXID
--       UNIQUE (TXID) WHERE Type = 'CardRefund'                 -- TXID = the physical card number
--   UIX_tblH_Partner_Webhook_ID_ReferralCommissionReversal_TXID
--       UNIQUE (TXID) WHERE Type = 'ReferralCommissionReversal' -- TXID = the referee order id
--
-- Deploy: additive only, no backfill, no EDMX regeneration. Idempotent (IF NOT EXISTS), safe to
-- re-run. A duplicate probe RAISERRORs actionably if existing rows already violate the rule.
-- =============================================================================

-- Make TXID indexable (narrow from nvarchar(max)). Guarded so re-runs are no-ops. (Shared with the
-- sibling dedup indexes; harmless if an earlier script already narrowed it.)
IF EXISTS (SELECT 1 FROM sys.columns
           WHERE object_id = OBJECT_ID('tblH_Partner_Webhook_ID')
             AND name = 'TXID'
             AND (max_length = -1 OR max_length > 400))
BEGIN
    ALTER TABLE tblH_Partner_Webhook_ID ALTER COLUMN TXID nvarchar(200) NULL;
END
GO

-- CardRefund: one refund per physical card (TXID = card number).
IF NOT EXISTS (SELECT 1 FROM sys.indexes
               WHERE name = 'UIX_tblH_Partner_Webhook_ID_CardRefund_TXID'
                 AND object_id = OBJECT_ID('tblH_Partner_Webhook_ID'))
BEGIN
    IF EXISTS (
        SELECT TXID FROM tblH_Partner_Webhook_ID
        WHERE Type = 'CardRefund'
        GROUP BY TXID HAVING COUNT(*) > 1)
    BEGIN
        RAISERROR (
            'Cannot create UIX_tblH_Partner_Webhook_ID_CardRefund_TXID: duplicate CardRefund TXID rows exist. Investigate (possible prior double-refund) and de-duplicate before re-running.',
            16, 1);
    END
    ELSE
    BEGIN
        CREATE UNIQUE NONCLUSTERED INDEX UIX_tblH_Partner_Webhook_ID_CardRefund_TXID
            ON tblH_Partner_Webhook_ID (TXID)
            WHERE Type = 'CardRefund';
    END
END
GO

-- ReferralCommissionReversal: one clawback per referee order (TXID = referee order id).
IF NOT EXISTS (SELECT 1 FROM sys.indexes
               WHERE name = 'UIX_tblH_Partner_Webhook_ID_ReferralCommissionReversal_TXID'
                 AND object_id = OBJECT_ID('tblH_Partner_Webhook_ID'))
BEGIN
    IF EXISTS (
        SELECT TXID FROM tblH_Partner_Webhook_ID
        WHERE Type = 'ReferralCommissionReversal'
        GROUP BY TXID HAVING COUNT(*) > 1)
    BEGIN
        RAISERROR (
            'Cannot create UIX_tblH_Partner_Webhook_ID_ReferralCommissionReversal_TXID: duplicate rows exist. Investigate (possible prior double-clawback) and de-duplicate before re-running.',
            16, 1);
    END
    ELSE
    BEGIN
        CREATE UNIQUE NONCLUSTERED INDEX UIX_tblH_Partner_Webhook_ID_ReferralCommissionReversal_TXID
            ON tblH_Partner_Webhook_ID (TXID)
            WHERE Type = 'ReferralCommissionReversal';
    END
END
GO
