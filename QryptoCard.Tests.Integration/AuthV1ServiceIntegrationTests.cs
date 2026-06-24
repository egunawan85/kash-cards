using System;
using System.Linq;
using Newtonsoft.Json;
using QryptoCard.INT;
using QryptoCard.INT.Model.Service;
using QryptoCard.INT.Script.Service.Auth.v1;
using QryptoCard.Sec;
using QryptoCard.Tests.Fixtures.LocalDb;
using Xunit;

namespace QryptoCard.Tests.Integration
{
    // End-to-end proof of the opaque-Bearer-token security model against a real
    // throwaway LocalDB. Drives the real AuthV1Service (instantiated directly via
    // its test ctor) and asserts the database effect. Covers: mint, verify (valid /
    // expired / revoked / cross-tier), refresh rotation, refresh-reuse detection
    // with chain-revoke + audit row, revoke, and revokeAllForSubject.
    public class AuthV1ServiceIntegrationTests : IClassFixture<LocalDbFixture>
    {
        readonly LocalDbFixture _db;

        // The service-token secret revokeAllForSubject gates on. Set into the
        // environment + SecretsConfig cache before any AuthV1Service is built so
        // the constant-time check has a known value to compare against.
        const string ServiceRevokeToken = "test-service-revoke-token-do-not-use";

        public AuthV1ServiceIntegrationTests(LocalDbFixture db)
        {
            _db = db;
            Environment.SetEnvironmentVariable(
                AuthV1Service.ServiceRevokeTokenSecretName, ServiceRevokeToken);
            // SecretsConfig caches per process; clear so the value above is the one
            // Require() reads regardless of prior test ordering.
            SecretsConfig.ResetCacheForTests();
        }

        // ----- helpers -----

        // Build an AuthV1Service wired to the fixture's LocalDB: legacy DBEntities
        // via the EntityConnection metadata format, AuthDbContext via the plain
        // SqlClient connection string. Both target the same physical database.
        AuthV1Service NewService()
        {
            return new AuthV1Service(_db.NewContext(), NewAuthContext());
        }

        AuthDbContext NewAuthContext() => new AuthDbContext(_db.ConnectionString);

        // Seed a verified-pending OTP session for the baseline user and return its
        // ID. mintAfterOtpVerify consumes it (isVerify 0 -> 1) when the plaintext
        // code hashes to the stored Code.
        string SeedUserOtpSession(string plainCode, DateTime? expires = null)
        {
            string id = Guid.NewGuid().ToString();
            using (var ctx = _db.NewContext())
            {
                ctx.tblH_User_Login.Add(new tblH_User_Login
                {
                    ID = id,
                    UserID = LocalDbFixture.Ids.UserA,
                    Code = OtpCodes.Hash(plainCode),
                    DateCreated = DateTime.UtcNow,
                    DateExpired = expires ?? DateTime.UtcNow.AddMinutes(15),
                    isVerify = 0
                });
                ctx.SaveChanges();
            }
            return id;
        }

        static AuthMintResponse Mint(OutputModel op)
            => JsonConvert.DeserializeObject<AuthMintResponse>((string)op.Data);

        static AuthVerifyResponse Verify(OutputModel op)
            => JsonConvert.DeserializeObject<AuthVerifyResponse>((string)op.Data);

        // ----- mint -----

