using System;
using System.Data.SqlClient;
using System.IO;
using System.Text;
using QryptoCard.Sec;

namespace QryptoCard.DevSeed
{
    // Seeds the minimal master/config rows + a seedable admin + a pre-seeded smoke
    // API user into a FRESH dev database (schema already published). Idempotent:
    // deletes its own seed identities before inserting. See the .csproj header for
    // the password/secret encryption contract.
    internal static class Program
    {
        // Stable logical keys so re-runs are idempotent.
        private const string UserRoleId   = "role-owner";
        private const string AdminRoleId  = "role-admin";
        private const string AdminEmail   = "edward@s16.ventures";
        private const string SmokeUserId  = "11111111-1111-1111-1111-111111111111";
        private const string SmokeEmail   = "smoke-user@kash.cards";
        private const string NetworkId    = "F580A411-0E37-4287-B975-408172A2B4BF"; // TRC20 (dev fake-address path)
        private const long   CardTypeId   = 111028;

        private static int Main(string[] args)
        {
            try
            {
                string conn = args.Length > 0 ? args[0]
                    : Environment.GetEnvironmentVariable("SEED_CONNECTION");
                if (string.IsNullOrWhiteSpace(conn))
                    return Fail("usage: QryptoCard.DevSeed \"<connection-string>\" [smoke-env-out]\n" +
                               "       (or set SEED_CONNECTION). DBKEY and APPKEY must be in the environment.");

                // Fail fast with the full list if the crypto keys are missing.
                SecretsConfig.Preload("DBKEY", "APPKEY");

                string smokeOut = args.Length > 1 ? args[1]
                    : Environment.GetEnvironmentVariable("SMOKE_ENV_OUT")
                    ?? "deploy/secrets/.smoke.env";

                // Dev credentials: overridable, else deterministic dev defaults.
                string adminPwd = Environment.GetEnvironmentVariable("SEED_ADMIN_PASSWORD") ?? "KashAdmin!dev1";
                string userPwd  = Environment.GetEnvironmentVariable("SEED_USER_PASSWORD")  ?? "KashUser!dev1";

                // Smoke API credential: fresh each run; the secret's wire form
                // (EncryptAPP) is what the smoke client sends, the DB form (EncryptDB)
                // is what validateAPI compares against.
                string apiKey       = "smoke-" + Guid.NewGuid().ToString("N");
                string apiSecret    = Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N");
                string apiSecretDb   = Secure.EncryptDB(apiSecret);   // stored
                string apiSecretWire = Secure.EncryptAPP(apiSecret);  // sent by client
                string adminPwdWire  = Secure.EncryptAPP(adminPwd);   // sent by admin client

                using (var c = new SqlConnection(conn))
                {
                    c.Open();
                    using (var tx = c.BeginTransaction())
                    {
                        Cleanup(c, tx);
                        SeedRoles(c, tx);
                        SeedSettings(c, tx);
                        SeedCardType(c, tx);
                        SeedNetwork(c, tx);
                        SeedAdmin(c, tx, Secure.EncryptDB(adminPwd));
                        SeedUser(c, tx, Secure.EncryptDB(userPwd));
                        SeedUserChildren(c, tx, apiKey, apiSecretDb);
                        tx.Commit();
                    }
                }

                WriteSmokeEnv(smokeOut, apiKey, apiSecretWire, adminPwdWire);
                // Do NOT print credentials to stdout: the orchestrator streams this
                // through `az vm run-command invoke`, whose output is retained in the
                // Azure control plane / Activity Log. Credentials live only in the
                // gitignored .smoke.env file.
                Console.WriteLine("[ok] seed complete.");
                Console.WriteLine("[ok] admin seeded: " + AdminEmail);
                Console.WriteLine("[ok] smoke + admin credentials written to: " + smokeOut);
                return 0;
            }
            catch (Exception ex)
            {
                return Fail(ex.GetType().Name + ": " + ex.Message);
            }
        }

        private static int Fail(string msg) { Console.Error.WriteLine("[xx] " + msg); return 1; }

        // -- helpers ------------------------------------------------------------
        private static int Exec(SqlConnection c, SqlTransaction tx, string sql, params (string, object)[] ps)
        {
            using (var cmd = new SqlCommand(sql, c, tx))
            {
                foreach (var p in ps) cmd.Parameters.AddWithValue(p.Item1, p.Item2 ?? DBNull.Value);
                return cmd.ExecuteNonQuery();
            }
        }

