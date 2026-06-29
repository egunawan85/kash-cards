using QryptoCard.Sec;

namespace QryptoCard.INT.Callback.Model
{
    /// <summary>
    /// WasabiCard config + secret accessors for the callback tier. Secret/key material is
    /// read from the process environment via <see cref="SecretsConfig"/> (never hardcoded);
    /// the API URL is selected per environment via WASABICARD_API_URL.
    /// </summary>
    public class KeyModel
    {
        // --- Non-secret config ---
        // Required, never defaulted: the WasabiCard base URL is the only sandbox/prod (test vs.
        // real-money) switch and the same credentials work against both, so a silent prod default
        // could quietly route real money to the wrong host. Must match the INT tier — both read
        // the same env var so a single per-pool value moves both tiers together (no split-brain).
        public static string WASABICARD_API_URL => SecretsConfig.Require("WASABICARD_API_URL");

        // --- Secrets / key material (env-only via SecretsConfig; never committed) ---
        public static string WASABICARD_API_KEY => SecretsConfig.Require("WASABICARD_API_KEY");
        public static string WASABICARD_PUBLIC_KEY => SecretsConfig.Require("WASABICARD_PUBLIC_KEY");
        public static string WASABICARD_PRIVATE_KEY => SecretsConfig.Require("WASABICARD_PRIVATE_KEY");
        public static string WASABICARD_PRIVATE_KEY_XML => SecretsConfig.Require("WASABICARD_PRIVATE_KEY_XML");
        public static string WASABICARD_WSBPUBLIC_KEY => SecretsConfig.Require("WASABICARD_WSBPUBLIC_KEY");

        // --- Runegate (PGCrypto) outbound transfer creds, for WasabiCard auto-funding (money-OUT) ---
        // The callback pool already receives PGCRYPTO_WEBHOOK_SECRET for inbound deposit verification;
        // the API key/secret (the same merchant creds the INT tier uses for address provisioning) must
        // also be injected here so this tier can call POST /v1/transfer. URL is env-derived so a
        // sandbox/prod switch can never silently route a real transfer at the wrong host.
        public static string PGCRYPTO_API_URL => SecretsConfig.GetOptional("PGCRYPTO_API_URL", "https://api.runegate.co");
        public static string PGCRYPTO_API_KEY => SecretsConfig.Require("PGCRYPTO_API_KEY");
        public static string PGCRYPTO_SECRET_KEY => SecretsConfig.Require("PGCRYPTO_SECRET_KEY");
    }
}
