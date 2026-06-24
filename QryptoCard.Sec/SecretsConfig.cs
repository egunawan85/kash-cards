using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace QryptoCard.Sec
{
    /// <summary>
    /// Single, fail-fast accessor for runtime secrets and config. Reads from process
    /// environment variables ONLY — never from source. On the server these are injected
    /// per app-pool from Azure Key Vault; locally they come from deploy/secrets/.env +
    /// .vault (loaded by deploy/scripts/load-env.ps1).
    ///
    /// A missing secret is always a deploy misconfiguration, never a runtime branch:
    /// <see cref="Require"/> throws, and <see cref="Preload"/> (called at
    /// Application_Start) aggregates every missing name so a misconfigured app fails to
    /// start with the complete list rather than failing lazily mid-request.
    /// </summary>
    public static class SecretsConfig
    {
        private static readonly ConcurrentDictionary<string, string> Cache =
            new ConcurrentDictionary<string, string>(StringComparer.Ordinal);

        /// <summary>Value of env var <paramref name="name"/>, or throws if missing/blank. Cached per process.</summary>
        public static string Require(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Secret name must be provided.", "name");
            return Cache.GetOrAdd(name, ReadOrThrow);
        }

        /// <summary>Value if present and non-blank, else <paramref name="fallback"/>. Optional NON-secret config only.</summary>
        public static string GetOptional(string name, string fallback = null)
        {
            string value = Environment.GetEnvironmentVariable(name);
            return string.IsNullOrWhiteSpace(value) ? fallback : value;
        }

        /// <summary>Validates and returns a hex-encoded key of the exact byte length. Use via Preload to fault at startup.</summary>
        public static byte[] RequireHexBytes(string name, int lengthBytes)
        {
            string hex = Require(name);
            if (hex.Length != lengthBytes * 2 || !IsHex(hex))
                throw new InvalidOperationException(
                    "Secret '" + name + "' must be " + (lengthBytes * 2) + " hex chars (" + lengthBytes + " bytes).");
            byte[] bytes = new byte[lengthBytes];
            for (int i = 0; i < lengthBytes; i++)
                bytes[i] = Convert.ToByte(hex.Substring(i * 2, 2), 16);
            return bytes;
        }

        /// <summary>
        /// Eagerly resolves every required name, aggregating ALL missing ones into a single
        /// exception. Call from each app's Application_Start so a misconfigured pool fails fast
        /// with the complete list rather than failing lazily on first use.
        /// </summary>
        public static void Preload(params string[] names)
        {
            if (names == null) return;
            List<string> missing = new List<string>();
            foreach (string name in names)
            {
                string value = Environment.GetEnvironmentVariable(name);
                if (string.IsNullOrWhiteSpace(value)) missing.Add(name);
                else Cache[name] = value;
            }
            if (missing.Count > 0)
                throw new InvalidOperationException(
                    "Required secret(s) not set in the environment: " +
                    string.Join(", ", missing.OrderBy(n => n, StringComparer.Ordinal)) +
                    ". On the server these come from Key Vault via inject-secrets.ps1; " +
                    "locally from deploy/secrets/.vault + .env (run deploy/scripts/load-env.ps1).");
        }

        /// <summary>
        /// TEST-ONLY: clears the per-process secret cache so a test can set an
        /// environment variable and have the next <see cref="Require"/> re-read it.
        /// Never call from production code — secrets are immutable per app-pool.
        /// </summary>
        public static void ResetCacheForTests()
        {
            Cache.Clear();
        }

        private static string ReadOrThrow(string name)
        {
            string value = Environment.GetEnvironmentVariable(name);
            if (string.IsNullOrWhiteSpace(value))
                throw new InvalidOperationException("Required secret '" + name + "' is not set in the environment.");
            return value;
        }

        private static bool IsHex(string s)
        {
            foreach (char c in s)
                if (!((c >= '0' && c <= '9') || (c >= 'a' && c <= 'f') || (c >= 'A' && c <= 'F')))
                    return false;
            return true;
        }
    }
}
