using Newtonsoft.Json;
using QryptoCard.INT.Model.Service;
using QryptoCard.INT.Script.Service;
using QryptoCard.Sec;
using System;
using System.Data.Entity;
using System.Diagnostics;
using System.Linq;

namespace QryptoCard.INT.Script.Service.Auth.v1
{
    // Auth-token WCF service (opaque-random Bearer tokens, Runegate parity).
    //
    // Token lifecycle:
    //   mintAfterOtpVerify  — verify a single-use OTP session row, then mint an
    //                         (access, refresh) pair. No mint() without OTP.
    //   refresh             — rotate the refresh token (RTR) with reuse detection
    //                         and atomic single-winner consume.
    //   verify              — per-request indexed lookup on the access-token hash.
    //   revoke              — logout; revokes the whole rotation chain (idempotent).
    //   revokeAllForSubject — ban / password-change; revokes every chain for a
    //                         subject, gated by a constant-time service-token check.
    //
    // Crypto (token generation + SHA-256 hashing) goes through QryptoCard.Sec.AuthTokens.
    // OTP codes are stored as OtpCodes.Hash(plain) on tblH_User_Login / tblH_Admin_Login.
    public class AuthV1Service : IAuthV1Service
    {
        // TTL constants — code constants, not env-overridable. Changing them
        // requires a code change + deploy, the intended ceremony for a
        // security-critical tunable on a financial app.
        public static readonly TimeSpan AccessTokenTtl  = AuthTokens.AccessLifetime;
        public static readonly TimeSpan RefreshTokenTtl = AuthTokens.RefreshLifetime;

        public const string SubjectTypeUser  = "user";
        public const string SubjectTypeAdmin = "admin";

        public const string EventTypeRefreshTokenReuse         = "refresh_token_reuse";
        public const string EventTypeRefreshTokenConcurrentUse = "refresh_token_concurrent_use";
        public const string EventTypeLogout                    = "logout";
        public const string EventTypeSubjectRevoke             = "subject_revoke";
        // Log wrong-service-token attempts so mass-token-guess campaigns leave a
        // trail. Without this, an attacker probing N candidate tokens against
        // revokeAllForSubject only generates an audit row for the (at most 1)
        // successful guess — N-1 failures would be invisible.
        public const string EventTypeRevokeAuthFailure         = "revoke_token_auth_failure";

        // Shared-secret env var read from SecretsConfig. Required at app-pool
        // start (Preload) — a missing value fails the process before the first
        // WCF call.
        public const string ServiceRevokeTokenSecretName = "AUTH_SERVICE_REVOKE_TOKEN";

        // Two contexts, both per-instance (matches existing WCF service pattern):
        //   - db: legacy DBEntities for tblM_User / tblM_Admin / tblH_*_Login
        //     lookups during mintAfterOtpVerify and verify
        //   - authDb: separate code-first AuthDbContext for token writes/reads
        // No cross-context joins (Subject is a value-typed pointer, not an FK).
        DBEntities db;
        AuthDbContext authDb;

        OutputModel op = new OutputModel();

        // Production ctor — field-initialise both contexts from config, matching
        // the existing UserV1Service / AdminV1Service pattern.
        public AuthV1Service()
        {
            db = new DBEntities();
            authDb = new AuthDbContext();
        }

        // Test ctor — point both contexts at an explicit connection. The legacy
        // DBEntities takes an EntityConnection string (metadata=res://*/DB.*...);
        // AuthDbContext (code-first) takes a plain SqlClient connection string.
        // Both target the same physical LocalDB database under test.
        public AuthV1Service(DBEntities legacyDb, AuthDbContext authContext)
        {
            db = legacyDb;
            authDb = authContext;
        }

        // ---------- mintAfterOtpVerify ----------

