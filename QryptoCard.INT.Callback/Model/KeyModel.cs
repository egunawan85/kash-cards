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
        public static string WASABICARD_API_URL =>
            SecretsConfig.GetOptional("WASABICARD_API_URL", "https://api-merchant.wasabicard.com");

        // --- Secrets / key material (env-only via SecretsConfig; never committed) ---
        public static string WASABICARD_API_KEY => SecretsConfig.Require("WASABICARD_API_KEY");
        public static string WASABICARD_PUBLIC_KEY => SecretsConfig.Require("WASABICARD_PUBLIC_KEY");
        public static string WASABICARD_PRIVATE_KEY => SecretsConfig.Require("WASABICARD_PRIVATE_KEY");
        public static string WASABICARD_PRIVATE_KEY_XML => SecretsConfig.Require("WASABICARD_PRIVATE_KEY_XML");
        public static string WASABICARD_WSBPUBLIC_KEY => SecretsConfig.Require("WASABICARD_WSBPUBLIC_KEY");
    }
}
