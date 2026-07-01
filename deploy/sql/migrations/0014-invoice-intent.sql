-- =============================================================================
-- Deposit-into-card: per-intent Runegate INVOICE funding (Design pass 3)
--
-- Moves the front half of settlement from a fixed per-user static address + "one open
-- intent per user" to a per-intent Runegate INVOICE (unique address + our PartnerReferenceID
-- = intent id). Consequences:
--   1. tblT_Card_Funding_Intent gains InvoiceID + InvoiceAddress (the invoice we created).
--   2. DROP the one-open-intent unique index UX_CFI_OneOpenPerUser — a user may now have
--      MULTIPLE concurrent intents (each isolated by its own invoice; a stuck one blocks none).
--      Deposit attribution is by the invoice's PartnerReferenceID, so it stays unambiguous.
--   3. tblM_Setting rows for the merchant-specific Runegate invoice config (PaymentID +
--      ProductID) — operator-provided; the streaming path can't mint invoices until set.
--
-- Deploy: additive, idempotent, self-healing (mirrors 0011/0012).
-- =============================================================================

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE Name = N'InvoiceID' AND Object_ID = Object_ID(N'dbo.tblT_Card_Funding_Intent'))
    ALTER TABLE dbo.tblT_Card_Funding_Intent ADD InvoiceID nvarchar(100) NULL;
GO
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE Name = N'InvoiceAddress' AND Object_ID = Object_ID(N'dbo.tblT_Card_Funding_Intent'))
    ALTER TABLE dbo.tblT_Card_Funding_Intent ADD InvoiceAddress nvarchar(128) NULL;
GO

-- Drop the one-open-intent-per-user constraint: multiple concurrent intents are now allowed
-- (each has its own invoice/address, so a landed deposit is attributed by PartnerReferenceID,
-- not by "the user's single open intent").
IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_CFI_OneOpenPerUser' AND object_id = OBJECT_ID('dbo.tblT_Card_Funding_Intent'))
    DROP INDEX UX_CFI_OneOpenPerUser ON dbo.tblT_Card_Funding_Intent;
GO

-- Lookup the intent by the invoice's PartnerReferenceID (= IntentID) is served by the existing
-- UNIQUE index UX_CFI_IntentID; no new index needed.

-- ---- Runegate invoice config (Param1 holds the string id; operator-provided) ----

-- The merchant PaymentID that resolves to our USDT-TRC20 payment method (Runegate
-- tblM_Company_Merchant_Payment_Availability). Required to mint a USDT-TRC20 invoice.
IF NOT EXISTS (SELECT 1 FROM dbo.tblM_Setting WHERE Name = 'RunegatePaymentId')
    INSERT INTO dbo.tblM_Setting (Name, Value, DateCreated, Param1) VALUES ('RunegatePaymentId', NULL, GETDATE(), NULL);
GO

-- A real active merchant ProductID (Runegate tblM_Company_Merchant_Product); the invoice's
-- Products[] must reference one (we override its Price with ExpectedTotal).
IF NOT EXISTS (SELECT 1 FROM dbo.tblM_Setting WHERE Name = 'RunegateProductId')
    INSERT INTO dbo.tblM_Setting (Name, Value, DateCreated, Param1) VALUES ('RunegateProductId', NULL, GETDATE(), NULL);
GO

-- The merchant CustomerID the invoices are created under. It MUST be a DYNAMIC-address customer
-- (Runegate tblM_Company_Merchant_Customer.isStaticAddress = 0) so each invoice gets its OWN unique
-- address and multiple concurrent invoices are allowed (a static customer is capped at one open
-- invoice). One shared customer serves all users; the per-intent key is PartnerReferenceID.
IF NOT EXISTS (SELECT 1 FROM dbo.tblM_Setting WHERE Name = 'RunegateCustomerId')
    INSERT INTO dbo.tblM_Setting (Name, Value, DateCreated, Param1) VALUES ('RunegateCustomerId', NULL, GETDATE(), NULL);
GO

-- ---- C2 three-way forward correlation (dormant until the streaming forward is live) ----
--
-- A forward is recorded with ProviderRef = the Runegate TRANSFER id (returned synchronously). The
-- ON-CHAIN tx hash is NOT known then — it arrives later on Runegate's transfer-completion webhook.
-- WasabiCard's wallet_transaction webhook (float credited) reports that ON-CHAIN hash. So matching a
-- float landing to our forward needs the captured chain hash, not the transfer id. ChainTxHash holds
-- the hash stamped by the transfer-completion webhook (keyed by ProviderRef = transfer id), which the
-- float-landing matcher then compares against. NULL until the live-forward spike wires + verifies the
-- transfer-completion payload; the whole C2 path stays gated behind CardFundingStreamingEnabled.
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE Name = N'ChainTxHash' AND Object_ID = Object_ID(N'dbo.tblH_WasabiCard_Refill'))
    ALTER TABLE dbo.tblH_WasabiCard_Refill ADD ChainTxHash nvarchar(200) NULL;
GO