        // Requires that the caller has already completed the existing
        // password-then-OTP login flow:
        //   1. Login (email, password) writes a tblH_User_Login row with Code (the
        //      OTP hash) and isVerify=0, and sends the OTP to the user. Returns ID
        //      (otpSessionId).
        //   2. User receives the OTP code out-of-band.
        //   3. mintAfterOtpVerify(otpSessionId, otpCode, subjectType) — THIS method.
        //      Atomic check-and-set on isVerify, then mint.
        //
        // Failure paths uniformly return "failed" with "Invalid OTP code or session"
        // — no oracle on which precondition failed (unknown session, wrong code,
        // expired, already used).
        public OutputModel mintAfterOtpVerify(string otpSessionId, string otpCode, string subjectType)
        {
            try
            {
                if (string.IsNullOrEmpty(otpSessionId) || string.IsNullOrEmpty(otpCode) ||
                    string.IsNullOrEmpty(subjectType))
                {
                    op.Status = "failed";
                    op.Message = "Invalid OTP code or session";
                    return op;
                }

                if (subjectType != SubjectTypeUser && subjectType != SubjectTypeAdmin)
                {
                    op.Status = "failed";
                    op.Message = "Invalid OTP code or session";
                    return op;
                }

                var now = DateTime.UtcNow;

                // OTP codes are stored hashed (OtpCodes.Hash); compare against the
                // hash of the presented code, never the plaintext.
                var otpCodeHash = OtpCodes.Hash(otpCode);

                // Atomic check-and-set on isVerify. SQL serializes the UPDATE on the
                // same row, so two concurrent mintAfterOtpVerify calls with the same
                // (otpSessionId, otpCode) cannot both succeed. The loser sees
                // rowsAffected==0 and gets the same uniform failure as wrong-OTP /
                // expired-OTP / unknown-session paths.
                //
                // WHERE clause guards: (a) row exists, (b) Code matches the hash of
                // the presented code, (c) isVerify is still 0 (single-use),
                // (d) DateExpired hasn't passed.
                //
                // {0}/{1}/{2} are parameterized by ExecuteSqlCommand — safe against
                // SQL injection. otpSessionId / otpCodeHash are never concatenated
                // into the SQL string.
                int rowsAffected;
                string subjectId;

                if (subjectType == SubjectTypeUser)
                {
                    rowsAffected = db.Database.ExecuteSqlCommand(
                        "UPDATE tblH_User_Login SET isVerify = 1 " +
                        "WHERE ID = {0} AND Code = {1} AND isVerify = 0 AND DateExpired > {2}",
                        otpSessionId, otpCodeHash, now);
                    if (rowsAffected == 0)
                    {
                        op.Status = "failed";
                        op.Message = "Invalid OTP code or session";
                        return op;
                    }
                    var otpRow = db.tblH_User_Login.AsNoTracking().FirstOrDefault(p => p.ID == otpSessionId);
                    subjectId = otpRow == null ? null : otpRow.UserID;
                }
                else
                {
                    rowsAffected = db.Database.ExecuteSqlCommand(
                        "UPDATE tblH_Admin_Login SET isVerify = 1 " +
                        "WHERE ID = {0} AND Code = {1} AND isVerify = 0 AND DateExpired > {2}",
                        otpSessionId, otpCodeHash, now);
                    if (rowsAffected == 0)
                    {
                        op.Status = "failed";
                        op.Message = "Invalid OTP code or session";
                        return op;
                    }
                    var otpRow = db.tblH_Admin_Login.AsNoTracking().FirstOrDefault(p => p.ID == otpSessionId);
                    subjectId = otpRow == null ? null : otpRow.AdminID;
                }

                if (string.IsNullOrEmpty(subjectId))
                {
                    // Row updated by us but reread came back empty / missing FK
                    // column. Treat as failure — should not happen under normal
                    // conditions (row was just verified to exist).
                    op.Status = "failed";
                    op.Message = "Invalid OTP code or session";
                    return op;
                }

                // Status checks on the user/admin row. Account could have been
                // deactivated or banned between Login() and mintAfterOtpVerify().
                var status = LookupSubjectStatus(subjectType, subjectId);
                if (status == null)
                {
                    op.Status = "failed";
                    op.Message = "Invalid OTP code or session";
                    return op;
                }
                if (status.IsActive == 0)
                {
                    op.Status = "failed";
                    op.Message = "Your account is nonactive. Please call admin for more";
                    return op;
                }
                if (status.IsBanned == 1)
                {
                    op.Status = "failed";
                    op.Message = "Your account has been banned for some reason";
                    return op;
                }

                // Mint the pair. No bcrypt here because Login() already verified
                // the password and the OTP was just verified above.
                var accessPlaintext  = AuthTokens.NewAccessToken();
                var refreshPlaintext = AuthTokens.NewRefreshToken();

                var refreshRow = new tblT_RefreshToken
                {
                    RefreshTokenID    = Guid.NewGuid().ToString(),
                    TokenHash         = AuthTokens.Hash(refreshPlaintext),
                    Subject           = subjectId,
                    SubjectType       = subjectType,
                    DateIssued        = now,
                    DateExpired       = now + RefreshTokenTtl,
                    RevokedAt         = null,
                    ReplacedByID      = null,
                    RotationChainRoot = null  // set to self below
                };
                refreshRow.RotationChainRoot = refreshRow.RefreshTokenID;

                var accessRow = new tblT_AuthToken
                {
                    TokenID              = Guid.NewGuid().ToString(),
                    TokenHash            = AuthTokens.Hash(accessPlaintext),
                    Subject              = subjectId,
                    SubjectType          = subjectType,
                    DateIssued           = now,
                    DateExpired          = now + AccessTokenTtl,
                    RevokedAt            = null,
                    ParentRefreshTokenID = refreshRow.RefreshTokenID
                };

                using (var tx = authDb.Database.BeginTransaction())
                {
                    authDb.tblT_RefreshToken.Add(refreshRow);
                    authDb.tblT_AuthToken.Add(accessRow);
                    authDb.SaveChanges();
                    tx.Commit();
                }

                op.Status  = "success";
                op.Message = "ok";
                // Serialize to JSON string — op.Data is typed as object. WCF
                // DataContractSerializer cannot serialize an unknown concrete type
                // in an object field without KnownType; every other service in this
                // codebase uses the same JSON-string-in-Data pattern.
                op.Data = JsonConvert.SerializeObject(new AuthMintResponse
                {
                    AccessToken         = accessPlaintext,
                    AccessTokenExpires  = accessRow.DateExpired,
                    RefreshToken        = refreshPlaintext,
                    RefreshTokenExpires = refreshRow.DateExpired,
                    Subject             = subjectId,
                    SubjectType         = subjectType
                }, Formatting.None);
            }
            catch (Exception ex)
            {
                // Do NOT surface ex.Message to the wire — SqlException leaks schema
                // details. Server-side log retains debuggability.
                LogError("mintAfterOtpVerify", ex);
                op.Status  = "failed";
                op.Message = "Invalid OTP code or session";
            }
            return op;
        }

