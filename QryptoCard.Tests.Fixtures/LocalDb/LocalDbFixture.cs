using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Threading;
using QryptoCard.INT;

namespace QryptoCard.Tests.Fixtures.LocalDb
{
    // Shared LocalDB fixture for DB-backed integration tests.
    //
    // Lifecycle: xUnit constructs once per IClassFixture<LocalDbFixture> usage. The constructor
    // drops + recreates a fixed-name database on the auto-instance (localdb)\MSSQLLocalDB,
    // applies the EF-generated schema (TestData/init.sql), and seeds a baseline tenant (one
    // company / role / user). Each test reads the shared seeded state; tests that mutate state
    // should target IDs not also read by other tests in the class.
    //
    // The database name is fixed (TestQryptoCard) so the WCF service classes' field-initialised
    // `new DBEntities()` — which reads name=DBEntities from the test App.config — lands on the
    // same DB this fixture seeds. Cross-class isolation is by drop+recreate at each fixture
    // construction; in-process serial execution is enforced via
    // [assembly: CollectionBehavior(DisableTestParallelization = true)] (AssemblyInfo.cs);
    // cross-process serialization is via the named global mutex below.
    public class LocalDbFixture : IDisposable
    {
        // Stable IDs the tests reference. nvarchar(50) so 50 chars max each.
        public static class Ids
        {
            public const string RoleOwner = "role-owner";

            // The baseline seeded user. Already verified/active so Login happy-path and
            // RegisterVerify (which seeds a fresh pending tblH_User_Register against it) both work.
            public const string UserA = "user-A";
            public const string EmailA = "user-a@alpha.example";

            // Plaintext login password. The legacy Login path compares against Secure.APPtoDB(pwd);
            // tests that exercise Login's success branch should seed the stored password the same way.
            public const string PasswordPlain = "TestPassword_DoNotUse";
        }

        public readonly string DatabaseName;
        public readonly string ConnectionString;

        // Cross-process serialization of integration-test runs. Concurrent test processes
        // (parallel agent sessions, VS Test Explorer, CLI vstest) share BOTH the single
        // (localdb)\MSSQLLocalDB instance and the fixed TestQryptoCard catalog name, so two
        // suites running at once would drop/recreate each other's database mid-run. A named
        // global mutex serializes at process granularity, acquired once before the first
        // fixture touches the database and rooted in a static so it is held until the test host
        // exits (the OS releases it on process death). In-process serialization is separately
        // guaranteed by [assembly: CollectionBehavior(DisableTestParallelization)].
        static Mutex _crossProcessLockHandle; // rooted for process lifetime — do not release or null
        static readonly Lazy<bool> CrossProcessTestLock =
            new Lazy<bool>(AcquireCrossProcessTestLock, LazyThreadSafetyMode.ExecutionAndPublication);

        static bool AcquireCrossProcessTestLock()
        {
            var mutex = new Mutex(false, @"Global\QryptoCard-TestQryptoCard-IntegrationSuite");
            var step = TimeSpan.FromSeconds(30);
            var waited = TimeSpan.Zero;
            var cap = TimeSpan.FromMinutes(30);
            while (true)
            {
                try
                {
                    if (mutex.WaitOne(step))
                    {
                        _crossProcessLockHandle = mutex;
                        return true;
                    }
                }
                catch (AbandonedMutexException)
                {
                    // Previous holder's process died without releasing. Safe to proceed:
                    // ownership transferred to us, and every fixture rebuilds the database
                    // from scratch, so a half-finished foreign run leaves nothing we depend on.
                    _crossProcessLockHandle = mutex;
                    return true;
                }
                waited += step;
                if (waited >= cap)
                    throw new InvalidOperationException(
                        "Timed out after 30 minutes waiting for the TestQryptoCard integration-suite " +
                        "lock (Global\\QryptoCard-TestQryptoCard-IntegrationSuite). Another test process " +
                        "appears stuck — find and stop it (tasklist | findstr /i \"vstest testhost\"), then re-run.");
            }
        }

        public LocalDbFixture()
        {
            // Block here (process-wide, once) until no other test process is mid-suite.
            bool _ = CrossProcessTestLock.Value;

            DatabaseName = "TestQryptoCard";
            ConnectionString = "data source=(localdb)\\MSSQLLocalDB;initial catalog=" + DatabaseName +
                               ";integrated security=True;multipleactiveresultsets=True;application name=QryptoCardTests";

            // RegisterVerify (and the other registration flows) take the non-prod branch unless
            // QRYPTO_ENVIRONMENT == "prod"; force the local default so the on-chain address
            // creation (Fireblocks) is skipped under test. SecretsConfig caches first-read per
            // process, so set it before any service method reads it.
            Environment.SetEnvironmentVariable("QRYPTO_ENVIRONMENT", "dev");

            // Secure.APPtoDB / EncryptDB / DecryptAPP read these symmetric keys via
            // SecretsConfig.Require, which throws if unset. The Login path and the seeded
            // password below both go through Secure, so seed fixed test-only key material here
            // (set before the first read so SecretsConfig caches these values). Not production
            // keys — they only ever encrypt the throwaway TestQryptoCard fixtures.
            Environment.SetEnvironmentVariable("APPKEY", "test-appkey-do-not-use");
            Environment.SetEnvironmentVariable("DBKEY", "test-dbkey-do-not-use");

            CreateDatabase();
            ApplySchema();
            ApplyTokenSchema();
            ApplyWalletIndexes();
            ApplyWebhookDedupIndex();
            ApplyReferralCommissionDedupIndex();
            SeedData();
        }

