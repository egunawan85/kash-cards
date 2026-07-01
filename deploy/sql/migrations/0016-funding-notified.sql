-- =============================================================================
-- Deposit-into-card: one-time terminal-state EMAIL notification bookkeeping.
--
-- Adds dbo.tblT_Card_Funding_Intent.NotifiedDate: the timestamp at which the user was
-- emailed that their card funding reached a terminal state (Completed / Failed / Expired).
-- The issuance tick's notification sweep CLAIMS a row by stamping this column in a single
-- conditional UPDATE (WHERE NotifiedDate IS NULL) before sending, so a redelivery / a second
-- overlapping tick can't double-send (at-most-once). NULL = not yet notified.
--
-- Deploy: additive, idempotent. Guarded with IF NOT EXISTS. NULL for all historical rows, so
-- pre-existing terminal intents are simply never emailed (correct: the feature ships dark, and
-- there are no live streaming intents until CardFundingStreamingEnabled is turned on).
-- =============================================================================

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE Name = N'NotifiedDate' AND Object_ID = Object_ID(N'dbo.tblT_Card_Funding_Intent'))
    ALTER TABLE dbo.tblT_Card_Funding_Intent ADD NotifiedDate datetime NULL;
GO

-- Sweep index: the notify pass scans for terminal-and-unnotified rows every tick. Filtered so it
-- stays tiny (only the unnotified tail), matching the sweep's predicate.
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_CFI_Notify' AND object_id = OBJECT_ID('dbo.tblT_Card_Funding_Intent'))
    CREATE INDEX IX_CFI_Notify ON dbo.tblT_Card_Funding_Intent (Status)
        WHERE NotifiedDate IS NULL;
GO
