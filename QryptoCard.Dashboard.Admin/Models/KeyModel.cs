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
        // Backend (admin-tier) API base. Config-driven so each deployment points at its
        // own backend; defaults to the on-box loopback tier (QryptoCard.API.Admin, port
        // 8083) which is how the admin dashboard reaches it server-side (backend stays internal).
        public static string API_URL => SecretsConfig.GetOptional("API_URL", "http://127.0.0.1:8083");
        public static string PAYMENT_LINK = "https://pay.qrypto.trade/payment.aspx?id=";
        public static string REFERRAL_URL = "http://localhost:50316/register?id=";
        public static string DETAIL_URL = "http://localhost:50316/card/carddetail?id=";
        public static string DETAIL_OWN_URL = "http://localhost:50316/card/mycarddetail?id=";
        public static string TXCARD_URL = "http://localhost:50316/txcard?id=";

        // --- Secrets / key material (env-only via SecretsConfig; never committed) ---
        public static string WASABICARD_PRIVATE_KEY_XML => SecretsConfig.Require("WASABICARD_PRIVATE_KEY_XML");
    }
}