        public void Dispose()
        {
            DropDatabase();
        }

        public DBEntities NewContext()
        {
            // Build an EntityConnection string that explicitly targets our per-fixture database:
            // the EF metadata path (res://*/DB.*, embedded in QryptoCard.INT.dll) plus our raw
            // provider connection string. Uses the DBEntities(string) ctor in DB.Context.Partial.cs.
            var entityConnStr =
                "metadata=res://*/DB.csdl|res://*/DB.ssdl|res://*/DB.msl;" +
                "provider=System.Data.SqlClient;provider connection string=\"" + ConnectionString + "\"";
            return new DBEntities(entityConnStr);
        }

        // ----- private: lifecycle -----

        const string MasterConnString =
            "data source=(localdb)\\MSSQLLocalDB;initial catalog=master;integrated security=True;application name=QryptoCardTests";

        void CreateDatabase()
        {
            // Issue plain DROPs, swallow their errors, and POLL until the catalog entry is gone
            // before creating (a recently-used LocalDB database can return error 3701 while the
            // engine completes removal asynchronously). ClearAllPools first so pooled connections
            // from earlier fixtures in the same process don't pin the database. AUTO_CLOSE OFF in
            // the same breath as CREATE: LocalDB defaults AUTO_CLOSE ON, and the open/close churn
            // intermittently fails logins; keeping the DB open for the fixture's lifetime avoids it.
            SqlConnection.ClearAllPools();
            using (var conn = new SqlConnection(MasterConnString))
            {
                conn.Open();
                if (!DropAndWaitUntilGone(conn))
                    throw new InvalidOperationException(
                        "LocalDB database '" + DatabaseName + "' would not drop within the retry budget. " +
                        "Recover manually: sqllocaldb stop MSSQLLocalDB; delete the orphan " +
                        DatabaseName + ".mdf/_log.ldf under %USERPROFILE%; sqllocaldb start MSSQLLocalDB.");
                Exec(conn, "create database [" + DatabaseName + "]");
                Exec(conn, "alter database [" + DatabaseName + "] set auto_close off");
            }
            WaitUntilOnline();
        }

        bool DropAndWaitUntilGone(SqlConnection conn)
        {
            for (int attempt = 0; attempt < 60; attempt++)
            {
                using (var check = new SqlCommand("select db_id('" + DatabaseName + "')", conn))
                {
                    if (check.ExecuteScalar() == DBNull.Value) return true;
                }
                try { Exec(conn, "drop database [" + DatabaseName + "]"); }
                catch (SqlException) { /* deferred-drop 3701 et al — poll again */ }
                Thread.Sleep(250);
            }
            using (var check = new SqlCommand("select db_id('" + DatabaseName + "')", conn))
            {
                return check.ExecuteScalar() == DBNull.Value;
            }
        }

        void WaitUntilOnline()
        {
            // CREATE DATABASE can return while the new database is still briefly inaccessible
            // (login 18456). Probe with Pooling=false so a failed Open isn't re-thrown from the
            // ADO.NET pool's blocking period, which would defeat the 250ms retry cadence.
            string probeConnString = ConnectionString + ";Pooling=false";
            for (int attempt = 0; attempt < 60; attempt++)
            {
                try
                {
                    using (var probe = new SqlConnection(probeConnString))
                    {
                        probe.Open();
                        return;
                    }
                }
                catch (SqlException)
                {
                    Thread.Sleep(250);
                }
            }
        }

        void DropDatabase()
        {
            try
            {
                SqlConnection.ClearAllPools();
                using (var conn = new SqlConnection(MasterConnString))
                {
                    conn.Open();
                    DropAndWaitUntilGone(conn);
                }
            }
            catch
            {
                // best-effort — leftover databases are harmless on a dev box, and we don't want a
                // teardown exception to mask the real test failure.
            }
        }

        void ApplySchema() => RunScriptFile(ResolveTestDataPath("init.sql"));

        // The opaque-Bearer-token tables (tblT_AuthToken, tblT_RefreshToken,
        // tblH_Auth_Log) are owned by the deploy DDL, not the EF-generated
        // init.sql (AuthDbContext is code-first with SetInitializer null — it
        // never creates its own schema). Apply the same idempotent deploy script
        // the production redeploy runs so AuthV1Service integration tests have
        // the tables to read/write against.
        void ApplyTokenSchema() => RunScriptFile(ResolveTokenSchemaPath());

        static string ResolveTokenSchemaPath()
            => ResolveRepoFilePath("deploy", "sql", "create-token-tables.sql");

