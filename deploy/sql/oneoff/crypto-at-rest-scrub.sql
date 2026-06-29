-- =============================================================================
-- crypto-at-rest-scrub.sql  —  ONE-OFF, operator-run (NOT an auto-applied migration).
--
-- Purpose: remove every at-rest secret that was stored under the old reversible
-- Rijndael cipher (IV == key) keyed off the historically-leaked DBKEY/APPKEY. After
-- the bcrypt + AES-256-GCM code ships, those columns still hold the OLD ciphertext,
-- which is recoverable by anyone with a DB copy and the leaked key. This scrubs them.
--
-- NOT in deploy/sql/migrations/ on purpose: the numbered migrations auto-apply on
-- every deploy and every environment (including fresh provisions). This is a
-- deliberate, destructive, prod-cutover action — it FORCES A PASSWORD RESET for every
-- user and admin and removes all enrolled 2FA + API secrets. Run it by hand, once, in
-- the cutover window, AFTER the bcrypt code is deployed. See the runbook:
-- docs/runbooks/crypto-at-rest-cutover.md
--
-- Idempotent: re-running re-applies the same sentinels (harmless). This is NOT a no-op on
-- a freshly-provisioned DB -- the unconditional admin UPDATE below would clobber the
-- bootstrap admin's freshly-seeded bcrypt hash -- so the runbook says SKIP this script on a
-- fresh provision (there are no legacy leaked-key rows to scrub there anyway).
--
-- Invocation:
--   sqlcmd -S <server> -d <db> -E -b -i crypto-at-rest-scrub.sql
-- =============================================================================
SET NOCOUNT ON;
SET XACT_ABORT ON;
BEGIN TRANSACTION;

DECLARE @users int, @admins int, @twofa int, @apis int;

-- Passwords: overwrite the recoverable ciphertext with a non-bcrypt sentinel. The
-- sentinel can never satisfy bcrypt verification (PasswordHasher.Verify swallows the
-- SaltParseException and returns false), so every account must go through the
-- password-reset flow to set a new bcrypt password.
UPDATE dbo.tblM_User  SET Password = N'!RESET-REQUIRED-CRYPTO-MIGRATION!';
SET @users = @@ROWCOUNT;
UPDATE dbo.tblM_Admin SET Password = N'!RESET-REQUIRED-CRYPTO-MIGRATION!';
SET @admins = @@ROWCOUNT;

-- 2FA seeds were AES-under-the-leaked-key and cannot be decrypted by the new
-- AES-256-GCM AesUtility anyway. Remove them and clear the per-user flag so affected
-- users re-enrol cleanly. (2FA was never actually verified at login, so live
-- enrolments are expected to be ~zero.)
DELETE FROM dbo.tblM_User_2FA;
SET @twofa = @@ROWCOUNT;
UPDATE dbo.tblM_User SET is2FA = 0, Date2FA = NULL WHERE is2FA = 1 OR Date2FA IS NOT NULL;

-- API secrets were stored under the leaked key too. Neutralise them (non-bcrypt
-- sentinel => validateAPI always fails) and deactivate the keys so they must be
-- re-issued. APIKey is left intact for operator reference / re-issue.
UPDATE dbo.tblM_User_API SET SecretKey = N'!REISSUE-REQUIRED-CRYPTO-MIGRATION!', isActive = 0;
SET @apis = @@ROWCOUNT;

COMMIT TRANSACTION;

PRINT 'crypto-at-rest-scrub: passwords reset users=' + CAST(@users AS varchar(10))
    + ' admins=' + CAST(@admins AS varchar(10))
    + '; 2FA rows removed=' + CAST(@twofa AS varchar(10))
    + '; API secrets neutralised=' + CAST(@apis AS varchar(10)) + '.';
PRINT 'NEXT: restore bootstrap admin access (runbook step 4) and notify users to reset their passwords.';