        private static void Cleanup(SqlConnection c, SqlTransaction tx)
        {
            Exec(c, tx, "DELETE FROM dbo.tblM_User_API WHERE UserID=@u", ("@u", SmokeUserId));
            Exec(c, tx, "DELETE FROM dbo.tblM_User_Crypto_Deposit WHERE UserID=@u", ("@u", SmokeUserId));
            Exec(c, tx, "DELETE FROM dbo.tblM_User_Referral WHERE UserID=@u", ("@u", SmokeUserId));
            Exec(c, tx, "DELETE FROM dbo.tblM_User_Commission WHERE UserID=@u", ("@u", SmokeUserId));
            Exec(c, tx, "DELETE FROM dbo.tblM_User_Balance WHERE UserID=@u", ("@u", SmokeUserId));
            Exec(c, tx, "DELETE FROM dbo.tblM_User WHERE UserID=@u OR Email=@e", ("@u", SmokeUserId), ("@e", SmokeEmail));
            Exec(c, tx, "DELETE FROM dbo.tblM_Admin WHERE Email=@e", ("@e", AdminEmail));
            Exec(c, tx, "DELETE FROM dbo.tblM_User_Role WHERE RoleID=@r", ("@r", UserRoleId));
            Exec(c, tx, "DELETE FROM dbo.tblM_Admin_Role WHERE RoleID=@r", ("@r", AdminRoleId));
            Exec(c, tx, "DELETE FROM dbo.tblM_Card_Type WHERE CardTypeId=@t", ("@t", CardTypeId));
            Exec(c, tx, "DELETE FROM dbo.tblM_Crypto_Network WHERE ID=@n", ("@n", NetworkId));
            Exec(c, tx, "DELETE FROM dbo.tblM_Setting WHERE ID=2");
            Exec(c, tx, "DELETE FROM dbo.tblM_Setting_Counter WHERE ID IN (1,2)");
        }

        private static void SeedRoles(SqlConnection c, SqlTransaction tx)
        {
            Exec(c, tx, "INSERT INTO dbo.tblM_User_Role (RoleID, Role, isActive, DateCreated) VALUES (@r,'Owner',1,GETUTCDATE())", ("@r", UserRoleId));
            Exec(c, tx, "INSERT INTO dbo.tblM_Admin_Role (RoleID, Role, isActive, DateCreated) VALUES (@r,'Admin',1,GETUTCDATE())", ("@r", AdminRoleId));
        }

        private static void SeedSettings(SqlConnection c, SqlTransaction tx)
        {
            // Code reads the commission at tblM_Setting ID=2 and the counters at
            // tblM_Setting_Counter ID=1/2, so force the IDs via IDENTITY_INSERT.
            Exec(c, tx, "SET IDENTITY_INSERT dbo.tblM_Setting ON; " +
                        "INSERT INTO dbo.tblM_Setting (ID, Name, Value, DateCreated) VALUES (2,'DefaultCommissionRate','0.1',GETUTCDATE()); " +
                        "SET IDENTITY_INSERT dbo.tblM_Setting OFF;");
            Exec(c, tx, "SET IDENTITY_INSERT dbo.tblM_Setting_Counter ON; " +
                        "INSERT INTO dbo.tblM_Setting_Counter (ID, Name, Value, DateCreated) VALUES (1,'CardCounter','1000',GETUTCDATE()); " +
                        "INSERT INTO dbo.tblM_Setting_Counter (ID, Name, Value, DateCreated) VALUES (2,'DepositCounter','5000',GETUTCDATE()); " +
                        "SET IDENTITY_INSERT dbo.tblM_Setting_Counter OFF;");
        }

        private static void SeedCardType(SqlConnection c, SqlTransaction tx)
        {
            Exec(c, tx,
                "INSERT INTO dbo.tblM_Card_Type (CardTypeId, CardName, CardPrice, CardPriceCurrency, RechargeFeeRate, FiatCurrency, Status, isActive, DateCreated) " +
                "VALUES (@t,'Virtual Card','10.00','USD','2.5','USD','active',1,GETUTCDATE())", ("@t", CardTypeId));
        }

        private static void SeedNetwork(SqlConnection c, SqlTransaction tx)
        {
            Exec(c, tx, "INSERT INTO dbo.tblM_Crypto_Network (ID, Network, Symbol, isActive, DateCreated) VALUES (@n,'TRC20','USDT',1,GETUTCDATE())", ("@n", NetworkId));
        }

