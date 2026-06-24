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

        public static string EMAIL_ADDRESS = "no-reply@qrypto.trade";
        public static string EMAIL_SMTP_GATEWAY = "smtp.gmail.com";
        public static int EMAIL_SMTP_PORT = 587;

        // --- Secrets / key material (env-only via SecretsConfig; never committed) ---
        public static string PGCRYPTO_API_KEY => SecretsConfig.Require("PGCRYPTO_API_KEY");
        public static string PGCRYPTO_SECRET_KEY => SecretsConfig.Require("PGCRYPTO_SECRET_KEY");
        public static string WASABICARD_API_KEY => SecretsConfig.Require("WASABICARD_API_KEY");
        public static string WASABICARD_PUBLIC_KEY => SecretsConfig.Require("WASABICARD_PUBLIC_KEY");
        public static string WASABICARD_PRIVATE_KEY => SecretsConfig.Require("WASABICARD_PRIVATE_KEY");
        public static string WASABICARD_PRIVATE_KEY_XML => SecretsConfig.Require("WASABICARD_PRIVATE_KEY_XML");
        public static string WASABICARD_WSBPUBLIC_KEY => SecretsConfig.Require("WASABICARD_WSBPUBLIC_KEY");
        public static string EMAIL_PASSWORD => SecretsConfig.Require("EMAIL_PASSWORD");
    }
}
