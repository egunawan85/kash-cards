-- =============================================================================
-- Card-buy idempotency guard (tblT_Card.UserReferenceID)
--
-- A card-open carries a per-attempt UserReferenceID: the cardholder buy page mints
-- one per page load into a hidden field, and the partner API requires one. The shared
-- money mechanic CardSpendService.OpenCard inserts the order, then debits the wallet.
-- This filtered unique index is the DB-layer idempotency gate: a double-click /
-- two-tab / Back-resubmit reuses the SAME UserReferenceID, hits the constraint
-- (2601/2627), and the app REPLAYS the original order instead of opening a second card
-- or debiting twice. Same house pattern as the webhook/referral dedup indexes
-- (0003/0004) -- a DB constraint, not a check-then-insert in app code (which races).
--
--   UIX_tblT_Card_User_Ref  UNIQUE (UserID, UserReferenceID)
--       WHERE UserReferenceID IS NOT NULL AND UserReferenceID <> ''
--
-- Filtered so legacy rows (which never carried a key -> NULL/'' UserReferenceID) are
-- excluded and never collide; only keyed orders dedup. A user can still buy the same
-- card type again later -- that is a NEW page load -> a NEW key -> a distinct row.
--
-- Deploy: additive only, no backfill, no EDMX regeneration. Apply BEFORE deploying the
-- code that relies on it (the app catches the duplicate-key; the index must exist first).
-- Idempotency: guarded with IF [NOT] EXISTS -- safe to re-run.
-- Pre-flight: a duplicate probe RAISERRORs if existing keyed rows already violate the
-- rule, so a dirty deploy fails actionably instead of on a cryptic CREATE INDEX error.
-- Column note: UserReferenceID ships as nvarchar(max), which cannot be an index key;
-- this script first narrows it to an indexable width.
-- =============================================================================

-- Make UserReferenceID indexable (narrow from nvarchar(max)). Guarded so re-runs are no-ops.
IF EXISTS (SELECT 1 FROM sys.columns
           WHERE object_id = OBJECT_ID('tblT_Card')
             AND name = 'UserReferenceID'
             AND (max_length = -1 OR max_length > 200))
BEGIN
    -- Pre-flight: a legacy ref longer than the new width would be silently truncated (cryptic 8152).
    -- Fail actionably instead. Harmless on re-run: once narrowed to nvarchar(100) no row can exceed it.
    IF EXISTS (SELECT 1 FROM dbo.tblT_Card WHERE LEN(UserReferenceID) > 100)
        RAISERROR('Cannot narrow UserReferenceID: rows >100 chars exist. Probe: SELECT ID, UserReferenceID FROM dbo.tblT_Card WHERE LEN(UserReferenceID) > 100;', 16, 1);
    ELSE
        ALTER TABLE tblT_Card ALTER COLUMN UserReferenceID nvarchar(100) NULL;
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes
               WHERE name = 'UIX_tblT_Card_User_Ref'
                 AND object_id = OBJECT_ID('tblT_Card'))
BEGIN
    IF EXISTS (
        SELECT UserID, UserReferenceID
        FROM tblT_Card
        WHERE UserReferenceID IS NOT NULL AND UserReferenceID <> ''
        GROUP BY UserID, UserReferenceID
        HAVING COUNT(*) > 1)
    BEGIN
        RAISERROR (
            'Cannot create UIX_tblT_Card_User_Ref: duplicate (UserID, UserReferenceID) rows exist. Investigate (possible prior double-open) and de-duplicate before re-running. Probe: SELECT UserID, UserReferenceID, COUNT(*) FROM tblT_Card WHERE UserReferenceID IS NOT NULL AND UserReferenceID <> '''' GROUP BY UserID, UserReferenceID HAVING COUNT(*) > 1;',
            16, 1);
    END
    ELSE
    BEGIN
        CREATE UNIQUE NONCLUSTERED INDEX UIX_tblT_Card_User_Ref
            ON tblT_Card (UserID, UserReferenceID)
            WHERE UserReferenceID IS NOT NULL AND UserReferenceID <> '';
    END
END
GO