        private static void SeedAdmin(SqlConnection c, SqlTransaction tx, string pwdDb)
        {
            Exec(c, tx,
                "INSERT INTO dbo.tblM_Admin (AdminID, Email, FirstName, LastName, Password, RoleID, Phone, isActive, isVerified, isBanned, DateJoin) " +
                "VALUES (@id,@e,'Edward','Admin',@p,@r,'+10000000000',1,1,0,GETUTCDATE())",
                ("@id", Guid.NewGuid().ToString()), ("@e", AdminEmail), ("@p", pwdDb), ("@r", AdminRoleId));
        }

        private static void SeedUser(SqlConnection c, SqlTransaction tx, string pwdDb)
        {
            Exec(c, tx,
                "INSERT INTO dbo.tblM_User (UserID, Email, FirstName, LastName, Password, RoleID, Phone, isActive, isVerified, isBanned, DateJoin) " +
                "VALUES (@id,@e,'Smoke','User',@p,@r,'+10000000001',1,1,0,GETUTCDATE())",
                ("@id", SmokeUserId), ("@e", SmokeEmail), ("@p", pwdDb), ("@r", UserRoleId));
        }

        private static void SeedUserChildren(SqlConnection c, SqlTransaction tx, string apiKey, string apiSecretDb)
        {
            Exec(c, tx, "INSERT INTO dbo.tblM_User_Balance (BalanceID, UserID, Currency, Balance, isActive, DateCreated) VALUES (@b,@u,'USDT',0,1,GETUTCDATE())",
                ("@b", Guid.NewGuid().ToString()), ("@u", SmokeUserId));
            Exec(c, tx, "INSERT INTO dbo.tblM_User_Commission (CommissionID, UserID, Commission, DateCreated) VALUES (@cm,@u,0.1,GETUTCDATE())",
                ("@cm", Guid.NewGuid().ToString()), ("@u", SmokeUserId));
            Exec(c, tx, "INSERT INTO dbo.tblM_User_Referral (UserID, Code, DateCreated) VALUES (@u,@code,GETUTCDATE())",
                ("@u", SmokeUserId), ("@code", Guid.NewGuid().ToString("N").Substring(0, 8).ToUpperInvariant()));
            Exec(c, tx, "INSERT INTO dbo.tblM_User_Crypto_Deposit (ID, UserID, NetworkID, Address, isActive, DateCreated) VALUES (@id,@u,@n,@addr,1,GETUTCDATE())",
                ("@id", Guid.NewGuid().ToString()), ("@u", SmokeUserId), ("@n", NetworkId), ("@addr", "T" + Guid.NewGuid().ToString("N").Substring(0, 12)));
            Exec(c, tx, "INSERT INTO dbo.tblM_User_API (UserID, APIKey, SecretKey, isActive, DateCreated) VALUES (@u,@k,@s,1,GETUTCDATE())",
                ("@u", SmokeUserId), ("@k", apiKey), ("@s", apiSecretDb));
        }

        private static void WriteSmokeEnv(string path, string apiKey, string apiSecretWire, string adminPwdWire)
        {
            var dir = Path.GetDirectoryName(Path.GetFullPath(path));
            if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
            var sb = new StringBuilder();
            sb.AppendLine("# Generated by QryptoCard.DevSeed. GITIGNORED. Credentials for the smoke E2E.");
            sb.AppendLine("# SMOKE_BASE_URL: fill with the deployed public API base (e.g. https://public-dev.kash.cards).");
            sb.AppendLine("SMOKE_BASE_URL=");
            sb.AppendLine("SMOKE_API_KEY=" + apiKey);
            sb.AppendLine("# SMOKE_API_SECRET is the wire form (EncryptAPP); used as the Basic-auth password.");
            sb.AppendLine("SMOKE_API_SECRET=" + apiSecretWire);
            sb.AppendLine("SMOKE_ADMIN_EMAIL=" + AdminEmail);
            sb.AppendLine("# SMOKE_ADMIN_PASSWORD is the wire form (EncryptAPP) the admin client sends.");
            sb.AppendLine("SMOKE_ADMIN_PASSWORD=" + adminPwdWire);
            File.WriteAllText(path, sb.ToString());
        }
    }
}
