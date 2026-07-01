-- =============================================================================
-- Deposit-into-card Phase C: record the ACTUAL float landing on the forward ledger
--
-- The `wallet_transaction` webhook (WasabiCard crediting our merchant float from one of
-- our forwards) reports the amount that ACTUALLY landed, matched to the forward by the
-- on-chain tx hash. Persist that as columns (not a JSON blob) so:
--   - issuance can gate on "confirmed landed amount >= the card's draw" (not a pooled guess), and
--   - ops can query overpaid/underpaid float and stuck forwards directly.
-- Additive, idempotent, self-healing (each column guarded independently). NULL for
-- historical rows and for the polling-confirm path (which has no per-forward landing proof).
-- =============================================================================

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE Name = N'ActualReceivedUsd' AND Object_ID = Object_ID(N'dbo.tblH_WasabiCard_Refill'))
    ALTER TABLE dbo.tblH_WasabiCard_Refill ADD ActualReceivedUsd decimal(18,4) NULL;
GO
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE Name = N'LandedTxId' AND Object_ID = Object_ID(N'dbo.tblH_WasabiCard_Refill'))
    ALTER TABLE dbo.tblH_WasabiCard_Refill ADD LandedTxId nvarchar(200) NULL;
GO
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE Name = N'LandedDate' AND Object_ID = Object_ID(N'dbo.tblH_WasabiCard_Refill'))
    ALTER TABLE dbo.tblH_WasabiCard_Refill ADD LandedDate datetime NULL;
GO