        [Fact]
        public void MintAfterOtpVerify_ValidOtpSession_IssuesValidPairAndConsumesSession()
        {
            const string code = "246810";
            string otpId = SeedUserOtpSession(code);

            var op = NewService().mintAfterOtpVerify(otpId, code, AuthV1Service.SubjectTypeUser);

            Assert.Equal("success", op.Status);
            var pair = Mint(op);
            Assert.StartsWith(AuthTokens.AccessPrefix, pair.AccessToken);
            Assert.StartsWith(AuthTokens.RefreshPrefix, pair.RefreshToken);
            Assert.Equal(LocalDbFixture.Ids.UserA, pair.Subject);
            Assert.Equal(AuthV1Service.SubjectTypeUser, pair.SubjectType);

            // OTP session is consumed (single-use) and the token rows exist.
            using (var auth = NewAuthContext())
            using (var ctx = _db.NewContext())
            {
                Assert.Equal(1, ctx.tblH_User_Login.Single(p => p.ID == otpId).isVerify);
                string accessHash = AuthTokens.Hash(pair.AccessToken);
                string refreshHash = AuthTokens.Hash(pair.RefreshToken);
                Assert.NotNull(auth.tblT_AuthToken.SingleOrDefault(p => p.TokenHash == accessHash));
                var refreshRow = auth.tblT_RefreshToken.Single(p => p.TokenHash == refreshHash);
                // Root of a fresh chain self-references.
                Assert.Equal(refreshRow.RefreshTokenID, refreshRow.RotationChainRoot);
                Assert.Null(refreshRow.ReplacedByID);
            }

            // A reused OTP session no longer mints (isVerify already 1).
            var reuse = NewService().mintAfterOtpVerify(otpId, code, AuthV1Service.SubjectTypeUser);
            Assert.Equal("failed", reuse.Status);
        }

        [Fact]
        public void MintAfterOtpVerify_WrongCode_Fails()
        {
            string otpId = SeedUserOtpSession("111111");
            var op = NewService().mintAfterOtpVerify(otpId, "222222", AuthV1Service.SubjectTypeUser);
            Assert.Equal("failed", op.Status);
        }

        // ----- verify -----

        [Fact]
        public void Verify_ValidAccessToken_ReturnsValidWithSubjectAndEmail()
        {
            const string code = "314159";
            string otpId = SeedUserOtpSession(code);
            var mintOp = NewService().mintAfterOtpVerify(otpId, code, AuthV1Service.SubjectTypeUser);
            var pair = Mint(mintOp);

            var op = NewService().verify(pair.AccessToken);
            Assert.Equal("success", op.Status);
            var v = Verify(op);
            Assert.True(v.Valid);
            Assert.Equal(LocalDbFixture.Ids.UserA, v.Subject);
            Assert.Equal(AuthV1Service.SubjectTypeUser, v.SubjectType);
            Assert.Equal(LocalDbFixture.Ids.EmailA, v.Email);
        }

        [Fact]
        public void Verify_ExpiredAccessToken_ReturnsInvalid()
        {
            // Insert an already-expired access token row directly.
            string raw = AuthTokens.NewAccessToken();
            using (var auth = NewAuthContext())
            {
                auth.tblT_AuthToken.Add(new tblT_AuthToken
                {
                    TokenID = Guid.NewGuid().ToString(),
                    TokenHash = AuthTokens.Hash(raw),
                    Subject = LocalDbFixture.Ids.UserA,
                    SubjectType = AuthV1Service.SubjectTypeUser,
                    DateIssued = DateTime.UtcNow.AddHours(-2),
                    DateExpired = DateTime.UtcNow.AddHours(-1),
                    RevokedAt = null,
                    ParentRefreshTokenID = null
                });
                auth.SaveChanges();
            }

            var v = Verify(NewService().verify(raw));
            Assert.False(v.Valid);
        }

        [Fact]
        public void Verify_RevokedAccessToken_ReturnsInvalid()
        {
            string raw = AuthTokens.NewAccessToken();
            using (var auth = NewAuthContext())
            {
                auth.tblT_AuthToken.Add(new tblT_AuthToken
                {
                    TokenID = Guid.NewGuid().ToString(),
                    TokenHash = AuthTokens.Hash(raw),
                    Subject = LocalDbFixture.Ids.UserA,
                    SubjectType = AuthV1Service.SubjectTypeUser,
                    DateIssued = DateTime.UtcNow.AddMinutes(-1),
                    DateExpired = DateTime.UtcNow.AddMinutes(14),
                    RevokedAt = DateTime.UtcNow,
                    ParentRefreshTokenID = null
                });
                auth.SaveChanges();
            }

            var v = Verify(NewService().verify(raw));
            Assert.False(v.Valid);
        }

