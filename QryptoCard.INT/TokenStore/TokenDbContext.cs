using System.Data.Entity;

namespace QryptoCard.INT.TokenStore
{
    /// <summary>
    /// Standalone EF6 code-first context for the two token tables, separate from the EDMX
    /// DBEntities (so no EDMX hand-edit). The initializer is disabled — the schema is owned by
    /// deploy/sql/create-token-tables.sql, not by code-first migrations.
    /// </summary>
    public class TokenDbContext : DbContext
    {
        static TokenDbContext()
        {
            Database.SetInitializer<TokenDbContext>(null);
        }

        // Takes a raw SQL Server connection string (NOT the EDMX metadata-wrapped one).
        public TokenDbContext(string connectionString) : base(connectionString) { }

        public DbSet<AuthTokenRow> AuthTokens { get; set; }
        public DbSet<RefreshTokenRow> RefreshTokens { get; set; }
    }
}
