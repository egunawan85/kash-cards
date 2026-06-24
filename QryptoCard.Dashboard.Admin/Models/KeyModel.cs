using QryptoCard.Sec;

namespace QryptoCard.Dashboard.Admin.Models
{
    /// <summary>
    /// Admin dashboard config + secret accessors. Secret/key material is read from the process
    /// environment via <see cref="SecretsConfig"/> (never hardcoded); non-secret URLs remain
    /// literals.
    /// </summary>
    public class KeyModel
    {
        // --- Non-secret config (environment-specific URLs) ---
        public static string API_URL = "https://api-admin-dev.qrypto.cards";
        public static string PAYMENT_LINK = "https://pay.qrypto.trade/payment.aspx?id=";
        public static string REFERRAL_URL = "http://localhost:50316/register?id=";
        public static string DETAIL_URL = "http://localhost:50316/card/carddetail?id=";
        public static string DETAIL_OWN_URL = "http://localhost:50316/card/mycarddetail?id=";
        public static string TXCARD_URL = "http://localhost:50316/txcard?id=";
        public static string ADMIN_EMAIL = "syapril@qrypto.trade";

        // --- Secrets / key material (env-only via SecretsConfig; never committed) ---
        public static string APPKEY => SecretsConfig.Require("APPKEY");
        public static string WASABICARD_PRIVATE_KEY_XML => SecretsConfig.Require("WASABICARD_PRIVATE_KEY_XML");

        // Dead legacy default credential (vestigial; never used on a live path). Externalised
        // so no credential blob remains in source; slated for deletion with the admin swap.
        public static string ADMIN_PASSWORD => SecretsConfig.GetOptional("ADMIN_PASSWORD", "");
    }
}
