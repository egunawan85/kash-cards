using System;

namespace QryptoCard.INT.Security
{
    // One-way password hashing for the auth path. Replaces the old reversible
    // QryptoCard.Sec.Secure.APPtoDB/DBtoAPP/EncryptDB scheme, which stored
    // passwords under a symmetric key (recoverable if the key leaked) and
    // compared by ciphertext equality.
    //
    // bcrypt is a one-way, salted, deliberately-slow hash; each call produces a
    // distinct 60-char string and verification is constant-time by construction.
    //
    // Work factor 12 = ~250 ms on a modern server CPU. Calibrate on prod hardware
    // if login latency becomes a concern. Password storage columns must be at
    // least varchar(60) (a bcrypt hash is 60 chars).
    public static class PasswordHasher
    {
        private const int WorkFactor = 12;

        // Precomputed bcrypt hash of a throwaway string. VerifyWithUniformTiming
        // runs a real bcrypt verify against this when the account lookup missed,
        // so caller latency does not reveal whether the account exists (otherwise
        // the ~250 ms bcrypt cost is an account-existence timing oracle). MUST be a
        // valid bcrypt hash, or the verify short-circuits and the equalization is lost.
        private const string DummyHash = "$2a$12$abcdefghijklmnopqrstuuGVpZPz6aO6kRtMNRBzGT7SjxL6yYtBK";

        public static string Hash(string plaintext)
        {
            if (string.IsNullOrEmpty(plaintext))
                throw new ArgumentException("plaintext must not be empty", nameof(plaintext));
            return BCrypt.Net.BCrypt.HashPassword(plaintext, WorkFactor);
        }

        public static bool Verify(string plaintext, string storedHash)
        {
            if (string.IsNullOrEmpty(plaintext) || string.IsNullOrEmpty(storedHash))
                return false;
            try
            {
                return BCrypt.Net.BCrypt.Verify(plaintext, storedHash);
            }
            catch (BCrypt.Net.SaltParseException)
            {
                // Stored value isn't a bcrypt hash: a forced-reset sentinel that
                // isn't valid bcrypt, a legacy ciphertext row not yet migrated, or
                // a corrupted column. Fail closed.
                return false;
            }
        }

        // Entry point for auth-path validators. Always runs a bcrypt verify —
        // against DummyHash when the account lookup missed — so the caller's
        // latency does not reveal whether the account exists.
        public static bool VerifyWithUniformTiming(string plaintext, string storedHashOrNull)
        {
            if (string.IsNullOrEmpty(storedHashOrNull))
            {
                Verify(plaintext ?? string.Empty, DummyHash);
                return false;
            }
            return Verify(plaintext, storedHashOrNull);
        }
    }
}
