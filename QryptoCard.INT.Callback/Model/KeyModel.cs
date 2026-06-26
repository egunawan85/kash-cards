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
    }
}
