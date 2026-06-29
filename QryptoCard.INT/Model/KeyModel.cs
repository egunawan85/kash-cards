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
        // The WasabiCard base URL is the ONLY switch between the test (sandbox) and real-money
        // (prod) environment — the same credentials authenticate against both — so it must never
        // carry a silent default that could quietly route real money to the wrong host. It is
        // Required (no fallback): a box missing WASABICARD_API_URL faults rather than guessing.
        // The deploy plumbing injects it per app-pool (inject-secrets.ps1, seeded from
        // deploy/secrets/.env), so requiring it is safe.
        public static string WASABICARD_API_URL => SecretsConfig.Require("WASABICARD_API_URL");
        // The password-reset link must point at the environment's OWN cardholder host, not a
        // hardcoded prod literal — a dev box was emailing https://kash.cards/newpassword reset
        // links (a dev-issued token is useless on the prod domain). PUBLIC_BASE_URL is the
        // cardholder app host (app-<env>.kash.cards); it is the same key the Dashboard
        // REFERRAL_URL uses. Required (no silent default): a reset link carries an auth token,
        // so a wrong-host default is unacceptable — exactly like WASABICARD_API_URL above, a box
        // missing PUBLIC_BASE_URL faults rather than emailing a wrong-host link. The deploy
        // plumbing injects it per app-pool (inject-secrets.ps1 $ConfigNames, seeded from
        // deploy/secrets/.env), so requiring it is safe.
        public static string QRYPTO_URL_FORGOT_PASSWORD =>
            SecretsConfig.Require("PUBLIC_BASE_URL").TrimEnd('/') + "/newpassword?id=";
        // The admin-invitation link must point at the environment's OWN admin-dashboard host
        // (admin-<env>.kash.cards), not the dead hardcoded admin-dev.qrypto.trade:88 literal.
        // ADMIN_BASE_URL is the QryptoCard.Dashboard.Admin host — distinct from the cardholder
        // PUBLIC_BASE_URL above. Required (no silent default): the invite carries an auth token,
        // so a wrong-host default is unacceptable, exactly like the reset link.
        public static string QRYPTO_URL_ADMIN_INVITE =>
            SecretsConfig.Require("ADMIN_BASE_URL").TrimEnd('/') + "/InvitedAccount?id=";
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