        // ---------- refresh ----------

        public OutputModel refresh(string refreshToken)
        {
            try
            {
                if (string.IsNullOrEmpty(refreshToken))
                {
                    op.Status = "failed";
                    op.Message = "Invalid refresh token";
                    return op;
                }

                var hash = AuthTokens.Hash(refreshToken);
                // AsNoTracking: refresh() never mutates this row via EF (the rotation
                // is performed via a raw conditional UPDATE below to atomically guard
                // ReplacedByID IS NULL against concurrent callers).
                var row = authDb.tblT_RefreshToken.AsNoTracking().FirstOrDefault(p => p.TokenHash == hash);
                var now = DateTime.UtcNow;

                if (row == null || row.DateExpired < now || row.RevokedAt != null)
                {
                    op.Status = "failed";
                    op.Message = "Invalid refresh token";
                    return op;
                }

                // Reuse detection: this row has already been rotated. Treat as a
                // compromise signal: revoke the entire chain and log for alerting.
                if (!string.IsNullOrEmpty(row.ReplacedByID))
                {
                    RevokeChainWithAuditLog(row.RotationChainRoot, EventTypeRefreshTokenReuse,
                                            row.RefreshTokenID, row.Subject, row.SubjectType);
                    op.Status = "failed";
                    op.Message = "Invalid refresh token";
                    return op;
                }

                // Mint the new pair, link the rotation chain, mark consumed atomically.
                var accessPlaintext  = AuthTokens.NewAccessToken();
                var refreshPlaintext = AuthTokens.NewRefreshToken();

                var newRefresh = new tblT_RefreshToken
                {
                    RefreshTokenID    = Guid.NewGuid().ToString(),
                    TokenHash         = AuthTokens.Hash(refreshPlaintext),
                    Subject           = row.Subject,
                    SubjectType       = row.SubjectType,
                    DateIssued        = now,
                    DateExpired       = now + RefreshTokenTtl,
                    RevokedAt         = null,
                    ReplacedByID      = null,
                    RotationChainRoot = row.RotationChainRoot
                };

                var newAccess = new tblT_AuthToken
                {
                    TokenID              = Guid.NewGuid().ToString(),
                    TokenHash            = AuthTokens.Hash(accessPlaintext),
                    Subject              = row.Subject,
                    SubjectType          = row.SubjectType,
                    DateIssued           = now,
                    DateExpired          = now + AccessTokenTtl,
                    RevokedAt            = null,
                    ParentRefreshTokenID = newRefresh.RefreshTokenID
                };

                // Atomic rotation. Two concurrent refresh() calls with the same
                // valid refresh token MUST NOT both succeed. SQL serializes the
                // UPDATE on the same row; only one transaction sees rowsAffected==1.
                bool raceLost = false;
                using (var tx = authDb.Database.BeginTransaction())
                {
                    var rowsAffected = authDb.Database.ExecuteSqlCommand(
                        "UPDATE tblT_RefreshToken SET ReplacedByID = {0} " +
                        "WHERE RefreshTokenID = {1} AND ReplacedByID IS NULL",
                        newRefresh.RefreshTokenID, row.RefreshTokenID);

                    if (rowsAffected == 0)
                    {
                        tx.Rollback();
                        raceLost = true;
                    }
                    else
                    {
                        authDb.tblT_RefreshToken.Add(newRefresh);
                        authDb.tblT_AuthToken.Add(newAccess);
                        authDb.SaveChanges();
                        tx.Commit();
                    }
                }

                if (raceLost)
                {
                    // Concurrent refresh detected — another transaction won the race.
                    // RevokeChainWithAuditLog opens its OWN transaction, so it must be
                    // called AFTER the outer using-block has disposed (you cannot nest
                    // BeginTransaction calls on the same DbConnection in EF6).
                    RevokeChainWithAuditLog(row.RotationChainRoot,
                        EventTypeRefreshTokenConcurrentUse,
                        row.RefreshTokenID, row.Subject, row.SubjectType);
                    op.Status = "failed";
                    op.Message = "Invalid refresh token";
                    return op;
                }

                op.Status  = "success";
                op.Message = "ok";
                op.Data = JsonConvert.SerializeObject(new AuthMintResponse
                {
                    AccessToken         = accessPlaintext,
                    AccessTokenExpires  = newAccess.DateExpired,
                    RefreshToken        = refreshPlaintext,
                    RefreshTokenExpires = newRefresh.DateExpired,
                    Subject             = row.Subject,
                    SubjectType         = row.SubjectType
                }, Formatting.None);
            }
            catch (Exception ex)
            {
                LogError("refresh", ex);
                op.Status  = "failed";
                op.Message = "Invalid refresh token";
            }
            return op;
        }

