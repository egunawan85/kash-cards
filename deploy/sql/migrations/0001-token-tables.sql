-- =============================================================================
-- Opaque-random Bearer-token infrastructure
--   (tblT_AuthToken, tblT_RefreshToken, tblH_Auth_Log)
--
-- Replaces HTTP Basic auth between dashboard and upstream API with opaque random
-- Bearer tokens. Three tables:
--
--   tblT_AuthToken      — short-lived (15 min) access tokens. Stored as SHA-256
--                         hex; plaintext never touches the DB. Verifier does an
--                         indexed exact-match lookup on TokenHash. Subject +
--                         SubjectType identify the user/admin without an FK (so
--                         ban/delete on tblM_User / tblM_Admin never cascades —
--                         chain-revoke handles it).
--
--   tblT_RefreshToken   — long-lived (7 day) refresh tokens with rotation. Every
--                         refresh() consumes one row, issues a new one, and links
--                         them via ReplacedByID. RotationChainRoot points at the
--                         original (login-time) refresh token in the chain — used
--                         by reuse-detection chain-revoke (sets RevokedAt on every
--                         row sharing the same RotationChainRoot in one sweep).
--
--   tblH_Auth_Log       — append-only audit ledger. Receives "refresh_token_reuse"
--                         / "refresh_token_concurrent_use" / "logout" /
--                         "subject_revoke" / "revoke_token_auth_failure" events for
--                         alerting. Separate from tblH_User_Login /
--                         tblH_Admin_Login (those are OTP session-tracking tables
--                         with a different shape).
--
-- ID columns are nvarchar(50) to match codebase convention (tblM_User.UserID,
-- tblM_Admin.AdminID, tblH_*.ID). TokenHash is char(64): SHA-256 hex is exactly
-- 64 ASCII chars; fixed-width char saves space over nvarchar.
--
-- No FKs: Subject is a value-typed pointer at tblM_User.UserID OR tblM_Admin.AdminID,
-- discriminated by SubjectType. Cascade behaviour on user/admin delete or ban is
-- handled explicitly via revokeAllForSubject — the WCF-side logic is the correct
-- ownership boundary, not a DB-side cascade.
--
-- Deploy: apply this DDL BEFORE deploying the AuthV1Service code. AuthDbContext
-- uses Database.SetInitializer<AuthDbContext>(null) — it will NOT auto-create
-- these tables. No EDMX regeneration needed (separate code-first AuthDbContext).
--
-- Idempotency: guarded with IF NOT EXISTS — safe to re-run on re-deploy.
-- =============================================================================

IF NOT EXISTS (SELECT 1 FROM sys.objects WHERE type = 'U' AND name = 'tblT_AuthToken')
BEGIN
    CREATE TABLE tblT_AuthToken
    (
        TokenID                 nvarchar(50)    NOT NULL,
        TokenHash               char(64)        NOT NULL,
        Subject                 nvarchar(50)    NOT NULL,
        SubjectType             nvarchar(10)    NOT NULL,
        DateIssued              datetime        NOT NULL,
        DateExpired             datetime        NOT NULL,
        RevokedAt               datetime        NULL,
        ParentRefreshTokenID    nvarchar(50)    NULL,
        CONSTRAINT PK_tblT_AuthToken PRIMARY KEY CLUSTERED (TokenID)
    );
END
GO

-- TokenHash is the only column the per-request verifier hits. Unique
-- non-clustered index gives O(log n) exact-match lookup and a uniqueness guard
-- against the (cryptographically infeasible) SHA-256 collision case.
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UIX_tblT_AuthToken_TokenHash' AND object_id = OBJECT_ID('tblT_AuthToken'))
BEGIN
    CREATE UNIQUE NONCLUSTERED INDEX UIX_tblT_AuthToken_TokenHash
        ON tblT_AuthToken (TokenHash);
END
GO

-- Subject + SubjectType supports revokeAllForSubject (ban / password-change
-- propagation) and any future per-user diagnostic queries.
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_tblT_AuthToken_Subject' AND object_id = OBJECT_ID('tblT_AuthToken'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_tblT_AuthToken_Subject
        ON tblT_AuthToken (Subject, SubjectType);
END
GO

