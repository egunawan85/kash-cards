-- =============================================================================
-- Deposit-into-card Phase D: reconciliation & ops visibility (D1 view + D2 drift settings)
--
-- D1: vw_Card_Funding_Reconcile stitches an intent's whole money trail into one row
--     (intent + invoice + forward ledger + order + current wallet balance) so ops can
--     see "where's the money" without hand-joining five tables. READ-ONLY view.
-- D2: settings for the streaming float-drift / stuck-forward alert emitted by the
--     WasabiCard monitor tick.
--
-- Deploy: additive, idempotent, self-healing (mirrors 0011/0012). The view is dropped +
-- recreated so a re-run always reflects the current shape.
-- =============================================================================

-- ---- D1: money-trail reconcile view -----------------------------------------
IF OBJECT_ID('dbo.vw_Card_Funding_Reconcile', 'V') IS NOT NULL
    DROP VIEW dbo.vw_Card_Funding_Reconcile;
GO
CREATE VIEW dbo.vw_Card_Funding_Reconcile AS
SELECT
    i.IntentID, i.UserID, i.Kind, i.Status AS IntentStatus,
    i.InvoiceID, i.InvoiceAddress,
    i.ExpectedTotal, i.ReceivedTotal,
    (i.ExpectedTotal - i.ReceivedTotal) AS Shortfall,
    CASE WHEN i.ReceivedTotal > i.ExpectedTotal THEN 1 ELSE 0 END AS IsOverpaid,
    i.Face, i.CardNo AS IntentCardNo, i.OrderID,
    i.CreatedDate, i.UpdatedDate, i.ExpiryDate,
    -- outbound forward to WasabiCard (one per intent: DepositTxId = IntentID, RefillType='intent')
    f.PartnerReferenceID   AS ForwardRef,
    f.Status               AS ForwardStatus,
    f.NetUsd               AS ForwardNetUsd,
    f.SentUsdt             AS ForwardSentUsdt,
    f.ActualReceivedUsd    AS ForwardLandedUsd,
    f.ChainTxHash          AS ForwardChainTxHash,
    f.ProviderRef          AS ForwardProviderRef,
    f.SubmittedDate        AS ForwardSubmittedDate,
    f.ConfirmedDate        AS ForwardConfirmedDate,
    f.LandedDate           AS ForwardLandedDate,
    -- the issued order (new card in tblT_Card, top-up in tblT_Card_Deposit; UserReferenceID = IntentID)
    COALESCE(c.Status, d.Status) AS OrderStatus,
    COALESCE(c.CardNo, i.CardNo) AS OrderCardNo,
    -- the user's current internal wallet (available balance), for context
    b.Balance AS WalletBalance
FROM dbo.tblT_Card_Funding_Intent i
LEFT JOIN dbo.tblH_WasabiCard_Refill f
    ON f.RefillType = 'intent' AND f.DepositTxId = i.IntentID
LEFT JOIN dbo.tblT_Card c
    ON c.UserReferenceID = i.IntentID
LEFT JOIN dbo.tblT_Card_Deposit d
    ON d.UserReferenceID = i.IntentID
LEFT JOIN dbo.tblM_User_Balance b
    ON b.UserID = i.UserID AND b.Currency = 'USDT' AND b.isActive = 1;
GO

-- ---- D2: drift / stuck-forward alert settings -------------------------------

-- A streaming forward that has been SENT but not confirmed-landed for longer than this (minutes)
-- is flagged by the monitor tick — money left Runegate but WasabiCard hasn't confirmed the float
-- credit, i.e. potential stranding / drift. Alert-only; never auto-resolves.
IF NOT EXISTS (SELECT 1 FROM dbo.tblM_Setting WHERE Name = 'CardFundingForwardStaleAlertMinutes')
    INSERT INTO dbo.tblM_Setting (Name, Value, DateCreated) VALUES ('CardFundingForwardStaleAlertMinutes', 30, GETDATE());
GO

-- Throttle state for the stuck-forward alert (Param1 = last state, Param2 = last alert ISO), so it
-- doesn't re-fire every tick while the condition persists. NULL until first alert.
IF NOT EXISTS (SELECT 1 FROM dbo.tblM_Setting WHERE Name = 'CardFundingDriftAlertState')
    INSERT INTO dbo.tblM_Setting (Name, Value, DateCreated, Param1) VALUES ('CardFundingDriftAlertState', NULL, GETDATE(), NULL);
GO
