-- =============================================================================
-- Password-login brute-force lockout columns
--
-- Adds FailureCount + LockoutEnd columns to the two identity tables (tblM_User, tblM_Admin) so the
-- Login handlers can atomically count failed password attempts and, at the threshold (5 failures),
-- lock the account for a window (15 minutes) — the first-factor companion to the OTP lockout
-- (create-otp-lockout-columns.sql) which guards the second factor.
--
-- Companion code (QryptoCard.INT/Security/PasswordLockout.cs, called from Login in
-- App/v1/UserV1Service and Admin/v1/AdminV1Service):
--   - on a wrong password it issues an atomic UPDATE … CASE … that increments FailureCount and sets
--     LockoutEnd = now + 15 min when the increment reaches the threshold (with an expired-lockout
--     branch that resets to a fresh window so an attempt right after expiry can't perpetually re-lock);
--   - a correct password resets FailureCount = 0, LockoutEnd = NULL;
--   - while LockoutEnd is in the future the handler returns the SAME "Your password is incorrect"
--     message as an ordinary wrong password, so a locked account is not an enumeration oracle.
--
-- Why raw SQL vs EF: the increment-and-threshold must be atomic across concurrent requests. A single
-- UPDATE with CASE lets SQL Server serialize per-row; an EF read-modify-write would race two parallel
-- wrong-password attempts.
--
-- Why NOT add these to the EDMX / entity classes: runtime code never READS them through EF; the only
-- consumers are the atomic UPDATE and a raw SELECT (PasswordLockout.IsLockedOut). Keeping them out of
-- the EDMX avoids a model regen. EF generates column-explicit SELECT/INSERT, so unmapped columns are
-- silently ignored on reads and default to NULL on inserts.
--
-- Why nullable: preserves wire compatibility for any EF-generated INSERT that doesn't list them.
-- ISNULL(FailureCount, 0) in the UPDATE treats NULL as 0; a NULL LockoutEnd means "not locked".
--
-- Deploy ordering: apply this DDL to the target database BEFORE deploying the QryptoCard.INT code that
-- references the columns. Applying after would crash login with "Invalid column name" on every failed
-- password attempt. Pure schema addition, no backfill.
--
-- Idempotency: IF NOT EXISTS guards on each ALTER make this safe to re-run.
-- =============================================================================

IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE Name = N'FailureCount' AND Object_ID = Object_ID(N'dbo.tblM_User')
)
BEGIN
    ALTER TABLE dbo.tblM_User ADD FailureCount int NULL;
END;
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE Name = N'LockoutEnd' AND Object_ID = Object_ID(N'dbo.tblM_User')
)
BEGIN
    ALTER TABLE dbo.tblM_User ADD LockoutEnd datetime NULL;
END;
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE Name = N'FailureCount' AND Object_ID = Object_ID(N'dbo.tblM_Admin')
)
BEGIN
    ALTER TABLE dbo.tblM_Admin ADD FailureCount int NULL;
END;
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE Name = N'LockoutEnd' AND Object_ID = Object_ID(N'dbo.tblM_Admin')
)
BEGIN
    ALTER TABLE dbo.tblM_Admin ADD LockoutEnd datetime NULL;
END;
GO
