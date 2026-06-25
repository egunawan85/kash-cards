-- =============================================================================
-- seed-smoke-user.sql -- the DEV/TEST smoke API user (tblM_User + children + an
-- API credential). DEV/TEST ONLY -- never apply against prod. Values arrive via
-- sqlcmd -v from vm-seed.ps1. Idempotent: deletes the fixed smoke identity first
-- (so the API key/secret rotate cleanly each run), then re-inserts.
-- Run AFTER seed-reference.sql (needs 'role-owner' + the TRC20 network row).
-- =============================================================================
SET NOCOUNT ON;
DECLARE @u nvarchar(50) = N'11111111-1111-1111-1111-111111111111';

-- Reset the fixed smoke identity (children first to respect any FKs).
DELETE FROM dbo.tblM_User_API            WHERE UserID = @u;
DELETE FROM dbo.tblM_User_Crypto_Deposit WHERE UserID = @u;
DELETE FROM dbo.tblM_User_Referral       WHERE UserID = @u;
DELETE FROM dbo.tblM_User_Commission     WHERE UserID = @u;
DELETE FROM dbo.tblM_User_Balance        WHERE UserID = @u;
DELETE FROM dbo.tblM_User                WHERE UserID = @u OR Email = N'$(SMOKE_EMAIL)';

INSERT INTO dbo.tblM_User (UserID, Email, FirstName, LastName, Password, RoleID, Phone, isActive, isVerified, isBanned, DateJoin)
VALUES (@u, N'$(SMOKE_EMAIL)', 'Smoke', 'User', N'$(SMOKE_USER_PWD_DB)', 'role-owner', '+10000000001', 1, 1, 0, GETUTCDATE());

INSERT INTO dbo.tblM_User_Balance        (BalanceID, UserID, Currency, Balance, isActive, DateCreated) VALUES (LOWER(CONVERT(nvarchar(36),NEWID())), @u, 'USDT', 0, 1, GETUTCDATE());
INSERT INTO dbo.tblM_User_Commission     (CommissionID, UserID, Commission, DateCreated)               VALUES (LOWER(CONVERT(nvarchar(36),NEWID())), @u, 0.1, GETUTCDATE());
INSERT INTO dbo.tblM_User_Referral       (UserID, Code, DateCreated)                                   VALUES (@u, UPPER(SUBSTRING(REPLACE(CONVERT(nvarchar(36),NEWID()),'-',''),1,8)), GETUTCDATE());
INSERT INTO dbo.tblM_User_Crypto_Deposit (ID, UserID, NetworkID, Address, isActive, DateCreated)       VALUES (LOWER(CONVERT(nvarchar(36),NEWID())), @u, 'F580A411-0E37-4287-B975-408172A2B4BF', 'T' + SUBSTRING(REPLACE(CONVERT(nvarchar(36),NEWID()),'-',''),1,12), 1, GETUTCDATE());
INSERT INTO dbo.tblM_User_API            (UserID, APIKey, SecretKey, isActive, DateCreated)            VALUES (@u, N'$(SMOKE_API_KEY)', N'$(SMOKE_API_SECRET_DB)', 1, GETUTCDATE());

PRINT 'seed-smoke-user: re-seeded $(SMOKE_EMAIL) with API key $(SMOKE_API_KEY)';
