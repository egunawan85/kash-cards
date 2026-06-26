using System;
using System.Linq;
using QryptoCard.INT;
using QryptoCard.INT.Script.Service.Admin.v1;
using QryptoCard.INT.Script.Service.Auth.v1;
using QryptoCard.INT.Security;
using QryptoCard.Tests.Fixtures.LocalDb;
using Xunit;

namespace QryptoCard.Tests.Integration
{
    // ---- Wall 1 (the load-bearing control): fail-closed environment allow-list ----
    // Pure-logic, no DB. Exhaustively pins the allow-list so an unset/typo'd/renamed
    // environment can never be mistaken for dev and mint money.
    public class TestCreditGateTests
    {
        [Theory]
        [InlineData("dev")]
        [InlineData("sandbox")]
        [InlineData("DEV")]        // case-insensitive
        [InlineData("Sandbox")]
        [InlineData(" dev ")]      // whitespace-insensitive
        [InlineData("sandbox\t")]
        public void IsAllowed_ExplicitNonProd_True(string env)
        {
            Assert.True(TestCreditGate.IsAllowed(env));
        }

        [Theory]
        [InlineData("prod")]
        [InlineData("production")]
        [InlineData("PROD")]
        [InlineData("staging")]
        [InlineData("test")]
        [InlineData("development")] // only the exact token "dev" is allowed, not a prefix-of
        [InlineData("prdo")]        // typo must fail closed, not fall through to "allow"
        [InlineData("")]
        [InlineData("   ")]
        [InlineData(null)]          // unset variable stays null -> denied
        public void IsAllowed_ProdUnknownOrUnset_False(string env)
        {
            Assert.False(TestCreditGate.IsAllowed(env));
        }
    }

    // ---- Full service path: env-gate + root-admin-only + audit + real credit ----
    // Drives the real AdminV1Service.devCreditWallet against the LocalDB fixture,
    // through the real WalletService.CreditDeposit. Only the vw_Admin role lookup is
    // stubbed (that EF defining-query view is not materialised by the fixture); every
    // money-touching path — gates, EnsureWallet, the atomic credit, dedup, the audit
    // write — is exercised for real.
    public class DevCreditToolIntegrationTests : IClassFixture<LocalDbFixture>
    {
        readonly LocalDbFixture _db;
        public DevCreditToolIntegrationTests(LocalDbFixture db) { _db = db; }

        const string EnvVar = "QRYPTO_ENVIRONMENT";

        static string Fresh(string tag) =>
            "dct-" + tag + "-" + Guid.NewGuid().ToString("N").Substring(0, 8);

        // AdminV1Service with the vw_Admin role lookup overridden to a fixed value.
        sealed class TestableAdminV1Service : AdminV1Service
        {
            readonly string _role;
            public TestableAdminV1Service(DBEntities db, AuthDbContext authDb, string role)
                : base(db, authDb) { _role = role; }
            protected override string getRole(string em) => _role;
        }

        TestableAdminV1Service NewService(string role) =>
            new TestableAdminV1Service(_db.NewContext(), new AuthDbContext(_db.ConnectionString), role);

        void SeedUser(string uid)
        {
            using (var ctx = _db.NewContext())
            {
                ctx.tblM_User.Add(new tblM_User { UserID = uid, Email = uid + "@t.test", isActive = 1 });
                ctx.SaveChanges();
            }
        }

        // Returns the seeded admin's AdminID (so the audit "who" can be asserted).
        string SeedAdmin(string email)
        {
            var adminId = Fresh("admin");
            using (var ctx = _db.NewContext())
            {
                ctx.tblM_Admin.Add(new tblM_Admin { AdminID = adminId, Email = email, Phone = "0", isActive = 1 });
                ctx.SaveChanges();
            }
            return adminId;
        }

        decimal BalanceOf(string uid)
        {
            using (var ctx = _db.NewContext())
                return ctx.tblM_User_Balance
                    .Where(p => p.UserID == uid && p.Currency == "USDT")
                    .Select(p => p.Balance).FirstOrDefault() ?? 0m;
        }

        // The Details JSON of this test's most recent test-credit audit row, or null.
        string LastAuditDetails(string uid)
        {
            using (var auth = new AuthDbContext(_db.ConnectionString))
                return auth.tblH_Auth_Log
                    .Where(p => p.EventType == "dev_test_credit" && p.Details.Contains(uid))
                    .OrderByDescending(p => p.DateLogged)
                    .Select(p => p.Details)
                    .FirstOrDefault();
        }

        int AuditRowCount(string uid)
        {
            using (var auth = new AuthDbContext(_db.ConnectionString))
                return auth.tblH_Auth_Log.Count(p => p.EventType == "dev_test_credit" && p.Details.Contains(uid));
        }

