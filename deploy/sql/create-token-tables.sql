-- Plan 4 Slice 2 — bearer-token store.
-- Two additive, relationship-free tables for opaque access/refresh tokens. Idempotent; no
-- backfill. Applied during the dev shakeout / launch (the token CODE is built and unit-tested
-- ahead of this; these tables are the only DB-gated piece). Tokens are stored ONLY as SHA-256
-- hashes (64 hex chars). SubjectType is 'user' or 'admin'.

IF OBJECT_ID(N'dbo.tblT_AuthToken', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.tblT_AuthToken
    (
        Id          BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_tblT_AuthToken PRIMARY KEY,
        TokenHash   NVARCHAR(64)  NOT NULL,
        SubjectId   NVARCHAR(64)  NOT NULL,
        SubjectType NVARCHAR(10)  NOT NULL,
        CreatedAt   DATETIME2(3)  NOT NULL,
        ExpiresAt   DATETIME2(3)  NULL,
        RevokedAt   DATETIME2(3)  NULL
    );
    CREATE UNIQUE INDEX UX_tblT_AuthToken_TokenHash ON dbo.tblT_AuthToken (TokenHash);
END;

IF OBJECT_ID(N'dbo.tblT_RefreshToken', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.tblT_RefreshToken
    (
        Id          BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_tblT_RefreshToken PRIMARY KEY,
        TokenHash   NVARCHAR(64)  NOT NULL,
        SubjectId   NVARCHAR(64)  NOT NULL,
        SubjectType NVARCHAR(10)  NOT NULL,
        CreatedAt   DATETIME2(3)  NOT NULL,
        ExpiresAt   DATETIME2(3)  NULL,
        RevokedAt   DATETIME2(3)  NULL
    );
    CREATE UNIQUE INDEX UX_tblT_RefreshToken_TokenHash ON dbo.tblT_RefreshToken (TokenHash);
END;
