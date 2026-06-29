-- =============================================================================
-- WasabiCard auto-funding (WC1 monitor / WC2 refill+eager / WC3 coverage)
--
-- Adds:
--   1. dbo.tblH_WasabiCard_Refill -- the money-OUT ledger. Every outbound USDT
--      transfer (floor refill or eager deposit pass-through) is recorded here
--      FIRST, keyed by a UNIQUE PartnerReferenceID, so a duplicate attempt is a
--      no-op (idempotency). In-flight accounting and the daily cap read this table.
--   2. tblM_Setting rows for the tunable thresholds. The master kill-switch
--      WasabiCardAutoFundEnabled defaults to 0 (OFF) -- nothing moves money until
--      it is deliberately set to 1. CardDepositFeeRate (the 3% platform margin) is
--      seeded by 0008 and reused.
--
-- Deploy: additive, idempotent. Guarded with IF NOT EXISTS -- safe to re-run, and
-- it never overwrites an admin-edited setting once the row exists.
-- =============================================================================

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = N'tblH_WasabiCard_Refill' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    CREATE TABLE dbo.tblH_WasabiCard_Refill (
        ID                  bigint IDENTITY(1,1) NOT NULL CONSTRAINT PK_tblH_WasabiCard_Refill PRIMARY KEY,
        RefillType          nvarchar(20)  NOT NULL,        -- 'floor' | 'eager'
        PartnerReferenceID  nvarchar(100) NOT NULL,        -- our idempotency key (sent to Runegate)
        DepositTxId         nvarchar(200) NULL,            -- source deposit TransactionID (eager only)
        NetUsd              decimal(18,4) NOT NULL,        -- USD we intend to LAND at WasabiCard
        SentUsdt            decimal(18,6) NOT NULL,        -- grossed-up USDT amount submitted on-chain
        Status              nvarchar(20)  NOT NULL,        -- Initiated|Submitted|Confirmed|Failed|Unknown
        ProviderRef         nvarchar(200) NULL,            -- best-effort Runegate transfer id / tx hash
        Note                nvarchar(500) NULL,
        CreatedDate         datetime      NOT NULL,
        UpdatedDate         datetime      NULL
    );

    -- The idempotency guarantee: a second attempt with the same PartnerReferenceID
    -- (redelivered deposit webhook, racing tick) fails the insert -> treated as a no-op.
    CREATE UNIQUE INDEX UX_WasabiCard_Refill_PartnerRef
        ON dbo.tblH_WasabiCard_Refill (PartnerReferenceID);

    -- In-flight queries filter on Status; the daily cap filters on CreatedDate.
    CREATE INDEX IX_WasabiCard_Refill_Status  ON dbo.tblH_WasabiCard_Refill (Status);
    CREATE INDEX IX_WasabiCard_Refill_Created ON dbo.tblH_WasabiCard_Refill (CreatedDate);
END
GO

-- ---- tunable settings (Value is double; Param1 holds string config) ----

-- Master kill-switch: 0 = OFF (ships disabled). NOTHING moves money until set to 1.
IF NOT EXISTS (SELECT 1 FROM dbo.tblM_Setting WHERE Name = 'WasabiCardAutoFundEnabled')
    INSERT INTO dbo.tblM_Setting (Name, Value, DateCreated) VALUES ('WasabiCardAutoFundEnabled', 0, GETDATE());
GO

-- Floor: refill trigger + low-balance alert threshold (USD).
IF NOT EXISTS (SELECT 1 FROM dbo.tblM_Setting WHERE Name = 'WasabiCardFloorUsd')
    INSERT INTO dbo.tblM_Setting (Name, Value, DateCreated) VALUES ('WasabiCardFloorUsd', 500, GETDATE());
GO

-- Target: floor refill tops the float back up to here (USD).
IF NOT EXISTS (SELECT 1 FROM dbo.tblM_Setting WHERE Name = 'WasabiCardTargetUsd')
    INSERT INTO dbo.tblM_Setting (Name, Value, DateCreated) VALUES ('WasabiCardTargetUsd', 700, GETDATE());
GO

-- Eager pass-through trigger: deposits strictly greater than this are forwarded (USD).
IF NOT EXISTS (SELECT 1 FROM dbo.tblM_Setting WHERE Name = 'WasabiCardEagerThresholdUsd')
    INSERT INTO dbo.tblM_Setting (Name, Value, DateCreated) VALUES ('WasabiCardEagerThresholdUsd', 500, GETDATE());
GO

-- Daily circuit breaker: total transferred in any rolling 24h is capped here (USD).
IF NOT EXISTS (SELECT 1 FROM dbo.tblM_Setting WHERE Name = 'WasabiCardDailyCapUsd')
    INSERT INTO dbo.tblM_Setting (Name, Value, DateCreated) VALUES ('WasabiCardDailyCapUsd', 30000, GETDATE());
GO

-- Skip uneconomic tiny transfers below this net amount (USD).
IF NOT EXISTS (SELECT 1 FROM dbo.tblM_Setting WHERE Name = 'WasabiCardMinTransferUsd')
    INSERT INTO dbo.tblM_Setting (Name, Value, DateCreated) VALUES ('WasabiCardMinTransferUsd', 50, GETDATE());
GO

-- WasabiCard's deposit fee %, used to gross up the send so the net amount lands.
IF NOT EXISTS (SELECT 1 FROM dbo.tblM_Setting WHERE Name = 'WasabiCardWcFeeRatePct')
    INSERT INTO dbo.tblM_Setting (Name, Value, DateCreated) VALUES ('WasabiCardWcFeeRatePct', 1.4, GETDATE());
GO

-- Re-alert cadence (hours) while a low/blind condition persists.
IF NOT EXISTS (SELECT 1 FROM dbo.tblM_Setting WHERE Name = 'WasabiCardReAlertHours')
    INSERT INTO dbo.tblM_Setting (Name, Value, DateCreated) VALUES ('WasabiCardReAlertHours', 6, GETDATE());
GO

-- Ops alert recipient, in Param1 (optional). NULL by default -> alerts fall back to the
-- OPS_ALERT_EMAIL env var, then EMAIL_FROM. OPERATOR: set Param1 to a monitored inbox so the
-- low-balance / cap / ambiguous-transfer alerts are actually seen.
IF NOT EXISTS (SELECT 1 FROM dbo.tblM_Setting WHERE Name = 'WasabiCardAlertEmail')
    INSERT INTO dbo.tblM_Setting (Name, Value, DateCreated, Param1) VALUES ('WasabiCardAlertEmail', NULL, GETDATE(), NULL);
GO

-- WasabiCard USDT-TRC20 deposit address (the transfer destination), in Param1. Seeded with the
-- value verified via the WasabiCard wallet addressList API. OPERATOR: re-verify before enabling
-- auto-funding -- a wrong address sends real funds to the wrong place. The funding service refuses
-- to transfer if this is absent or not a valid TRON address.
IF NOT EXISTS (SELECT 1 FROM dbo.tblM_Setting WHERE Name = 'WasabiCardDepositAddress')
    INSERT INTO dbo.tblM_Setting (Name, Value, DateCreated, Param1)
    VALUES ('WasabiCardDepositAddress', NULL, GETDATE(), 'TC75gyAywXztsQAmwLhkAvcYm6yEjjBEQP');
GO
