-- =============================================================================
-- seed-admin.sql -- the bootstrap staff admin (tblM_Admin). The email and the
-- bcrypt-hashed password arrive via sqlcmd -v from vm-seed.ps1 (which computes
-- PasswordHasher.Hash(password) via the app's BCrypt library). NO secret in this file.
-- ADMIN_PWD_DB is a one-way bcrypt hash (60 chars, '$2a$...'); the column is nvarchar(MAX).
--
-- Idempotent: INSERT-ONLY, guarded by NOT EXISTS on Email. There is intentionally
-- NO UPDATE branch -- a redeploy must never flip isActive/isBanned back on for a
-- row an operator deliberately deactivated. Mirrors runegate's seed-admin-bootstrap.
-- Run AFTER seed-reference.sql (it needs the 'role-admin' role row).
-- isVerified=1 so first login is immediate; OTP is still required at login time.
-- =============================================================================
SET NOCOUNT ON;
IF NOT EXISTS (SELECT 1 FROM dbo.tblM_Admin WHERE Email = N'$(ADMIN_EMAIL)')
BEGIN
    INSERT INTO dbo.tblM_Admin (AdminID, Email, FirstName, LastName, Password, RoleID, Phone, isActive, isVerified, isBanned, DateJoin)
    VALUES (LOWER(CONVERT(nvarchar(36), NEWID())), N'$(ADMIN_EMAIL)', N'$(ADMIN_FIRST)', N'$(ADMIN_LAST)', N'$(ADMIN_PWD_DB)', 'role-admin', '+10000000000', 1, 1, 0, GETUTCDATE());
    PRINT 'seed-admin: inserted $(ADMIN_EMAIL)';
END
ELSE
    PRINT 'seed-admin: $(ADMIN_EMAIL) already exists -- no change.';
