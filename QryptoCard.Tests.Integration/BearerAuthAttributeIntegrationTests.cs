using System;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using System.Web.Http.Controllers;
using Newtonsoft.Json;
using QryptoCard.INT;
using QryptoCard.API;
using QryptoCard.INT.Model.Service;
using QryptoCard.INT.Script.Service.Auth.v1;
using QryptoCard.Sec;
using QryptoCard.Tests.Fixtures.LocalDb;
using Xunit;

namespace QryptoCard.Tests.Integration
{
    // End-to-end proof of the Bearer auth path against a real throwaway LocalDB.
    //
    // The BearerAuthenticationAttribute lives in QryptoCard.API and normally reaches
    // AuthV1Service over WCF (AuthTokenSecurity.WcfVerify). That WCF hop is the only
    // part that cannot run in-process under test, so we redirect the attribute's
    // single seam — AuthTokenSecurity.VerifyImpl — straight at a real, LocalDB-backed
    // AuthV1Service.verify. Everything else is the production code path: tokens are
    // minted by the real service, verify hits the real indexed token tables, and the
    // real attribute logic (scheme parse, Valid check, cross-tier guard, subject
    // stash, 401 shaping) runs unchanged.
    //
    // Asserts: a valid user token of the correct tier authenticates and exposes the
    // Subject; expired / revoked / missing / wrong-tier (user token on the admin
    // attribute) are all rejected with 401.
    public class BearerAuthAttributeIntegrationTests : IClassFixture<LocalDbFixture>, IDisposable
    {
        readonly LocalDbFixture _db;
        readonly Func<string, AuthVerifyResponse> _originalUserVerifyImpl;
        readonly Func<string, AuthVerifyResponse> _originalAdminVerifyImpl;

        public BearerAuthAttributeIntegrationTests(LocalDbFixture db)
        {
            _db = db;

            // The fixture sets KASH_DATA_KEY/QRYPTO_ENVIRONMENT; mirror the AuthV1Service
            // suite and give revokeAllForSubject a known service token (unused here but
            // keeps SecretsConfig consistent if any path reads it).
            Environment.SetEnvironmentVariable(
                AuthV1Service.ServiceRevokeTokenSecretName, "test-service-revoke-token-do-not-use");
            SecretsConfig.ResetCacheForTests();

            // Point both tiers' Bearer seam at the real LocalDB-backed verify. Capture
            // the originals so teardown restores production wiring for other assemblies.
            _originalUserVerifyImpl  = QryptoCard.API.AuthTokenSecurity.VerifyImpl;
            _originalAdminVerifyImpl = QryptoCard.API.Admin.AuthTokenSecurity.VerifyImpl;

            QryptoCard.API.AuthTokenSecurity.VerifyImpl       = RealVerify;
            QryptoCard.API.Admin.AuthTokenSecurity.VerifyImpl = RealVerify;
        }

        public void Dispose()
        {
            QryptoCard.API.AuthTokenSecurity.VerifyImpl       = _originalUserVerifyImpl;
            QryptoCard.API.Admin.AuthTokenSecurity.VerifyImpl = _originalAdminVerifyImpl;
        }

        // ----- real-service bridge -----

        AuthV1Service NewService()
            => new AuthV1Service(_db.NewContext(), new AuthDbContext(_db.ConnectionString));

        // Bridge the attribute's seam to the genuine WCF method: call the real
        // verify(), then deserialize op.Data exactly the way the production
        // WcfVerify path does after the wire hop.
        AuthVerifyResponse RealVerify(string accessToken)
        {
            var op = NewService().verify(accessToken);
            if (op == null || op.Status != "success" || op.Data == null)
                return new AuthVerifyResponse { Valid = false };
            return JsonConvert.DeserializeObject<AuthVerifyResponse>((string)op.Data)
                ?? new AuthVerifyResponse { Valid = false };
        }

        // ----- helpers -----

        static HttpActionContext BuildContext(string authorizationValue)
        {
            var req = new HttpRequestMessage(HttpMethod.Get, "http://localhost/test");
            if (authorizationValue != null)
                req.Headers.TryAddWithoutValidation("Authorization", authorizationValue);

            var config = new HttpConfiguration();
            req.SetConfiguration(config);

            var ctx = new HttpControllerContext { Request = req, Configuration = config };
            return new HttpActionContext { ControllerContext = ctx };
        }

