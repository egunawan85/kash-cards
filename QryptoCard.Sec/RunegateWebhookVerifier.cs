using System;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace QryptoCard.Sec
{
    /// <summary>
    /// Verifies inbound Runegate / PGCrypto webhook signatures (Stripe-style).
    /// Header "X-Runegate-Signature" = "t=&lt;unix-seconds&gt;,v1=&lt;hex(HMAC_SHA256(key, ts + "." + rawBody))&gt;".
    /// Pure and fail-closed: returns false on any malformed/expired/forged input, never throws.
    /// Verifies over the EXACT raw request bytes, uses a constant-time compare, enforces a
    /// freshness window, and rejects a too-weak signing secret.
    /// </summary>
    public static class RunegateWebhookVerifier
    {
        public const int DefaultToleranceSeconds = 300; // 5 minutes
        private const int MinSecretBytes = 32;          // 256-bit minimum

        /// <summary>Verify against the current time.</summary>
        public static bool Verify(string signatureHeader, byte[] rawBody, string secret,
                                  int toleranceSeconds = DefaultToleranceSeconds)
        {
            return Verify(signatureHeader, rawBody, secret,
                          DateTimeOffset.UtcNow.ToUnixTimeSeconds(), toleranceSeconds);
        }

        /// <summary>Verify with an explicit current time (unit-testable).</summary>
        public static bool Verify(string signatureHeader, byte[] rawBody, string secret,
                                  long nowUnixSeconds, int toleranceSeconds)
        {
            if (string.IsNullOrEmpty(signatureHeader) || rawBody == null || string.IsNullOrEmpty(secret))
                return false;

            byte[] secretBytes = Encoding.UTF8.GetBytes(secret);
            if (secretBytes.Length < MinSecretBytes) return false;

            long ts; string v1;
            if (!TryParseHeader(signatureHeader, out ts, out v1)) return false;
            if (Math.Abs(nowUnixSeconds - ts) > toleranceSeconds) return false;

            byte[] provided = FromHex(v1);
            if (provided == null || provided.Length != 32) return false;

            // Signed input is the ASCII timestamp, a literal '.', then the raw body bytes.
            byte[] prefix = Encoding.UTF8.GetBytes(ts.ToString(CultureInfo.InvariantCulture) + ".");
            byte[] signedInput = new byte[prefix.Length + rawBody.Length];
            Buffer.BlockCopy(prefix, 0, signedInput, 0, prefix.Length);
            Buffer.BlockCopy(rawBody, 0, signedInput, prefix.Length, rawBody.Length);

            byte[] expected;
            using (var hmac = new HMACSHA256(secretBytes))
                expected = hmac.ComputeHash(signedInput);

            return ConstantTimeEquals(expected, provided);
        }

        private static bool TryParseHeader(string header, out long ts, out string v1)
        {
            ts = 0; v1 = null;
            foreach (string part in header.Split(','))
            {
                int eq = part.IndexOf('=');
                if (eq <= 0) continue;
                string k = part.Substring(0, eq).Trim();
                string val = part.Substring(eq + 1).Trim();
                if (k == "t")
                {
                    if (!long.TryParse(val, NumberStyles.None, CultureInfo.InvariantCulture, out ts)) return false;
                }
                else if (k == "v1")
                {
                    v1 = val;
                }
            }
            return ts > 0 && !string.IsNullOrEmpty(v1);
        }

        // Manual hex decode (avoids NumberStyles.HexNumber whitespace/sign quirks).
        private static byte[] FromHex(string hex)
        {
            if (string.IsNullOrEmpty(hex) || (hex.Length & 1) != 0) return null;
            byte[] b = new byte[hex.Length / 2];
            for (int i = 0; i < b.Length; i++)
            {
                int hi = HexVal(hex[i * 2]);
                int lo = HexVal(hex[i * 2 + 1]);
                if (hi < 0 || lo < 0) return null;
                b[i] = (byte)((hi << 4) | lo);
            }
            return b;
        }

        private static int HexVal(char c)
        {
            if (c >= '0' && c <= '9') return c - '0';
            if (c >= 'a' && c <= 'f') return c - 'a' + 10;
            if (c >= 'A' && c <= 'F') return c - 'A' + 10;
            return -1;
        }

        private static bool ConstantTimeEquals(byte[] a, byte[] b)
        {
            if (a == null || b == null) return false;
            int diff = a.Length ^ b.Length;
            int n = Math.Min(a.Length, b.Length);
            for (int i = 0; i < n; i++) diff |= a[i] ^ b[i];
            return diff == 0;
        }
    }
}
