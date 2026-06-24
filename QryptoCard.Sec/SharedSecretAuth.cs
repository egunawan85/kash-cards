using System.Text;

namespace QryptoCard.Sec
{
    /// <summary>
    /// Shared-secret header authentication. Pure and fail-closed: returns true only when the
    /// presented value matches the configured secret, compared in constant time. Factored out so
    /// the auth decision is unit-testable without a WCF host.
    /// </summary>
    public static class SharedSecretAuth
    {
        /// <summary>
        /// True iff <paramref name="providedHeader"/> equals the environment secret named
        /// <paramref name="secretName"/>. A missing/blank presented value is rejected; a missing
        /// configured secret throws (fail-closed) via <see cref="SecretsConfig.Require"/>.
        /// </summary>
        public static bool IsAuthorized(string providedHeader, string secretName)
        {
            if (string.IsNullOrEmpty(providedHeader)) return false;
            string expected = SecretsConfig.Require(secretName);
            return FixedTimeEquals(providedHeader, expected);
        }

        /// <summary>Constant-time string comparison (length-folded; no early exit on content mismatch).</summary>
        public static bool FixedTimeEquals(string a, string b)
        {
            if (a == null || b == null) return false;
            byte[] ba = Encoding.UTF8.GetBytes(a);
            byte[] bb = Encoding.UTF8.GetBytes(b);
            int diff = ba.Length ^ bb.Length;
            int n = ba.Length < bb.Length ? ba.Length : bb.Length;
            for (int i = 0; i < n; i++) diff |= ba[i] ^ bb[i];
            return diff == 0;
        }
    }
}