        string SeedUserOtpSession(string plainCode)
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
                    DateExpired = DateTime.UtcNow.AddMinutes(15),
                    isVerify = 0
                });
                ctx.SaveChanges();
            }
            return id;
        }

        // Mint a real user-tier access/refresh pair via the genuine service.
        AuthMintResponse MintUserPair(string code)
        {
            string otpId = SeedUserOtpSession(code);
            var op = NewService().mintAfterOtpVerify(otpId, code, AuthV1Service.SubjectTypeUser);
            Assert.Equal("success", op.Status);
            return JsonConvert.DeserializeObject<AuthMintResponse>((string)op.Data);
        }

        // ----- tests -----

        [Fact]
        public void ValidUserToken_OnUserAttribute_Authenticates_AndStashesSubject()
        {
            var pair = MintUserPair("314159");

            var attr = new BearerAuthenticationAttribute(); // ExpectedSubjectType defaults to "user"
            var ctx = BuildContext("Bearer " + pair.AccessToken);

            attr.OnAuthorization(ctx);

            // No response set -> request continues to the controller.
            Assert.Null(ctx.Response);

            object subject, subjectType;
            Assert.True(ctx.Request.Properties.TryGetValue(
                BearerAuthenticationAttribute.SubjectPropertyKey, out subject));
            Assert.True(ctx.Request.Properties.TryGetValue(
                BearerAuthenticationAttribute.SubjectTypePropertyKey, out subjectType));
            Assert.Equal(LocalDbFixture.Ids.UserA, subject);
            Assert.Equal(AuthV1Service.SubjectTypeUser, subjectType);
        }

        [Fact]
        public void MissingAuthorizationHeader_Returns401()
        {
            var attr = new BearerAuthenticationAttribute();
            var ctx = BuildContext(null);

            attr.OnAuthorization(ctx);

            Assert.NotNull(ctx.Response);
            Assert.Equal(HttpStatusCode.Unauthorized, ctx.Response.StatusCode);
            // RFC 6750 §3: a Bearer 401 carries WWW-Authenticate: Bearer realm=...
            Assert.Contains(ctx.Response.Headers.WwwAuthenticate,
                h => string.Equals(h.Scheme, "Bearer", StringComparison.OrdinalIgnoreCase));
        }

        [Fact]
        public void NonBearerScheme_Returns401()
        {
            var attr = new BearerAuthenticationAttribute();
            var ctx = BuildContext("Basic dXNlcjpwYXNz");

            attr.OnAuthorization(ctx);

            Assert.NotNull(ctx.Response);
            Assert.Equal(HttpStatusCode.Unauthorized, ctx.Response.StatusCode);
        }

        [Fact]
        public void GarbageToken_Returns401()
        {
            var attr = new BearerAuthenticationAttribute();
            var ctx = BuildContext("Bearer at_this-token-was-never-minted");

            attr.OnAuthorization(ctx);

            Assert.NotNull(ctx.Response);
            Assert.Equal(HttpStatusCode.Unauthorized, ctx.Response.StatusCode);
        }

        [Fact]
        public void ExpiredToken_Returns401()
        {
            // Insert an already-expired access-token row directly, then present it.
            string raw = AuthTokens.NewAccessToken();
            using (var auth = new AuthDbContext(_db.ConnectionString))
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

            var attr = new BearerAuthenticationAttribute();
            var ctx = BuildContext("Bearer " + raw);

            attr.OnAuthorization(ctx);

            Assert.NotNull(ctx.Response);
            Assert.Equal(HttpStatusCode.Unauthorized, ctx.Response.StatusCode);
        }

        [Fact]
        public void RevokedToken_Returns401()
        {
            // Mint a real pair, then revoke the chain (logout) — the access token dies.
            var pair = MintUserPair("141421");
            Assert.Equal("success", NewService().revoke(pair.RefreshToken).Status);

            var attr = new BearerAuthenticationAttribute();
            var ctx = BuildContext("Bearer " + pair.AccessToken);

            attr.OnAuthorization(ctx);

            Assert.NotNull(ctx.Response);
            Assert.Equal(HttpStatusCode.Unauthorized, ctx.Response.StatusCode);
        }

        [Fact]
        public void UserToken_OnAdminAttribute_IsRejected_CrossTierGuard()
        {
            // A genuine user-tier token presented to the Admin-tier attribute
            // (ExpectedSubjectType = "admin") must 401 — the cross-tier guard.
            var pair = MintUserPair("271828");

            var adminAttr = new QryptoCard.API.Admin.BearerAuthenticationAttribute(); // defaults to "admin"
            var ctx = BuildContext("Bearer " + pair.AccessToken);

            adminAttr.OnAuthorization(ctx);

            Assert.NotNull(ctx.Response);
            Assert.Equal(HttpStatusCode.Unauthorized, ctx.Response.StatusCode);
        }
    }
}
