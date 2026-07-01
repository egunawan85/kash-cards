-- =============================================================================
-- Deposit-into-card: card-funding intents + streaming-forward support
--
-- Adds:
--   1. dbo.tblT_Card_Funding_Intent -- the record of a user's intent to fund a card
--      (a new card, or a top-up of an owned card) by depositing crypto to their fixed
--      address. Managed by raw SQL (like tblH_WasabiCard_Refill) -- NOT an EF entity.
--      A filtered UNIQUE index enforces AT MOST ONE OPEN intent per user, so a deposit
--      to the shared fixed address maps unambiguously to one intent.
--   2. Timing columns on dbo.tblH_WasabiCard_Refill (SubmittedDate / ConfirmedDate) so
--      the outbound-forward -> WasabiCard-credit latency is actually measurable (the
--      single mutable UpdatedDate could not capture it).
--   3. tblM_Setting rows for the new tunables. Every money-moving switch ships OFF /
--      neutral: CardFundingStreamingEnabled = 0, CardFixedDepositFee = 0,
--      CardMinDepositUsd = 0 (fall back to the WasabiCard per-program quota).
--
-- Deploy: additive, idempotent. Guarded with IF NOT EXISTS -- safe to re-run, and it
-- never overwrites an admin-edited setting once the row exists.
-- =============================================================================

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = N'tblT_Card_Funding_Intent' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    CREATE TABLE dbo.tblT_Card_Funding_Intent (
        ID              bigint IDENTITY(1,1) NOT NULL CONSTRAINT PK_tblT_Card_Funding_Intent PRIMARY KEY,
        IntentID        nvarchar(64)  NOT NULL,        -- opaque public id (GUID) used by the app API
        UserID          nvarchar(128) NOT NULL,        -- owner (matches tblM_User_Balance.UserID)
        Kind            nvarchar(10)  NOT NULL,        -- 'new' | 'topup'
        CardTypeId      bigint        NULL,            -- new-card only (from the live catalog)
        HolderID        bigint        NULL,            -- KYC holder, created at intent creation (fail-fast)
        CardNo          nvarchar(64)  NULL,            -- topup: the target card; new: bound after issuance
        OrderID         nvarchar(64)  NULL,            -- tblT_Card / tblT_Card_Deposit order id once issuance starts
        DepositAddress  nvarchar(128) NULL,            -- fixed deposit address shown to the user (snapshot)

        -- Pricing snapshot (what the customer was quoted; history immutable).
        Face            decimal(18,4) NOT NULL,        -- amount that must LAND on the card (deposit / top-up)
        Price           decimal(18,4) NOT NULL CONSTRAINT DF_CFI_Price DEFAULT (0),  -- card price (new only)
        FeeInPercentage decimal(9,4)  NOT NULL CONSTRAINT DF_CFI_FeePct DEFAULT (0), -- % rate snapshot (e.g. 3)
        PercentageFee   decimal(18,4) NOT NULL CONSTRAINT DF_CFI_PctFee DEFAULT (0), -- FeeInPercentage% * Face
        FixedFee        decimal(18,4) NOT NULL CONSTRAINT DF_CFI_FixFee DEFAULT (0), -- flat fixed deposit fee
        ExpectedTotal   decimal(18,4) NOT NULL,        -- Price + Face + PercentageFee + FixedFee (send-amount)
        ReceivedTotal   decimal(18,4) NOT NULL CONSTRAINT DF_CFI_Recv DEFAULT (0),   -- credited-so-far (X of Y)

        Status          nvarchar(20)  NOT NULL,        -- Pending|Funding|Confirming|Issuing|Completed|Expired|Cancelled|Failed
        ForwardRef      nvarchar(100) NULL,            -- PartnerReferenceID of the outbound forward (-> tblH_WasabiCard_Refill)
        Note            nvarchar(500) NULL,

        CreatedDate     datetime      NOT NULL,
        UpdatedDate     datetime      NULL,
        ExpiryDate      datetime      NULL
    );
END
GO

-- Indexes guarded INDIVIDUALLY (not only inside the table IF NOT EXISTS) so a partial prior run
-- that created the table but not every index self-heals on re-run.

-- Public id lookup (app polls status by IntentID).
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_CFI_IntentID' AND object_id = OBJECT_ID('dbo.tblT_Card_Funding_Intent'))
    CREATE UNIQUE INDEX UX_CFI_IntentID ON dbo.tblT_Card_Funding_Intent (IntentID);
GO
-- The one-open-intent-per-user guarantee: only one row per user may be in an OPEN status. The filter
-- predicate is on the base Status column directly (NOT a computed column — SQL Server rejects a
-- filtered-index predicate that references a computed column, even PERSISTED).
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_CFI_OneOpenPerUser' AND object_id = OBJECT_ID('dbo.tblT_Card_Funding_Intent'))
    CREATE UNIQUE INDEX UX_CFI_OneOpenPerUser ON dbo.tblT_Card_Funding_Intent (UserID)
        WHERE Status IN ('Pending','Funding','Confirming','Issuing');
GO
-- Settlement looks up a user's open intent; expiry sweep filters on ExpiryDate.
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_CFI_User_Status' AND object_id = OBJECT_ID('dbo.tblT_Card_Funding_Intent'))
    CREATE INDEX IX_CFI_User_Status ON dbo.tblT_Card_Funding_Intent (UserID, Status);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_CFI_Expiry' AND object_id = OBJECT_ID('dbo.tblT_Card_Funding_Intent'))
    CREATE INDEX IX_CFI_Expiry ON dbo.tblT_Card_Funding_Intent (ExpiryDate);
GO