        [Fact]
        public void DevAndOwner_Credits_AndAuditsWho()
        {
            // Fixture sets QRYPTO_ENVIRONMENT=dev.
            var email = Fresh("owner") + "@t.test";
            var adminId = SeedAdmin(email);
            var uid = Fresh("ok");
            SeedUser(uid);

            var op = NewService("Owner").devCreditWallet(email, uid, 250m, Fresh("ref"));

            Assert.Equal("success", op.Status);
            Assert.Equal(250m, BalanceOf(uid));

            var details = LastAuditDetails(uid);
            Assert.NotNull(details);
            Assert.Contains("\"result\":\"credited\"", details);
            // The acting root-admin (who) is captured on the audit row.
            using (var auth = new AuthDbContext(_db.ConnectionString))
            {
                var row = auth.tblH_Auth_Log
                    .Where(p => p.EventType == "dev_test_credit" && p.Details.Contains(uid))
                    .OrderByDescending(p => p.DateLogged).First();
                Assert.Equal(adminId, row.Subject);
                Assert.Equal("admin", row.SubjectType);
            }
        }

        [Fact]
        public void Replay_SameReference_DedupesNoDoubleCredit()
        {
            var email = Fresh("owner") + "@t.test";
            SeedAdmin(email);
            var uid = Fresh("replay");
            SeedUser(uid);
            var reference = Fresh("idem");

            var first = NewService("Owner").devCreditWallet(email, uid, 100m, reference);
            Assert.Equal("success", first.Status);
            Assert.Equal(100m, BalanceOf(uid));

            var second = NewService("Owner").devCreditWallet(email, uid, 100m, reference);
            Assert.Equal("failed", second.Status);
            Assert.Contains("already", second.Message);
            // Balance is unchanged — the replay did NOT double-credit.
            Assert.Equal(100m, BalanceOf(uid));
        }

        [Fact]
        public void ProdEnv_RefusesEvenForOwner_NoCredit()
        {
            var original = Environment.GetEnvironmentVariable(EnvVar);
            try
            {
                Environment.SetEnvironmentVariable(EnvVar, "prod");

                var email = Fresh("owner") + "@t.test";
                SeedAdmin(email);
                var uid = Fresh("prod");
                SeedUser(uid);

                var op = NewService("Owner").devCreditWallet(email, uid, 500m, Fresh("ref"));

                Assert.Equal("failed", op.Status);
                Assert.Equal(0m, BalanceOf(uid));
                Assert.Contains("\"result\":\"env_refused\"", LastAuditDetails(uid));
            }
            finally
            {
                Environment.SetEnvironmentVariable(EnvVar, original);
            }
        }

        [Fact]
        public void UnsetEnv_RefusesEvenForOwner_NoCredit()
        {
            // Defense in depth: an unset variable must fail closed, NOT default to dev.
            var original = Environment.GetEnvironmentVariable(EnvVar);
            try
            {
                Environment.SetEnvironmentVariable(EnvVar, null);

                var email = Fresh("owner") + "@t.test";
                SeedAdmin(email);
                var uid = Fresh("unset");
                SeedUser(uid);

                var op = NewService("Owner").devCreditWallet(email, uid, 500m, Fresh("ref"));

                Assert.Equal("failed", op.Status);
                Assert.Equal(0m, BalanceOf(uid));
            }
            finally
            {
                Environment.SetEnvironmentVariable(EnvVar, original);
            }
        }

        [Theory]
        [InlineData("Admin")]   // a high but non-root role is still refused
        [InlineData("Viewer")]
        [InlineData("")]        // unknown/blank role
        public void NonOwner_Refused_NoCredit(string role)
        {
            var email = Fresh("nonowner") + "@t.test";
            SeedAdmin(email);
            var uid = Fresh("role");
            SeedUser(uid);

            var op = NewService(role).devCreditWallet(email, uid, 300m, Fresh("ref"));

            Assert.Equal("failed", op.Status);
            Assert.Equal(0m, BalanceOf(uid));
            Assert.Contains("\"result\":\"not_owner\"", LastAuditDetails(uid));
        }

        [Fact]
        public void UnknownTargetUser_Refused()
        {
            var email = Fresh("owner") + "@t.test";
            SeedAdmin(email);
            var uid = Fresh("ghost"); // deliberately NOT seeded

            var op = NewService("Owner").devCreditWallet(email, uid, 10m, Fresh("ref"));

            Assert.Equal("failed", op.Status);
            Assert.Contains("\"result\":\"user_not_found\"", LastAuditDetails(uid));
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-5)]
        public void NonPositiveAmount_Refused_NoCredit(int amount)
        {
            var email = Fresh("owner") + "@t.test";
            SeedAdmin(email);
            var uid = Fresh("amt");
            SeedUser(uid);

            var op = NewService("Owner").devCreditWallet(email, uid, amount, Fresh("ref"));

            Assert.Equal("failed", op.Status);
            Assert.Equal(0m, BalanceOf(uid));
        }
    }
}
