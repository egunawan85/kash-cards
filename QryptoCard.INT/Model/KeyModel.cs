using QryptoCard.Sec;

namespace QryptoCard.INT.Model
{
    /// <summary>
    /// Runtime config + secret accessors. Secret/key material is read from the process
    /// environment via <see cref="SecretsConfig"/> (never hardcoded in source); non-secret
    /// URLs remain literals. The dev/prod split is controlled by QRYPTO_ENVIRONMENT and the
    /// injected secret values per environment — not by editing this file.
    /// </summary>
    public class KeyModel
    {
        // --- Non-secret config ---
        public static string PGCRYPTO_API_URL = "https://api.runegate.co";
        public static string QRYPTO_PAY_URL = "https://pay-otc.qrypto.trade/pay?id=";
        public static string WASABICARD_API_URL = "https://sandbox-api-merchant.wasabicard.com";
        public static string QRYPTO_URL_FORGOT_PASSWORD = "https://kash.cards/newpassword?id=";
        public static string QRYPTO_ENVIRONMENT => SecretsConfig.GetOptional("QRYPTO_ENVIRONMENT", "dev");

        // Email delivery via Postmark SMTP. The From (EMAIL_ADDRESS) must be a Postmark-verified
        // sender signature; the SMTP login (EMAIL_SMTP_USER, below) is the Postmark Server API
        // Token, distinct from the From. Non-secret, env-overridable; defaults target Postmark.
        public static string EMAIL_ADDRESS => SecretsConfig.GetOptional("EMAIL_FROM", "no-reply@kash.cards");
        public static string EMAIL_SMTP_GATEWAY => SecretsConfig.GetOptional("EMAIL_SMTP_GATEWAY", "smtp.postmarkapp.com");
        public static int EMAIL_SMTP_PORT => System.Convert.ToInt32(SecretsConfig.GetOptional("EMAIL_SMTP_PORT", "587"));

        // --- Secrets / key material (env-only via SecretsConfig; never committed) ---
        public static string PGCRYPTO_API_KEY => SecretsConfig.Require("PGCRYPTO_API_KEY");
        public static string PGCRYPTO_SECRET_KEY => SecretsConfig.Require("PGCRYPTO_SECRET_KEY");
        public static string WASABICARD_API_KEY => SecretsConfig.Require("WASABICARD_API_KEY");
        public static string WASABICARD_PUBLIC_KEY => SecretsConfig.Require("WASABICARD_PUBLIC_KEY");
        public static string WASABICARD_PRIVATE_KEY => SecretsConfig.Require("WASABICARD_PRIVATE_KEY");
        public static string WASABICARD_PRIVATE_KEY_XML => SecretsConfig.Require("WASABICARD_PRIVATE_KEY_XML");
        public static string WASABICARD_WSBPUBLIC_KEY => SecretsConfig.Require("WASABICARD_WSBPUBLIC_KEY");
        // Postmark SMTP login — the Server API Token, used as BOTH the SMTP username and password
        // (Postmark's convention), and kept separate from EMAIL_ADDRESS so the From is a verified
        // sender signature distinct from the auth credential.
        public static string EMAIL_SMTP_USER => SecretsConfig.Require("POSTMARK_SERVER_TOKEN");
        public static string EMAIL_PASSWORD => SecretsConfig.Require("POSTMARK_SERVER_TOKEN");
    }
}