        // ---------- verify ----------

        public OutputModel verify(string accessToken)
        {
            // Per-request hot path. Returns OutputModel with Data = AuthVerifyResponse
            // for wire-shape symmetry with the rest of the WCF surface.
            try
            {
                if (string.IsNullOrEmpty(accessToken))
                {
                    op.Status = "success";
                    op.Data = JsonConvert.SerializeObject(new AuthVerifyResponse { Valid = false }, Formatting.None);
                    return op;
                }

                var hash = AuthTokens.Hash(accessToken);
                var row = authDb.tblT_AuthToken.AsNoTracking().FirstOrDefault(p => p.TokenHash == hash);
                var now = DateTime.UtcNow;

                if (row == null || row.DateExpired < now || row.RevokedAt != null)
                {
                    op.Status = "success";
                    op.Data = JsonConvert.SerializeObject(new AuthVerifyResponse { Valid = false }, Formatting.None);
                    return op;
                }

                // Also resolve the subject's email so the Bearer attribute can stash
                // it for controllers' getEmail() helpers — avoids an extra WCF
                // round-trip per authenticated request. Indexed PK lookup.
                string email = LookupEmailForSubject(row.SubjectType, row.Subject);

                op.Status = "success";
                op.Data = JsonConvert.SerializeObject(new AuthVerifyResponse
                {
                    Valid       = true,
                    Subject     = row.Subject,
                    SubjectType = row.SubjectType,
                    ExpiresAt   = row.DateExpired,
                    Email       = email
                }, Formatting.None);
            }
            catch (Exception ex)
            {
                // Fail closed: caller treats as Valid=false. Don't leak the
                // exception message to the wire.
                LogError("verify", ex);
                op.Status = "success";
                op.Data = JsonConvert.SerializeObject(new AuthVerifyResponse { Valid = false }, Formatting.None);
                op.Message = "verify failed";
            }
            return op;
        }

