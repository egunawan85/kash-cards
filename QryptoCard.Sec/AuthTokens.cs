using System;
using System.Security.Cryptography;
using System.Text;

namespace QryptoCard.Sec
{
    /// <summary>
    /// Opaque bearer tokens (Runegate parity): a high-entropy random token is shown to the client
    /// ONCE and persisted only as a SHA-256 hash, looked up and compared in constant time. Access
    /// tokens are short-lived; refresh tokens are long-lived and rotated on each use. The token
    /// carries no data — all authority (subject, type, expiry, revocation) lives in the stored row.
    /// </summary>
    public static class AuthTokens
    {
        public const string AccessPrefix = "at_";
        public const string RefreshPrefix = "rt_";

        public static readonly TimeSpan AccessLifetime = TimeSpan.FromMinutes(15);
        public static readonly TimeSpan RefreshLifetime = TimeSpan.FromDays(7);

        private const int TokenBytes = 32; // 256 bits of entropy

        public static string NewAccessToken() { return AccessPrefix + RandomToken(); }
        public static string NewRefreshToken() { return RefreshPrefix + RandomToken(); }

        /// <summary>SHA-256 (hex) of the token — what gets persisted; the raw token is never stored.</summary>
        public static string Hash(string token)
        {
            if (token == null) throw new ArgumentNullException("token");
            using (var sha = SHA256.Create())
            {
                byte[] h = sha.ComputeHash(Encoding.UTF8.GetBytes(token));
                var sb = new StringBuilder(h.Length * 2);
                for (int i = 0; i < h.Length; i++) sb.Append(h[i].ToString("x2"));
                return sb.ToString();
            }
        }

        /// <summary>Constant-time check that a presented token hashes to a stored hash.</summary>
        public static bool Matches(string presentedToken, string storedHash)
        {
            if (string.IsNullOrEmpty(presentedToken) || string.IsNullOrEmpty(storedHash)) return false;
            return SharedSecretAuth.FixedTimeEquals(Hash(presentedToken), storedHash);
        }

        /// <summary>True if the token row is expired (or has no expiry) or has been revoked. Fail-closed.</summary>
        public static bool IsActive(DateTime? expiresAt, DateTime? revokedAt, DateTime now)
        {
            if (revokedAt != null) return false;
            if (expiresAt == null) return false;
            return now <= expiresAt.Value;
        }

        private static string RandomToken()
        {
            byte[] b = new byte[TokenBytes];
            using (var rng = RandomNumberGenerator.Create()) rng.GetBytes(b);
            // base64url (no padding) -> safe in URLs and Authorization headers.
            return Convert.ToBase64String(b).TrimEnd('=').Replace('+', '-').Replace('/', '_');
        }
    }
}
