-- =============================================================================
-- Top-up idempotency guard (tblT_Card_Deposit.UserReferenceID)
--
-- The card-OPEN path (tblT_Card) already has a filtered unique idempotency index
-- (0007) + a duplicate-key replay in CardSpendService.OpenCard, so a re-submit of the
-- same UserReferenceID replays instead of debiting twice. The TOP-UP path
-- (tblT_Card_Deposit) had NO such guard: CardSpendService.TopUp inserted a fresh row
-- every call. The deposit-into-card streaming issuance re-runs a top-up whenever the
-- provider outcome is ambiguous (intent stays Issuing), so without this index +
-- replay a single top-up intent would debit the wallet and re-send to WasabiCard on
-- every tick. This adds the SAME house pattern used by 0007 to the top-up table.
--
--   UX_tblT_Card_Deposit_User_Ref  UNIQUE (UserID, UserReferenceID)
--       WHERE UserReferenceID IS NOT NULL AND UserReferenceID <> ''
--
-- Filtered so legacy rows (NULL/'' UserReferenceID) never collide. Deploy: additive,
-- no backfill, no EDMX regen. Apply BEFORE the code that relies on it. Idempotent.
-- =============================================================================

-- Make UserReferenceID indexable (narrow from nvarchar(max)). Guarded so re-runs are no-ops.
IF EXISTS (SELECT 1 FROM sys.columns
           WHERE object_id = OBJECT_ID('tblT_Card_Deposit')
             AND name = 'UserReferenceID'
             AND (max_length = -1 OR max_length > 200))
BEGIN
    IF EXISTS (SELECT 1 FROM dbo.tblT_Card_Deposit WHERE LEN(UserReferenceID) > 100)
        RAISERROR('Cannot narrow tblT_Card_Deposit.UserReferenceID: rows >100 chars exist. Probe: SELECT ID, UserReferenceID FROM dbo.tblT_Card_Deposit WHERE LEN(UserReferenceID) > 100;', 16, 1);
    ELSE
        ALTER TABLE tblT_Card_Deposit ALTER COLUMN UserReferenceID nvarchar(100) NULL;
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes
               WHERE name = 'UX_tblT_Card_Deposit_User_Ref'
                 AND object_id = OBJECT_ID('tblT_Card_Deposit'))
BEGIN
    IF EXISTS (
        SELECT UserID, UserReferenceID
        FROM tblT_Card_Deposit
        WHERE UserReferenceID IS NOT NULL AND UserReferenceID <> ''
        GROUP BY UserID, UserReferenceID
        HAVING COUNT(*) > 1)
    BEGIN
        RAISERROR (
            'Cannot create UX_tblT_Card_Deposit_User_Ref: duplicate (UserID, UserReferenceID) rows exist. Investigate (possible prior double top-up) and de-duplicate before re-running. Probe: SELECT UserID, UserReferenceID, COUNT(*) FROM tblT_Card_Deposit WHERE UserReferenceID IS NOT NULL AND UserReferenceID <> '''' GROUP BY UserID, UserReferenceID HAVING COUNT(*) > 1;',
            16, 1);
    END
    ELSE
    BEGIN
        CREATE UNIQUE NONCLUSTERED INDEX UX_tblT_Card_Deposit_User_Ref
            ON tblT_Card_Deposit (UserID, UserReferenceID)
            WHERE UserReferenceID IS NOT NULL AND UserReferenceID <> '';
    END
END
GO