-- DateExpired supports an hourly purge worker
-- (DELETE WHERE DateExpired < DATEADD(hour, -24, GETDATE())).
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_tblT_AuthToken_DateExpired' AND object_id = OBJECT_ID('tblT_AuthToken'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_tblT_AuthToken_DateExpired
        ON tblT_AuthToken (DateExpired);
END
GO

-- ParentRefreshTokenID lets the chain-revoke find every access token minted
-- under a given refresh-token chain in one indexed sweep.
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_tblT_AuthToken_ParentRefreshTokenID' AND object_id = OBJECT_ID('tblT_AuthToken'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_tblT_AuthToken_ParentRefreshTokenID
        ON tblT_AuthToken (ParentRefreshTokenID);
END
GO


IF NOT EXISTS (SELECT 1 FROM sys.objects WHERE type = 'U' AND name = 'tblT_RefreshToken')
BEGIN
    CREATE TABLE tblT_RefreshToken
    (
        RefreshTokenID          nvarchar(50)    NOT NULL,
        TokenHash               char(64)        NOT NULL,
        Subject                 nvarchar(50)    NOT NULL,
        SubjectType             nvarchar(10)    NOT NULL,
        DateIssued              datetime        NOT NULL,
        DateExpired             datetime        NOT NULL,
        RevokedAt               datetime        NULL,
        ReplacedByID            nvarchar(50)    NULL,
        RotationChainRoot       nvarchar(50)    NOT NULL,
        CONSTRAINT PK_tblT_RefreshToken PRIMARY KEY CLUSTERED (RefreshTokenID)
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UIX_tblT_RefreshToken_TokenHash' AND object_id = OBJECT_ID('tblT_RefreshToken'))
BEGIN
    CREATE UNIQUE NONCLUSTERED INDEX UIX_tblT_RefreshToken_TokenHash
        ON tblT_RefreshToken (TokenHash);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_tblT_RefreshToken_Subject' AND object_id = OBJECT_ID('tblT_RefreshToken'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_tblT_RefreshToken_Subject
        ON tblT_RefreshToken (Subject, SubjectType);
END
GO

-- RotationChainRoot supports chain-revoke on reuse detection
-- (UPDATE tblT_RefreshToken SET RevokedAt = ... WHERE RotationChainRoot = X)
-- and explicit logout-time revoke.
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_tblT_RefreshToken_RotationChainRoot' AND object_id = OBJECT_ID('tblT_RefreshToken'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_tblT_RefreshToken_RotationChainRoot
        ON tblT_RefreshToken (RotationChainRoot);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_tblT_RefreshToken_DateExpired' AND object_id = OBJECT_ID('tblT_RefreshToken'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_tblT_RefreshToken_DateExpired
        ON tblT_RefreshToken (DateExpired);
END
GO


IF NOT EXISTS (SELECT 1 FROM sys.objects WHERE type = 'U' AND name = 'tblH_Auth_Log')
BEGIN
    CREATE TABLE tblH_Auth_Log
    (
        LogID                   nvarchar(50)    NOT NULL,
        EventType               nvarchar(50)    NOT NULL,
        Subject                 nvarchar(50)    NULL,
        SubjectType             nvarchar(10)    NULL,
        RefreshTokenID          nvarchar(50)    NULL,
        RotationChainRoot       nvarchar(50)    NULL,
        SourceIP                nvarchar(45)    NULL,
        Details                 nvarchar(max)   NULL,
        DateLogged              datetime        NOT NULL,
        CONSTRAINT PK_tblH_Auth_Log PRIMARY KEY CLUSTERED (LogID)
    );
END
GO

-- EventType + DateLogged supports the alerting query pattern
-- (SELECT ... WHERE EventType = 'refresh_token_reuse' AND DateLogged > ?).
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_tblH_Auth_Log_EventType_Date' AND object_id = OBJECT_ID('tblH_Auth_Log'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_tblH_Auth_Log_EventType_Date
        ON tblH_Auth_Log (EventType, DateLogged);
END
GO

-- Subject lookup for per-user audit trails.
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_tblH_Auth_Log_Subject' AND object_id = OBJECT_ID('tblH_Auth_Log'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_tblH_Auth_Log_Subject
        ON tblH_Auth_Log (Subject, SubjectType);
END
GO
