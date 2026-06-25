-- =============================================================================
-- seed-reference.sql -- Tier A "structural minimums": the master/config rows the
-- app's flows read on a fresh DB. No secrets; fully committed. Idempotent
-- (NOT EXISTS guarded -- no duplicate, no resurrect on re-run).
--
-- Applied by deploy/scripts/deploy/vm-seed.ps1 (sqlcmd) after the schema publish.
-- Mirrors the sister db/seed-reference.sql pattern (qrypto-omni SEEDING_STRATEGY.md):
-- seed only what the launch flow needs, strategy separate from data.
-- =============================================================================
SET NOCOUNT ON;

-- Roles -----------------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM dbo.tblM_User_Role WHERE RoleID = 'role-owner')
    INSERT INTO dbo.tblM_User_Role  (RoleID, Role,  isActive, DateCreated) VALUES ('role-owner','Owner',1,GETUTCDATE());
IF NOT EXISTS (SELECT 1 FROM dbo.tblM_Admin_Role WHERE RoleID = 'role-admin')
    INSERT INTO dbo.tblM_Admin_Role (RoleID, Role,  isActive, DateCreated) VALUES ('role-admin','Admin',1,GETUTCDATE());

-- Settings: code reads the commission rate at tblM_Setting ID=2 and the counters
-- at tblM_Setting_Counter ID=1/2, so the IDs are forced via IDENTITY_INSERT.
IF NOT EXISTS (SELECT 1 FROM dbo.tblM_Setting WHERE ID = 2)
BEGIN
    SET IDENTITY_INSERT dbo.tblM_Setting ON;
    INSERT INTO dbo.tblM_Setting (ID, Name, Value, DateCreated) VALUES (2,'DefaultCommissionRate','0.1',GETUTCDATE());
    SET IDENTITY_INSERT dbo.tblM_Setting OFF;
END
IF NOT EXISTS (SELECT 1 FROM dbo.tblM_Setting_Counter WHERE ID = 1)
BEGIN
    SET IDENTITY_INSERT dbo.tblM_Setting_Counter ON;
    INSERT INTO dbo.tblM_Setting_Counter (ID, Name, Value, DateCreated) VALUES (1,'CardCounter','1000',GETUTCDATE());
    SET IDENTITY_INSERT dbo.tblM_Setting_Counter OFF;
END
IF NOT EXISTS (SELECT 1 FROM dbo.tblM_Setting_Counter WHERE ID = 2)
BEGIN
    SET IDENTITY_INSERT dbo.tblM_Setting_Counter ON;
    INSERT INTO dbo.tblM_Setting_Counter (ID, Name, Value, DateCreated) VALUES (2,'DepositCounter','5000',GETUTCDATE());
    SET IDENTITY_INSERT dbo.tblM_Setting_Counter OFF;
END

-- Card type + crypto network (dev fake-address path uses the TRC20 network) ----
IF NOT EXISTS (SELECT 1 FROM dbo.tblM_Card_Type WHERE CardTypeId = 111028)
    INSERT INTO dbo.tblM_Card_Type (CardTypeId, CardName, CardPrice, CardPriceCurrency, RechargeFeeRate, FiatCurrency, Status, isActive, DateCreated)
    VALUES (111028,'Virtual Card','10.00','USD','2.5','USD','active',1,GETUTCDATE());
IF NOT EXISTS (SELECT 1 FROM dbo.tblM_Crypto_Network WHERE ID = 'F580A411-0E37-4287-B975-408172A2B4BF')
    INSERT INTO dbo.tblM_Crypto_Network (ID, Network, Symbol, isActive, DateCreated)
    VALUES ('F580A411-0E37-4287-B975-408172A2B4BF','TRC20','USDT',1,GETUTCDATE());

PRINT 'seed-reference: applied (roles, settings, counters, card type, network).';
