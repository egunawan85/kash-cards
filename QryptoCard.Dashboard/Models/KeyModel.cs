using QryptoCard.Sec;

namespace QryptoCard.Dashboard.Models
{
    /// <summary>
    /// Cardholder dashboard config + secret accessors. Secret/key material is read from the
    /// process environment via <see cref="SecretsConfig"/> (never hardcoded); non-secret URLs
    /// remain literals.
    /// </summary>
    public class KeyModel
    {
        // --- Non-secret config (environment-specific URLs) ---
        // Backend (user-tier) API base. Config-driven so each deployment points at its
        // own backend; defaults to the on-box loopback tier (QryptoCard.API, port 8081)
        // which is how the dashboard reaches it server-side (the backend stays internal).
        public static string API_URL => SecretsConfig.GetOptional("API_URL", "http://127.0.0.1:8081");

        // Public-facing base for links SHARED OUTSIDE the app (the referral/register
        // link a user hands to someone else) — must be an absolute URL with the host,
        // so it can't be relative. Config-driven per deployment; defaults to dev.
        public static string REFERRAL_URL =>
            SecretsConfig.GetOptional("PUBLIC_BASE_URL", "https://dash-dev.kash.cards").TrimEnd('/') + "/register?id=";

        // In-app navigation targets — root-relative so a tile click / post-buy redirect
        // stays on whatever host is serving the page (previously hardcoded to dev).
        public static string DETAIL_URL = "/card/carddetail?id=";
        public static string DETAIL_OWN_URL = "/card/mycarddetail?id=";
        public static string TXCARD_URL = "/txcard?id=";

        // --- Secrets / key material (env-only via SecretsConfig; never committed) ---
        public static string APPKEY => SecretsConfig.Require("APPKEY");
        public static string WASABICARD_PRIVATE_KEY_XML => SecretsConfig.Require("WASABICARD_PRIVATE_KEY_XML");
    }
}