        // Returns email (or null on miss) for a verified (Subject, SubjectType)
        // pair. Indexed lookup on the PK column.
        string LookupEmailForSubject(string subjectType, string subjectId)
        {
            try
            {
                if (subjectType == SubjectTypeUser)
                {
                    return db.tblM_User.AsNoTracking()
                        .Where(p => p.UserID == subjectId)
                        .Select(p => p.Email)
                        .FirstOrDefault();
                }
                if (subjectType == SubjectTypeAdmin)
                {
                    return db.tblM_Admin.AsNoTracking()
                        .Where(p => p.AdminID == subjectId)
                        .Select(p => p.Email)
                        .FirstOrDefault();
                }
                return null;
            }
            catch
            {
                // Verify is the per-request hot path. Don't let a transient DB blip
                // on the email lookup take down the entire auth path — token
                // validity is what matters. Caller treats null email as "subject
                // not resolvable to email" and proceeds.
                return null;
            }
        }

        // ---------- revoke ----------

        // Logout. Idempotent — re-revoking an already-revoked chain is a no-op.
        // Writes a tblH_Auth_Log "logout" event so legitimate logouts are visible
        // (paired with subject_revoke audit for ban / password-change to make
        // mass-revoke abuse detectable).
        public OutputModel revoke(string refreshToken)
        {
            try
            {
                if (string.IsNullOrEmpty(refreshToken))
                {
                    // Idempotent: nothing to revoke.
                    op.Status = "success";
                    op.Message = "ok";
                    return op;
                }

                var hash = AuthTokens.Hash(refreshToken);
                // AsNoTracking: only need RotationChainRoot value; chain-revoke
                // re-queries with tracking for the actual updates.
                var row = authDb.tblT_RefreshToken.AsNoTracking().FirstOrDefault(p => p.TokenHash == hash);

                if (row != null)
                {
                    // Only emit the audit event if there was actually a chain to
                    // revoke — bogus / stale tokens silently no-op (avoids
                    // attacker-controlled audit-log spam by replaying expired
                    // tokens).
                    RevokeChainWithAuditLog(row.RotationChainRoot, EventTypeLogout,
                                            row.RefreshTokenID, row.Subject, row.SubjectType);
                }

                op.Status  = "success";
                op.Message = "ok";
            }
            catch (Exception ex)
            {
                // Idempotent semantics — return success even on internal error so
                // the dashboard can clear its session unconditionally. Server-side
                // log retains debuggability.
                LogError("revoke", ex);
                op.Status  = "success";
                op.Message = "ok";
            }
            return op;
        }

        // ---------- revokeAllForSubject ----------

