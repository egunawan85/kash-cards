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
        // so it can't be relative. PUBLIC_BASE_URL is the cardholder app host
        // (app-<env>.kash.cards), the same key the INT reset link uses. Required (no silent
        // default): the prior dash-dev.kash.cards default mapped to no route and silently
        // shipped a wrong host, so — like the INT reset link — require it and fault if unset.
        public static string REFERRAL_URL =>
            SecretsConfig.Require("PUBLIC_BASE_URL").TrimEnd('/') + "/register?id=";

        // In-app navigation targets — root-relative so a tile click / post-buy redirect
        // stays on whatever host is serving the page (previously hardcoded to dev).
        public static string DETAIL_URL = "/card/carddetail?id=";
        public static string DETAIL_OWN_URL = "/card/mycarddetail?id=";
        public static string TXCARD_URL = "/txcard?id=";

        // Deposit-into-card CUSTOMER-UI switch (front-end only). Ships OFF ("0"): every existing page
        // renders exactly as today. Flip this to "1" IN TANDEM with the backend CardFundingStreamingEnabled
        // to turn on the "Total card balance" relabel, the referrals "Available balance" line, the card-list
        // "In progress" section, and the txdeposit -> fund-card redirect. Separate flag because the web tier
        // has no DB access to read the backend setting; keeping it explicit avoids a half-on UI.
        public static bool CARD_FUNDING_UI_ENABLED
        {
            get
            {
                string v = SecretsConfig.GetOptional("CARD_FUNDING_UI_ENABLED", "0");
                if (string.IsNullOrWhiteSpace(v)) return false;
                v = v.Trim();
                return v == "1" || string.Equals(v, "true", System.StringComparison.OrdinalIgnoreCase);
            }
        }

        // --- Secrets / key material (env-only via SecretsConfig; never committed) ---
        public static string WASABICARD_PRIVATE_KEY_XML => SecretsConfig.Require("WASABICARD_PRIVATE_KEY_XML");
    }
}