        // The prepaid-wallet filtered unique indexes (one active wallet / deposit address
        // per user) are additive deploy DDL, not part of the EF init.sql. Apply the same
        // idempotent script the production redeploy runs so the wallet race-safety and
        // dedup tests exercise the real constraints.
        void ApplyWalletIndexes() => RunScriptFile(ResolveWalletIndexesPath());

        static string ResolveWalletIndexesPath()
            => ResolveRepoFilePath("deploy", "sql", "create-wallet-indexes.sql");

        // The per-event webhook dedup unique index — the replay defence the deposit-credit
        // branch relies on. Applied like the other additive deploy DDL so the dedup tests
        // exercise the real constraint.
        void ApplyWebhookDedupIndex() => RunScriptFile(ResolveWebhookDedupIndexPath());

        static string ResolveWebhookDedupIndexPath()
            => ResolveRepoFilePath("deploy", "sql", "create-webhook-dedup-index.sql");

        // The referral-commission dedup unique index — the replay defence the referral payout
        // relies on (filtered to Type='ReferralCommission', sibling to the PGCrypto index).
        void ApplyReferralCommissionDedupIndex()
            => RunScriptFile(ResolveRepoFilePath("deploy", "sql", "create-referral-commission-dedup-index.sql"));

        void RunScriptFile(string path)
        {
            var script = File.ReadAllText(path);
            using (var conn = new SqlConnection(ConnectionString))
            {
                conn.Open();
                foreach (var batch in SplitBatches(script))
                {
                    if (string.IsNullOrWhiteSpace(batch)) continue;
                    Exec(conn, batch);
                }
            }
        }

        static IEnumerable<string> SplitBatches(string script)
        {
            // Split on the "go" batch separator (case-insensitive, on its own line) — matches how
            // sqlcmd / SSMS would run the CreateDatabaseScript output.
            var lines = script.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
            var current = new System.Text.StringBuilder();
            foreach (var line in lines)
            {
                var trimmed = line.Trim();
                if (trimmed.Equals("go", StringComparison.OrdinalIgnoreCase))
                {
                    yield return current.ToString();
                    current.Clear();
                }
                else if (trimmed.StartsWith("USE ", StringComparison.OrdinalIgnoreCase))
                {
                    // Skip USE <db>; — it would switch the open connection off the per-fixture DB.
                    continue;
                }
                else
                {
                    current.AppendLine(line);
                }
            }
            yield return current.ToString();
        }

        static string ResolveTestDataPath(string fileName)
            => ResolveRepoFilePath("QryptoCard.Tests.Fixtures", "TestData", fileName);

        static string ResolveRepoFilePath(params string[] relSegments)
        {
            // Walk up from bin/Debug to the repo root by looking for the
            // QryptoCard.Tests.Fixtures/TestData directory as the anchor. The .sql files are kept
            // in source (not copied to bin/Debug) so any consumer resolves them via this walk-up.
            var dir = AppDomain.CurrentDomain.BaseDirectory;
            while (dir != null && !Directory.Exists(Path.Combine(dir, "QryptoCard.Tests.Fixtures", "TestData")))
                dir = Directory.GetParent(dir)?.FullName;
            if (dir == null)
                throw new InvalidOperationException("Cannot locate repo root (QryptoCard.Tests.Fixtures/TestData anchor) from " +
                                                    AppDomain.CurrentDomain.BaseDirectory);
            var path = dir;
            foreach (var seg in relSegments) path = Path.Combine(path, seg);
            return path;
        }

        static void Exec(SqlConnection conn, string sql)
        {
            using (var cmd = new SqlCommand(sql, conn))
            {
                cmd.CommandTimeout = 30;
                cmd.ExecuteNonQuery();
            }
        }

        // ----- private: seed -----

        void SeedData()
        {
            using (var db = NewContext())
            {
                db.tblM_User_Role.Add(new tblM_User_Role
                {
                    RoleID = Ids.RoleOwner,
                    Role = "Owner",
                    isActive = 1,
                    DateCreated = DateTime.UtcNow
                });

                db.tblM_User.Add(new tblM_User
                {
                    UserID = Ids.UserA,
                    RoleID = Ids.RoleOwner,
                    Email = Ids.EmailA,
                    FirstName = "Alpha",
                    LastName = "User",
                    // Login computes Secure.APPtoDB(x.Password) == EncryptDB(DecryptAPP(x.Password))
                    // and compares it to the stored Password. Store EncryptDB(plaintext) so that a
                    // Login call passing EncryptAPP(plaintext) round-trips to a match. Tests that
                    // only need RegisterVerify don't depend on this value.
                    Password = QryptoCard.Sec.Secure.EncryptDB(Ids.PasswordPlain),
                    isActive = 1,
                    isVerified = 1,
                    isBanned = 0,
                    DateJoin = DateTime.UtcNow,
                    DateActivated = DateTime.UtcNow,
                    DateVerified = DateTime.UtcNow
                });

                db.SaveChanges();
            }
        }
    }
}