        [Fact]
        public void Verify_CrossTier_UserTokenCarriesUserType_NotAdmin()
        {
            // A user-minted access token verifies as SubjectType "user" — a caller
            // gating an admin route on SubjectType == "admin" rejects it. The
            // SubjectType is bound at mint and surfaced (not coerceable) at verify.
            const string code = "271828";
            string otpId = SeedUserOtpSession(code);
            var pair = Mint(NewService().mintAfterOtpVerify(otpId, code, AuthV1Service.SubjectTypeUser));

            var v = Verify(NewService().verify(pair.AccessToken));
            Assert.True(v.Valid);
            Assert.Equal(AuthV1Service.SubjectTypeUser, v.SubjectType);
            Assert.NotEqual(AuthV1Service.SubjectTypeAdmin, v.SubjectType);
        }

        // ----- refresh -----

        [Fact]
        public void Refresh_ValidRefreshToken_RotatesOldConsumedNewWorks()
        {
            const string code = "161803";
            string otpId = SeedUserOtpSession(code);
            var first = Mint(NewService().mintAfterOtpVerify(otpId, code, AuthV1Service.SubjectTypeUser));

            var refreshOp = NewService().refresh(first.RefreshToken);
            Assert.Equal("success", refreshOp.Status);
            var second = Mint(refreshOp);

            // New access token verifies, and the new refresh token rotates again —
            // proving the freshly minted pair is fully usable.
            Assert.True(Verify(NewService().verify(second.AccessToken)).Valid);
            var third = Mint(NewService().refresh(second.RefreshToken));
            Assert.True(Verify(NewService().verify(third.AccessToken)).Valid);

            // The chain is linked: original row's ReplacedByID points forward and
            // both share one RotationChainRoot.
            string firstHash = AuthTokens.Hash(first.RefreshToken);
            string secondHash = AuthTokens.Hash(second.RefreshToken);
            using (var auth = NewAuthContext())
            {
                var firstRow = auth.tblT_RefreshToken.Single(p => p.TokenHash == firstHash);
                var secondRow = auth.tblT_RefreshToken.Single(p => p.TokenHash == secondHash);
                Assert.Equal(secondRow.RefreshTokenID, firstRow.ReplacedByID);
                Assert.Equal(firstRow.RotationChainRoot, secondRow.RotationChainRoot);
            }

            // The original (already-rotated) refresh token is consumed: presenting
            // it again is rejected — single-use rotation holds.
            Assert.Equal("failed", NewService().refresh(first.RefreshToken).Status);
        }

        [Fact]
        public void Refresh_ReuseDetection_RevokesWholeChainAndWritesAuditRow()
        {
            const string code = "112358";
            string otpId = SeedUserOtpSession(code);
            var first = Mint(NewService().mintAfterOtpVerify(otpId, code, AuthV1Service.SubjectTypeUser));

            // Rotate once: first -> second (first now consumed).
            var second = Mint(NewService().refresh(first.RefreshToken));
            // And again so the chain has three live-ish refresh rows.
            var third = Mint(NewService().refresh(second.RefreshToken));

            string firstHash = AuthTokens.Hash(first.RefreshToken);
            string chainRoot;
            using (var auth = NewAuthContext())
            {
                chainRoot = auth.tblT_RefreshToken
                    .Single(p => p.TokenHash == firstHash).RotationChainRoot;
            }

            // Present the ALREADY-ROTATED first token again — classic refresh-token
            // reuse. The whole rotation chain must be revoked and an audit row
            // written.
            var reuse = NewService().refresh(first.RefreshToken);
            Assert.Equal("failed", reuse.Status);

            using (var auth = NewAuthContext())
            {
                // Every refresh row in the chain is revoked.
                var chainRows = auth.tblT_RefreshToken.Where(p => p.RotationChainRoot == chainRoot).ToList();
                Assert.NotEmpty(chainRows);
                Assert.All(chainRows, r => Assert.NotNull(r.RevokedAt));

                // The 'refresh_token_reuse' audit row exists for this chain.
                var auditRow = auth.tblH_Auth_Log.FirstOrDefault(p =>
                    p.EventType == AuthV1Service.EventTypeRefreshTokenReuse &&
                    p.RotationChainRoot == chainRoot);
                Assert.NotNull(auditRow);
                Assert.Equal(LocalDbFixture.Ids.UserA, auditRow.Subject);
            }

            // The still-current (third) refresh token is now dead too — the
            // chain-revoke killed the entire family, not just the reused token.
            Assert.Equal("failed", NewService().refresh(third.RefreshToken).Status);
        }

