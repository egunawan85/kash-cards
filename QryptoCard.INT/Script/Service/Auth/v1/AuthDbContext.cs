using System.Data.Entity;
using System.Data.Entity.ModelConfiguration.Conventions;

namespace QryptoCard.INT.Script.Service.Auth.v1
{
    // Separate code-first DbContext for the three auth-token tables. Lives
    // alongside (NOT inside) the legacy DBEntities .edmx — that context is
    // Database-First and throws UnintentionalCodeFirstException on
    // OnModelCreating, so adding new entities to it requires a Visual Studio
    // "Update Model from Database" regen pass. AuthDbContext sidesteps that
    // entirely: code-first, explicit mapping, no .edmx.
    //
    //   - new tables stay in their own EF model (clean blast radius)
    //   - tests can spin up just AuthDbContext without booting the 100+ DbSet
    //     DBEntities monster
    //
    // Connection string: SEPARATE entry "AuthDbEntities" in web.config / app.config.
    // Cannot reuse the legacy "DBEntities" name because that connection string is
    // in EntityConnection format (metadata=res://*/DB.csdl|... wrapping a SqlClient
    // provider string) which only the .edmx-bound DBEntities context understands.
    // Code-first DbContext needs a plain ADO.NET SqlClient connection string.
    // Both connection strings point at the SAME physical database; no cross-
    // context joins are needed because Subject is a value-typed pointer (not an FK).
    //
    // Schema management: Database.SetInitializer<AuthDbContext>(null) tells
    // EF6 to NEVER auto-create or auto-migrate. The schema must already exist,
    // applied via deploy/sql/create-token-tables.sql at fresh-redeploy time.
    public class AuthDbContext : DbContext
    {
        public const string ConnectionStringName = "AuthDbEntities";

        static AuthDbContext()
        {
            // Disable EF schema management. Schema is owned by the SQL migration
            // file; EF is read-only at the schema level.
            Database.SetInitializer<AuthDbContext>(null);
        }

        public AuthDbContext()
            : base("name=" + ConnectionStringName)
        {
        }

        // Test fixtures that target a non-default connection string (e.g.
        // QryptoCard.Tests.Fixtures.LocalDbFixture pointing at TestQryptoCard)
        // construct via a plain ADO.NET SqlClient connection string. Mirror the
        // constructor shape DBEntities exposes for that case.
        public AuthDbContext(string nameOrConnectionString)
            : base(nameOrConnectionString)
        {
        }

        public virtual DbSet<tblT_AuthToken> tblT_AuthToken { get; set; }
        public virtual DbSet<tblT_RefreshToken> tblT_RefreshToken { get; set; }
        public virtual DbSet<tblH_Auth_Log> tblH_Auth_Log { get; set; }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            // Default EF6 convention pluralizes table names (DbSet<tblT_AuthToken>
            // -> "tblT_AuthTokens"). Our [Table] attributes already pin the
            // singular form, but stripping the convention removes any surprise
            // for entities that might be added without [Table].
            modelBuilder.Conventions.Remove<PluralizingTableNameConvention>();
            base.OnModelCreating(modelBuilder);
        }
    }
}
