-- =============================================================================
-- Global card pricing settings (tblM_Setting: CardPrice, CardDepositFeeRate)
--
-- The card catalog is now sourced LIVE from WasabiCard and overlaid with two
-- GLOBAL pricing knobs read from tblM_Setting by CardCatalogService:
--
--   CardPrice           -> the customer-facing card price   (default 0)
--   CardDepositFeeRate  -> the deposit/top-up fee, in %      (default 3)
--
-- These replace the old per-card-type price/fee columns as the source of the
-- customer-facing numbers. This migration seeds the two rows if they are missing
-- so the overlay reads the intended defaults instead of falling back in code.
--
-- Deploy: additive, idempotent. Guarded with IF NOT EXISTS on Name -- safe to
-- re-run, and it never overwrites an admin-edited value once the row exists.
-- =============================================================================

IF NOT EXISTS (SELECT 1 FROM dbo.tblM_Setting WHERE Name = 'CardPrice')
BEGIN
    INSERT INTO dbo.tblM_Setting (Name, Value, DateCreated)
    VALUES ('CardPrice', 0, GETDATE());
END
GO

IF NOT EXISTS (SELECT 1 FROM dbo.tblM_Setting WHERE Name = 'CardDepositFeeRate')
BEGIN
    INSERT INTO dbo.tblM_Setting (Name, Value, DateCreated)
    VALUES ('CardDepositFeeRate', 3, GETDATE());
END
GO