        // ----- revoke -----

        [Fact]
        public void Revoke_KillsSessionChain()
        {
            const string code = "141421";
            string otpId = SeedUserOtpSession(code);
            var pair = Mint(NewService().mintAfterOtpVerify(otpId, code, AuthV1Service.SubjectTypeUser));

            // Access token valid before revoke.
            Assert.True(Verify(NewService().verify(pair.AccessToken)).Valid);

            var revokeOp = NewService().revoke(pair.RefreshToken);
            Assert.Equal("success", revokeOp.Status);

            // Access token and refresh token are both dead after logout.
            Assert.False(Verify(NewService().verify(pair.AccessToken)).Valid);
            Assert.Equal("failed", NewService().refresh(pair.RefreshToken).Status);

            string refreshHash = AuthTokens.Hash(pair.RefreshToken);
            using (var auth = NewAuthContext())
            {
                var refreshRow = auth.tblT_RefreshToken.Single(p => p.TokenHash == refreshHash);
                Assert.NotNull(refreshRow.RevokedAt);
                Assert.NotNull(auth.tblH_Auth_Log.FirstOrDefault(p =>
                    p.EventType == AuthV1Service.EventTypeLogout &&
                    p.RotationChainRoot == refreshRow.RotationChainRoot));
            }
        }

        // ----- revokeAllForSubject -----

        [Fact]
        public void RevokeAllForSubject_ValidServiceToken_KillsAllSubjectTokens()
        {
            // Two independent sessions for the same subject.
            string id1 = SeedUserOtpSession("202020");
            var s1 = Mint(NewService().mintAfterOtpVerify(id1, "202020", AuthV1Service.SubjectTypeUser));
            string id2 = SeedUserOtpSession("303030");
            var s2 = Mint(NewService().mintAfterOtpVerify(id2, "303030", AuthV1Service.SubjectTypeUser));

            var op = NewService().revokeAllForSubject(
                LocalDbFixture.Ids.UserA, AuthV1Service.SubjectTypeUser, ServiceRevokeToken);
            Assert.Equal("success", op.Status);

            // Both sessions' access + refresh tokens are dead.
            Assert.False(Verify(NewService().verify(s1.AccessToken)).Valid);
            Assert.False(Verify(NewService().verify(s2.AccessToken)).Valid);
            Assert.Equal("failed", NewService().refresh(s1.RefreshToken).Status);
            Assert.Equal("failed", NewService().refresh(s2.RefreshToken).Status);

            using (var auth = NewAuthContext())
            {
                Assert.NotNull(auth.tblH_Auth_Log.FirstOrDefault(p =>
                    p.EventType == AuthV1Service.EventTypeSubjectRevoke &&
                    p.Subject == LocalDbFixture.Ids.UserA));
            }
        }

        [Fact]
        public void RevokeAllForSubject_WrongServiceToken_FailsAndLeavesTokensAndWritesAuditFailure()
        {
            string id = SeedUserOtpSession("404040");
            var s = Mint(NewService().mintAfterOtpVerify(id, "404040", AuthV1Service.SubjectTypeUser));

            var op = NewService().revokeAllForSubject(
                LocalDbFixture.Ids.UserA, AuthV1Service.SubjectTypeUser, "wrong-token");
            Assert.Equal("failed", op.Status);
            Assert.Equal("unauthorized", op.Message);

            // Token survives the unauthorized call.
            Assert.True(Verify(NewService().verify(s.AccessToken)).Valid);

            using (var auth = NewAuthContext())
            {
                Assert.NotNull(auth.tblH_Auth_Log.FirstOrDefault(p =>
                    p.EventType == AuthV1Service.EventTypeRevokeAuthFailure &&
                    p.Subject == LocalDbFixture.Ids.UserA));
            }
        }
    }
}