        // Requires a serviceToken parameter compared constant-time against
        // SecretsConfig "AUTH_SERVICE_REVOKE_TOKEN". Without the secret, any
        // localhost-reachable caller could iterate known UserIDs/AdminIDs and
        // mass-logout every user. Writes a tblH_Auth_Log "subject_revoke" event so
        // admin-initiated bans + password-change cascades are visible, AND so any
        // abuse of this endpoint leaves a trail.
        public OutputModel revokeAllForSubject(string subject, string subjectType, string serviceToken)
        {
            try
            {
                // Auth check FIRST — fail closed before doing any work or leaking
                // timing information about whether the subject exists.
                var expectedToken = SecretsConfig.Require(ServiceRevokeTokenSecretName);
                if (!ConstantTimeEqual(expectedToken, serviceToken))
                {
                    // Log the auth-failure attempt so mass-token-guess campaigns are
                    // detectable. Best-effort — wrapped so an audit-write failure
                    // can't escape this branch and reveal a different exception path
                    // from the success-path failure shape.
                    try
                    {
                        WriteAuditLog(EventTypeRevokeAuthFailure, subject, subjectType,
                                      refreshTokenId: null, rotationChainRoot: null,
                                      details: "{\"reason\":\"service_token_mismatch\"}");
                    }
                    catch (Exception logEx)
                    {
                        LogError("revokeAllForSubject.AuthFailureLog", logEx);
                    }
                    // Same uniform failure as missing-args path. Don't leak whether
                    // the token was wrong vs subject didn't exist.
                    op.Status  = "failed";
                    op.Message = "unauthorized";
                    return op;
                }

                if (string.IsNullOrEmpty(subject) || string.IsNullOrEmpty(subjectType))
                {
                    op.Status  = "success";
                    op.Message = "ok";
                    return op;
                }

                // Write the audit row inline inside the same tx as the revokes so
                // either both happen or neither does (mirrors the
                // RevokeChainWithAuditLog pattern — audit inside tx).
                var now = DateTime.UtcNow;
                using (var tx = authDb.Database.BeginTransaction())
                {
                    var refreshRows = authDb.tblT_RefreshToken
                        .Where(p => p.Subject == subject && p.SubjectType == subjectType && p.RevokedAt == null)
                        .ToList();
                    foreach (var r in refreshRows) r.RevokedAt = now;
                    var refreshCount = refreshRows.Count;

                    var accessRows = authDb.tblT_AuthToken
                        .Where(p => p.Subject == subject && p.SubjectType == subjectType && p.RevokedAt == null)
                        .ToList();
                    foreach (var a in accessRows) a.RevokedAt = now;
                    var accessCount = accessRows.Count;

                    // Always log — even on no-op (subject had no active tokens) so
                    // probe-and-enumerate attempts via this (now auth'd) endpoint
                    // leave a trail. Details captures row counts.
                    authDb.tblH_Auth_Log.Add(new tblH_Auth_Log
                    {
                        LogID             = Guid.NewGuid().ToString(),
                        EventType         = EventTypeSubjectRevoke,
                        Subject           = TruncateForAuditSubject(subject),
                        SubjectType       = TruncateForAuditSubjectType(subjectType),
                        RefreshTokenID    = null,
                        RotationChainRoot = null,
                        SourceIP          = WcfSourceIp.TryGet(),
                        Details           = "{\"refreshRevoked\":" + refreshCount +
                                            ",\"accessRevoked\":" + accessCount + "}",
                        DateLogged        = now
                    });

                    authDb.SaveChanges();
                    tx.Commit();
                }

                op.Status  = "success";
                op.Message = "ok";
            }
            catch (Exception ex)
            {
                LogError("revokeAllForSubject", ex);
                op.Status  = "failed";
                op.Message = "unauthorized";
            }
            return op;
        }

        // ---------- helpers (private) ----------

        // Constant-time string comparison for the service-token check. Length
        // comparison is a side-channel but acceptable since the configured token
        // has a fixed deploy-time length. Per-character XOR-OR ensures the function
        // does not short-circuit on the first mismatching position.
        static bool ConstantTimeEqual(string expected, string actual)
        {
            if (expected == null || actual == null) return false;
            if (expected.Length != actual.Length) return false;
            int diff = 0;
            for (int i = 0; i < expected.Length; i++) diff |= expected[i] ^ actual[i];
            return diff == 0;
        }

        // Server-side log path. Trace.TraceError lands in the WCF service host's
        // standard trace listener. Caller has redacted to a fixed user-facing
        // string; this preserves debuggability without leaking.
        static void LogError(string method, Exception ex)
        {
            Trace.TraceError("AuthV1Service." + method + ": " + ex);
        }

        // Subject status (active / banned). No password — mintAfterOtpVerify
        // doesn't need to bcrypt because Login() already did. Status checks matter
        // because the user / admin could have been deactivated or banned between
        // Login() and mintAfterOtpVerify().
        class SubjectStatus
        {
            public int? IsActive;
            public int? IsBanned;
        }

