using System;
using System.Web;

namespace QryptoCard.INT
{
    public class Global : HttpApplication
    {
        protected void Application_Start(object sender, EventArgs e)
        {
            // Pin outbound TLS to 1.2 (no weak-protocol fallback); certificate validation is the
            // framework default now that the accept-all cert-bypass has been removed process-wide.
            System.Net.ServicePointManager.SecurityProtocol = System.Net.SecurityProtocolType.Tls12;

            // Fail fast if the Postmark token used to send password-reset / OTP / invitation email
            // is not provisioned, rather than faulting on the first email send.
            //
            // NOTE: this tier's other required secrets (WASABICARD_*, PGCRYPTO_*,
            // AUTH_SERVICE_REVOKE_TOKEN) still resolve lazily on first use via SecretsConfig.Require
            // — there was no Application_Start here before this. Folding them into this Preload is a
            // deliberate follow-up, kept out of this change so the email migration stays reviewable
            // and the money/auth tier's startup behaviour isn't altered in the same diff.
            QryptoCard.Sec.SecretsConfig.Preload("POSTMARK_SERVER_TOKEN");
        }
    }
}
