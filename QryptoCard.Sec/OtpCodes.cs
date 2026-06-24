using System;
using System.Security.Cryptography;
using System.Text;

namespace QryptoCard.Sec
{
    /// <summary>
    /// One-time code generation and verification. Codes are generated with a CSPRNG (replacing
    /// the hardcoded "000000"), persisted only as a base64 SHA-256 hash — 44 chars, so it fits
    /// the legacy nvarchar(50) Code column with no schema change — and compared in constant time.
    /// </summary>
    public static class OtpCodes
    {
        /// <summary>A numeric one-time code of the given length, from a CSPRNG with no modulo bias.</summary>
        public static string Generate(int digits = 6)
        {
            if (digits < 1 || digits > 9) throw new ArgumentOutOfRangeException("digits");
            uint bound = 1;
            for (int i = 0; i < digits; i++) bound *= 10;          // 10^digits, e.g. 1_000_000
            uint limit = uint.MaxValue - (uint.MaxValue % bound);  // rejection threshold removes bias
            byte[] buf = new byte[4];
            uint value;
            using (var rng = RandomNumberGenerator.Create())
            {
                do
                {
                    rng.GetBytes(buf);
                    value = BitConverter.ToUInt32(buf, 0);
                } while (value >= limit);
            }
            return (value % bound).ToString(new string('0', digits));
        }

        /// <summary>Base64 SHA-256 of the code — what gets persisted (never the plaintext code).</summary>
        public static string Hash(string code)
        {
            if (code == null) throw new ArgumentNullException("code");
            using (var sha = SHA256.Create())
                return Convert.ToBase64String(sha.ComputeHash(Encoding.UTF8.GetBytes(code)));
        }

        /// <summary>Constant-time check that <paramref name="providedCode"/> hashes to <paramref name="storedHash"/>.</summary>
        public static bool Verify(string providedCode, string storedHash)
        {
            if (string.IsNullOrEmpty(providedCode) || string.IsNullOrEmpty(storedHash)) return false;
            return SharedSecretAuth.FixedTimeEquals(Hash(providedCode), storedHash);
        }

        /// <summary>True if the code is past its expiry, or has no expiry set (fail-closed).</summary>
        public static bool IsExpired(DateTime? expiresAt, DateTime now)
        {
            return expiresAt == null || now > expiresAt.Value;
        }
    }
}
