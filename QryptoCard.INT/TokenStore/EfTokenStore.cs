using System;
using System.Linq;
using QryptoCard.Sec;

namespace QryptoCard.INT.TokenStore
{
    /// <summary>
    /// EF6 persistence for <see cref="TokenService"/>. A short-lived context per operation (the
    /// legacy DAL style). The raw SQL connection string is injected (sourced from SecretsConfig
    /// where this is constructed), so this type has no config dependency of its own.
    /// </summary>
    public class EfTokenStore : ITokenStore
    {
        private readonly string _conn;

        public EfTokenStore(string connectionString)
        {
            if (string.IsNullOrEmpty(connectionString)) throw new ArgumentNullException("connectionString");
            _conn = connectionString;
        }

        public void SaveAccess(TokenRecord r)
        {
            using (var db = new TokenDbContext(_conn))
            {
                db.AuthTokens.Add(new AuthTokenRow
                {
                    TokenHash = r.TokenHash, SubjectId = r.SubjectId, SubjectType = r.SubjectType,
                    CreatedAt = r.CreatedAt, ExpiresAt = r.ExpiresAt, RevokedAt = r.RevokedAt
                });
                db.SaveChanges();
            }
        }

        public void SaveRefresh(TokenRecord r)
        {
            using (var db = new TokenDbContext(_conn))
            {
                db.RefreshTokens.Add(new RefreshTokenRow
                {
                    TokenHash = r.TokenHash, SubjectId = r.SubjectId, SubjectType = r.SubjectType,
                    CreatedAt = r.CreatedAt, ExpiresAt = r.ExpiresAt, RevokedAt = r.RevokedAt
                });
                db.SaveChanges();
            }
        }

        public TokenRecord FindAccess(string tokenHash)
        {
            using (var db = new TokenDbContext(_conn))
            {
                var row = db.AuthTokens.AsNoTracking().FirstOrDefault(t => t.TokenHash == tokenHash);
                return row == null ? null : new TokenRecord
                {
                    TokenHash = row.TokenHash, SubjectId = row.SubjectId, SubjectType = row.SubjectType,
                    CreatedAt = row.CreatedAt, ExpiresAt = row.ExpiresAt, RevokedAt = row.RevokedAt
                };
            }
        }

        public TokenRecord FindRefresh(string tokenHash)
        {
            using (var db = new TokenDbContext(_conn))
            {
                var row = db.RefreshTokens.AsNoTracking().FirstOrDefault(t => t.TokenHash == tokenHash);
                return row == null ? null : new TokenRecord
                {
                    TokenHash = row.TokenHash, SubjectId = row.SubjectId, SubjectType = row.SubjectType,
                    CreatedAt = row.CreatedAt, ExpiresAt = row.ExpiresAt, RevokedAt = row.RevokedAt
                };
            }
        }

        public void RevokeAccess(string tokenHash, DateTime at)
        {
            using (var db = new TokenDbContext(_conn))
            {
                var row = db.AuthTokens.FirstOrDefault(t => t.TokenHash == tokenHash);
                if (row != null && row.RevokedAt == null) { row.RevokedAt = at; db.SaveChanges(); }
            }
        }

        public void RevokeRefresh(string tokenHash, DateTime at)
        {
            using (var db = new TokenDbContext(_conn))
            {
                var row = db.RefreshTokens.FirstOrDefault(t => t.TokenHash == tokenHash);
                if (row != null && row.RevokedAt == null) { row.RevokedAt = at; db.SaveChanges(); }
            }
        }
    }
}
