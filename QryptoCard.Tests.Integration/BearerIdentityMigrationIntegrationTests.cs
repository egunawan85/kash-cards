using System;
using System.Linq;
using System.Net.Http;
using System.Web.Http;
using System.Web.Http.Controllers;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using QryptoCard.API;
using QryptoCard.INT;
using QryptoCard.INT.Model.Service;
using QryptoCard.INT.Script.Service.Auth.v1;
using QryptoCard.Sec;
using QryptoCard.Tests.Fixtures.LocalDb;
using Xunit;

namespace QryptoCard.Tests.Integration
{
    // Proves the Bearer-authed IDENTITY path that the user-facing controllers now
    // depend on after the Basic -> Bearer cutover.
    //
    // The migrated controllers no longer decode a Basic header for the caller's
    // email — they call getEmail(), which reads the qc_email the
    // BearerAuthenticationAttribute stashes on Request.Properties after a
    // successful token verify. The legacy WCF services are keyed on EMAIL (the
    // controller passes `em` -> getUserId(em)), so qc_email MUST be the verified
    // subject's email — not the opaque qc_subject (UserID). If the attribute (or a
    // base-controller helper) returned the subject instead, every WCF lookup would
    // resolve the wrong user or fail. This suite locks that contract end-to-end:
    //
    //   1. mint a real user token for the seeded user (real AuthV1Service + LocalDB),
    //   2. run the real attribute over a request carrying that Bearer token,
    //   3. assert the value a controller's getEmail() would read is the seeded
    //      user's EMAIL (and is NOT the UserID), so the downstream WCF email lookup
    //      resolves the correct user,
    //   4. assert mintAfterOtpVerify now returns a non-empty Profile that
    //      deserializes to the seeded user with credential columns redacted.
    //
    // Same in-process bridge as BearerAuthAttributeIntegrationTests: the attribute's
    // single WCF seam (AuthTokenSecurity.VerifyImpl) is redirected at the real
    // LocalDB-backed verify; everything else is the production code path.
    public class BearerIdentityMigrationIntegrationTests
        : IClassFixture<LocalDbFixture>, IDisposable
    {
        readonly LocalDbFixture _db;
        readonly Func<string, AuthVerifyResponse> _originalUserVerifyImpl;

        public BearerIdentityMigrationIntegrationTests(LocalDbFixture db)
        {
            _db = db;
            Environment.SetEnvironmentVariable(
                AuthV1Service.ServiceRevokeTokenSecretName, "test-service-revoke-token-do-not-use");
            SecretsConfig.ResetCacheForTests();

            _originalUserVerifyImpl = QryptoCard.API.AuthTokenSecurity.VerifyImpl;
            QryptoCard.API.AuthTokenSecurity.VerifyImpl = RealVerify;
        }

        public void Dispose()
        {
            QryptoCard.API.AuthTokenSecurity.VerifyImpl = _originalUserVerifyImpl;
        }

        // ----- real-service bridge -----

        AuthV1Service NewService()
            => new AuthV1Service(_db.NewContext(), new AuthDbContext(_db.ConnectionString));

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
            var req = new HttpRequestMessage(HttpMethod.Get, "http://localhost/v1/user/dashboard/data");
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

        OutputModel MintUserOp(string code)
        {
            string otpId = SeedUserOtpSession(code);
            var op = NewService().mintAfterOtpVerify(otpId, code, AuthV1Service.SubjectTypeUser);
            Assert.Equal("success", op.Status);
            return op;
        }

        static AuthMintResponse Mint(OutputModel op)
            => JsonConvert.DeserializeObject<AuthMintResponse>((string)op.Data);

        // The value a migrated controller's getEmail() helper reads: the qc_email
        // stash the attribute writes on a successful verify. Mirrors
        // QryptoCardApiController.getEmail() exactly (same property key).
        static string EmailStashFor(HttpActionContext ctx)
        {
            object email;
            return ctx.Request.Properties.TryGetValue(
                BearerAuthenticationAttribute.EmailPropertyKey, out email)
                ? email as string
                : null;
        }

        // ----- tests -----

        // The crux of the migration: the identity a controller forwards to the
        // email-keyed WCF layer is the EMAIL, resolved from the token — not the
        // opaque subject id.
        [Fact]
        public void ValidUserToken_StashesEmail_SoControllerGetEmailResolvesSeededUser()
        {
            var pair = Mint(MintUserOp("314159"));

            var attr = new BearerAuthenticationAttribute(); // defaults to "user"
            var ctx = BuildContext("Bearer " + pair.AccessToken);
            attr.OnAuthorization(ctx);

            // Authenticated — request continues to the controller.
            Assert.Null(ctx.Response);

            // getEmail() (what every migrated WCF call site now passes as `em`)
            // returns the seeded user's email.
            string email = EmailStashFor(ctx);
            Assert.Equal(LocalDbFixture.Ids.EmailA, email);

            // And it is NOT the opaque UserID — guarding the email-vs-subject
            // correction. A regression that stashed the subject here would
            // authenticate the wrong user (or none) at the email-keyed WCF layer.
            Assert.NotEqual(LocalDbFixture.Ids.UserA, email);

            // qc_subject is still available (for callers that want the opaque id).
            object subject;
            Assert.True(ctx.Request.Properties.TryGetValue(
                BearerAuthenticationAttribute.SubjectPropertyKey, out subject));
            Assert.Equal(LocalDbFixture.Ids.UserA, subject);
        }

        // Proves the email the controller forwards actually resolves the correct
        // user at the WCF tier — the same email->UserID lookup getDashboardData(em)
        // performs. Resolving the seeded user (and not a wrong one / not a miss) is
        // the whole point of keying on email.
        [Fact]
        public void StashedEmail_ResolvesCorrectUserId_AtWcfTier()
        {
            var pair = Mint(MintUserOp("161803"));

            var attr = new BearerAuthenticationAttribute();
            var ctx = BuildContext("Bearer " + pair.AccessToken);
            attr.OnAuthorization(ctx);

            string email = EmailStashFor(ctx);
            Assert.Equal(LocalDbFixture.Ids.EmailA, email);

            // The email->UserID resolution the email-keyed services do internally
            // (e.g. getUserId(em)). It lands on exactly the seeded user.
            using (var ctxDb = _db.NewContext())
            {
                string resolvedUserId = ctxDb.tblM_User
                    .Where(u => u.Email == email)
                    .Select(u => u.UserID)
                    .FirstOrDefault();
                Assert.Equal(LocalDbFixture.Ids.UserA, resolvedUserId);
            }
        }

        // ----- Profile in mint -----

        [Fact]
        public void MintAfterOtpVerify_ReturnsNonEmptyProfile_ThatDeserializesToSeededUser()
        {
            var op = MintUserOp("271828");
            var mint = Mint(op);

            Assert.False(string.IsNullOrEmpty(mint.Profile));

            var profile = JObject.Parse(mint.Profile);
            Assert.Equal(LocalDbFixture.Ids.UserA, (string)profile["UserID"]);
            Assert.Equal(LocalDbFixture.Ids.EmailA, (string)profile["Email"]);
            Assert.Equal("Alpha", (string)profile["FirstName"]);

            // Credential columns are redacted before serialization — the legacy
            // /auth/login/verify leaked them; the Bearer mint must not.
            Assert.Null((string)profile["Password"]);
            Assert.Null((string)profile["PIN"]);
        }
    }
}