-- Real forward-timing capture on the refill ledger (the previously-single UpdatedDate could
-- not measure submitted->confirmed latency). Additive; NULL for historical rows.
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE Name = N'SubmittedDate' AND Object_ID = Object_ID(N'dbo.tblH_WasabiCard_Refill'))
    ALTER TABLE dbo.tblH_WasabiCard_Refill ADD SubmittedDate datetime NULL;
GO
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE Name = N'ConfirmedDate' AND Object_ID = Object_ID(N'dbo.tblH_WasabiCard_Refill'))
    ALTER TABLE dbo.tblH_WasabiCard_Refill ADD ConfirmedDate datetime NULL;
GO

-- ---- tunable settings (Value is double; Param1 holds string config) ----

-- Master kill-switch for the streaming deposit-into-card settlement path: 0 = OFF (ships
-- disabled). Separate from WasabiCardAutoFundEnabled; NOTHING settles a deposit into a card
-- automatically until this is set to 1.
IF NOT EXISTS (SELECT 1 FROM dbo.tblM_Setting WHERE Name = 'CardFundingStreamingEnabled')
    INSERT INTO dbo.tblM_Setting (Name, Value, DateCreated) VALUES ('CardFundingStreamingEnabled', 0, GETDATE());
GO

-- Flat "fixed deposit fee" charged per card funding (USDT), covering the Runegate withdrawal
-- (float top-up) cost. Ships 0 until the operator sets it (client to decide 0 / 1.5 / 3.5).
IF NOT EXISTS (SELECT 1 FROM dbo.tblM_Setting WHERE Name = 'CardFixedDepositFee')
    INSERT INTO dbo.tblM_Setting (Name, Value, DateCreated) VALUES ('CardFixedDepositFee', 0, GETDATE());
GO

-- Optional minimum deposit (USD). 0 = no override -> fall back to the WasabiCard per-program
-- quota (DepositAmountMinQuotaForActiveCard). Raise only if the fixed fee is set below the
-- real transfer cost so small cards would otherwise be margin-negative.
IF NOT EXISTS (SELECT 1 FROM dbo.tblM_Setting WHERE Name = 'CardMinDepositUsd')
    INSERT INTO dbo.tblM_Setting (Name, Value, DateCreated) VALUES ('CardMinDepositUsd', 0, GETDATE());
GO

-- How long a Pending intent waits for funds before it expires (minutes). Funds already
-- received remain as internal-wallet residual and apply to the next purchase.
IF NOT EXISTS (SELECT 1 FROM dbo.tblM_Setting WHERE Name = 'CardFundingIntentExpiryMinutes')
    INSERT INTO dbo.tblM_Setting (Name, Value, DateCreated) VALUES ('CardFundingIntentExpiryMinutes', 1440, GETDATE());
GO

-- WasabiCard flat card-creation cost drawn from the float on a new-card open (USD). Prod data
-- shows exactly $1 per create; a top-up incurs none. Forwarded amount = draw for the card.
IF NOT EXISTS (SELECT 1 FROM dbo.tblM_Setting WHERE Name = 'WasabiCardCreateCostUsd')
    INSERT INTO dbo.tblM_Setting (Name, Value, DateCreated) VALUES ('WasabiCardCreateCostUsd', 1, GETDATE());
GO

-- WasabiCard deposit fee % applied on a NEW-card open, used to size the forward. Prod data
-- shows 0% on open (WasabiCard draws exactly face + $1 create). Kept tunable; DO NOT blanket
-- gross-up by the top-up rate or we over-forward into the one-way float.
IF NOT EXISTS (SELECT 1 FROM dbo.tblM_Setting WHERE Name = 'WasabiCardWcFeeRatePctOpen')
    INSERT INTO dbo.tblM_Setting (Name, Value, DateCreated) VALUES ('WasabiCardWcFeeRatePctOpen', 0, GETDATE());
GO

-- WasabiCard deposit fee % applied on a TOP-UP (separate from the open rate; prod shows 0% for both,
-- but WasabiCard could price top-ups differently). Never size a top-up forward off the open rate.
IF NOT EXISTS (SELECT 1 FROM dbo.tblM_Setting WHERE Name = 'WasabiCardWcFeeRatePctTopUp')
    INSERT INTO dbo.tblM_Setting (Name, Value, DateCreated) VALUES ('WasabiCardWcFeeRatePctTopUp', 0, GETDATE());
GO

-- Streaming forward confirmation loop: poll cadence (seconds) and max wait (minutes) for the
-- WasabiCard float to reflect our forward before parking the intent for reconciliation.
IF NOT EXISTS (SELECT 1 FROM dbo.tblM_Setting WHERE Name = 'WasabiCardForwardConfirmPollSeconds')
    INSERT INTO dbo.tblM_Setting (Name, Value, DateCreated) VALUES ('WasabiCardForwardConfirmPollSeconds', 30, GETDATE());
GO
IF NOT EXISTS (SELECT 1 FROM dbo.tblM_Setting WHERE Name = 'WasabiCardForwardConfirmTimeoutMinutes')
    INSERT INTO dbo.tblM_Setting (Name, Value, DateCreated) VALUES ('WasabiCardForwardConfirmTimeoutMinutes', 30, GETDATE());
GO

-- Allowlisted treasury address (Param1) for sweeping retained margin out of the Runegate
-- wallet. NULL by default -> sweeps refuse to run until an operator sets a verified address.
IF NOT EXISTS (SELECT 1 FROM dbo.tblM_Setting WHERE Name = 'TreasurySweepAddress')
    INSERT INTO dbo.tblM_Setting (Name, Value, DateCreated, Param1) VALUES ('TreasurySweepAddress', NULL, GETDATE(), NULL);
GO