        SubjectStatus LookupSubjectStatus(string subjectType, string subjectId)
        {
            if (subjectType == SubjectTypeUser)
            {
                var u = db.tblM_User.AsNoTracking().FirstOrDefault(p => p.UserID == subjectId);
                if (u == null) return null;
                return new SubjectStatus { IsActive = u.isActive, IsBanned = u.isBanned };
            }
            if (subjectType == SubjectTypeAdmin)
            {
                var a = db.tblM_Admin.AsNoTracking().FirstOrDefault(p => p.AdminID == subjectId);
                if (a == null) return null;
                return new SubjectStatus { IsActive = a.isActive, IsBanned = a.isBanned };
            }
            return null;
        }

        // Sets RevokedAt = now on every refresh token sharing this RotationChainRoot,
        // and on every access token whose ParentRefreshTokenID is in that set, AND
        // writes a tblH_Auth_Log row tagged with the supplied EventType. Used by
        // refresh()'s reuse-detection branch, refresh()'s race-loser branch, and
        // revoke() for explicit logout.
        void RevokeChainWithAuditLog(string rotationChainRoot, string eventType,
                                     string triggeringRefreshTokenID, string subject, string subjectType)
        {
            var now = DateTime.UtcNow;
            using (var tx = authDb.Database.BeginTransaction())
            {
                var refreshRows = authDb.tblT_RefreshToken
                    .Where(p => p.RotationChainRoot == rotationChainRoot && p.RevokedAt == null)
                    .ToList();

                var refreshIds = refreshRows.Select(r => r.RefreshTokenID).ToList();
                var accessRows = authDb.tblT_AuthToken
                    .Where(p => refreshIds.Contains(p.ParentRefreshTokenID) && p.RevokedAt == null)
                    .ToList();

                foreach (var r in refreshRows) r.RevokedAt = now;
                foreach (var a in accessRows) a.RevokedAt = now;

                authDb.tblH_Auth_Log.Add(new tblH_Auth_Log
                {
                    LogID             = Guid.NewGuid().ToString(),
                    EventType         = eventType,
                    Subject           = subject,
                    SubjectType       = subjectType,
                    RefreshTokenID    = triggeringRefreshTokenID,
                    RotationChainRoot = rotationChainRoot,
                    SourceIP          = WcfSourceIp.TryGet(),
                    Details           = null,
                    DateLogged        = now
                });

                authDb.SaveChanges();
                tx.Commit();
            }
        }

        // Standalone audit-log write — used by the auth-failure path of
        // revokeAllForSubject() (outside any caller-managed transaction).
        // Truncates Subject + SubjectType to column lengths so attacker-controlled
        // overlong inputs cannot trip a SaveChanges validation exception that would
        // defeat the audit goal.
        void WriteAuditLog(string eventType, string subject, string subjectType,
                           string refreshTokenId, string rotationChainRoot, string details)
        {
            var row = new tblH_Auth_Log
            {
                LogID             = Guid.NewGuid().ToString(),
                EventType         = eventType,
                Subject           = TruncateForAuditSubject(subject),
                SubjectType       = TruncateForAuditSubjectType(subjectType),
                RefreshTokenID    = refreshTokenId,
                RotationChainRoot = rotationChainRoot,
                SourceIP          = WcfSourceIp.TryGet(),
                Details           = details,
                DateLogged        = DateTime.UtcNow
            };
            authDb.tblH_Auth_Log.Add(row);
            authDb.SaveChanges();
        }

        // Subject column on tblH_Auth_Log is nvarchar(50). Untrusted callers
        // (auth-failure path) can submit overlong values; truncate so the audit
        // row writes cleanly. Production subject IDs (UserID/AdminID) are well
        // under 50 chars — truncation only ever affects probes.
        const int AuditSubjectMaxLen     = 50;
        const int AuditSubjectTypeMaxLen = 10;
        static string TruncateForAuditSubject(string s) =>
            s == null ? null : (s.Length <= AuditSubjectMaxLen ? s : s.Substring(0, AuditSubjectMaxLen));
        static string TruncateForAuditSubjectType(string s) =>
            s == null ? null : (s.Length <= AuditSubjectTypeMaxLen ? s : s.Substring(0, AuditSubjectTypeMaxLen));
    }
}
