-- =============================================================================
-- Deposit-into-card: switch per-intent funding from Runegate INVOICE to PAYMENT REQUEST
--
-- A Runegate payment request (POST /v1/payment) mints a dynamic deposit address for a
-- custom amount tagged with our PartnerReferenceID, needing NO pre-registered product or
-- customer — just the merchant + coin. This replaces the invoice model's three-entity
-- setup (PaymentId + CustomerId + ProductId) with two operator-provided ids:
--   RunegateMerchantId  — the merchant that owns the payment request
--   RunegateCoinId      — the TRON chain coin id at Runegate
--   RunegateTokenId     — the USDT token id (on the TRON chain); USDT-TRC20 = coin + token
-- All held in tblM_Setting.Param1 (string id); the streaming path can't mint a payment
-- request until ALL THREE are set, so the feature stays dark by default.
--
-- The now-unused invoice config rows (RunegatePaymentId/ProductId/CustomerId, seeded by
-- 0014) are removed to avoid operator confusion — nothing reads them after this change.
--
-- Deploy: additive, idempotent, self-healing (mirrors 0011-0016).
-- =============================================================================

-- ---- new payment-request config (Param1 holds the string id; operator-provided) ----

IF NOT EXISTS (SELECT 1 FROM dbo.tblM_Setting WHERE Name = 'RunegateMerchantId')
    INSERT INTO dbo.tblM_Setting (Name, Value, DateCreated, Param1) VALUES ('RunegateMerchantId', NULL, GETDATE(), NULL);
GO

IF NOT EXISTS (SELECT 1 FROM dbo.tblM_Setting WHERE Name = 'RunegateCoinId')
    INSERT INTO dbo.tblM_Setting (Name, Value, DateCreated, Param1) VALUES ('RunegateCoinId', NULL, GETDATE(), NULL);
GO

IF NOT EXISTS (SELECT 1 FROM dbo.tblM_Setting WHERE Name = 'RunegateTokenId')
    INSERT INTO dbo.tblM_Setting (Name, Value, DateCreated, Param1) VALUES ('RunegateTokenId', NULL, GETDATE(), NULL);
GO

-- ---- retire the invoice-model config (no longer read after the payment-request switch) ----
-- ROLLBACK NOTE (ops): this DELETE is one-way — reverting to the invoice model would require manually
-- re-inserting RunegatePaymentId/ProductId/CustomerId (and their values). Acceptable for a one-way switch.
DELETE FROM dbo.tblM_Setting WHERE Name IN ('RunegatePaymentId', 'RunegateProductId', 'RunegateCustomerId');
GO
