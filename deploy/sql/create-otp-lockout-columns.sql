-- =============================================================================
-- OTP brute-force lockout counter
--
-- Adds a per-session FailureCount column to the 5 OTP / Login / Register tables so the
-- verify-side handlers can atomically increment on the wrong-code branch and flip
-- isVerify = -1 at the threshold (5 failures) to permanently invalidate the session.
--
-- Companion code (QryptoCard.INT/Security/OtpLockout.cs, called from the verify handlers
-- in App/v1/UserV1Service and Admin/v1/AdminV1Service) issues an atomic UPDATE of the form:
--
--   UPDATE <table>
--   SET FailureCount = ISNULL(FailureCount, 0) + 1,
--       isVerify = CASE WHEN ISNULL(FailureCount, 0) + 1 >= 5 THEN -1 ELSE isVerify END
--   WHERE <pk> = @p0 AND isVerify = 0;
--
-- After the 5th failure the row has isVerify = -1, which the verify handlers' existing
-- "isVerify == 0" lookup then excludes — the attacker sees the unified "session is ended" /
-- "OTP is wrong" branch with no oracle distinguishing a locked session from a missing one.
--
-- Why raw SQL vs EF: the increment-and-threshold must be atomic across concurrent requests.
-- A single UPDATE with a CASE expression lets SQL Server serialize per-row; an EF
-- read-modify-write would race two parallel wrong-code attempts.
--
-- Why NOT add FailureCount to the EDMX / entity classes: runtime code never READS
-- FailureCount through EF; the only consumer is the atomic UPDATE above. Keeping it out of the
-- EDMX avoids a model regen. EF generates column-explicit SELECT / INSERT, so an unmapped
-- column in the DB is silently ignored on reads and defaults to NULL on inserts.
--
-- Why nullable: preserves wire compatibility for any EF-generated INSERT that doesn't list
-- FailureCount. ISNULL(FailureCount, 0) in the UPDATE treats NULL as 0.
--
-- Deploy ordering: apply this DDL to the target database BEFORE deploying the QryptoCard.INT
-- code that issues the UPDATE. Applying after would crash the UPDATE with "Invalid column name
-- 'FailureCount'" on every failed OTP attempt. Pure schema addition, no backfill.
--
-- Idempotency: IF NOT EXISTS guards on each ALTER make this safe to re-run.
-- =============================================================================

IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE Name = N'FailureCount' AND Object_ID = Object_ID(N'dbo.tblH_User_OTP')
)
BEGIN
    ALTER TABLE dbo.tblH_User_OTP ADD FailureCount int NULL;
END;
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE Name = N'FailureCount' AND Object_ID = Object_ID(N'dbo.tblH_User_Register')
)
BEGIN
    ALTER TABLE dbo.tblH_User_Register ADD FailureCount int NULL;
END;
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE Name = N'FailureCount' AND Object_ID = Object_ID(N'dbo.tblH_User_Login')
)
BEGIN
    ALTER TABLE dbo.tblH_User_Login ADD FailureCount int NULL;
END;
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE Name = N'FailureCount' AND Object_ID = Object_ID(N'dbo.tblH_Admin_OTP')
)
BEGIN
    ALTER TABLE dbo.tblH_Admin_OTP ADD FailureCount int NULL;
END;
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE Name = N'FailureCount' AND Object_ID = Object_ID(N'dbo.tblH_Admin_Login')
)
BEGIN
    ALTER TABLE dbo.tblH_Admin_Login ADD FailureCount int NULL;
END;
GO
