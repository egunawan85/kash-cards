using System;
using System.Web;

namespace QryptoCard.INT.Callback
{
    public class Global : HttpApplication
    {
        protected void Application_Start(object sender, EventArgs e)
        {
            // Pin outbound TLS to 1.2 (no weak-protocol fallback); certificate validation is the
            // framework default now that the accept-all cert-bypass has been removed process-wide.
            System.Net.ServicePointManager.SecurityProtocol = System.Net.SecurityProtocolType.Tls12;

            // Fail fast if the shared secret protecting the money-tier WCF operations, or the
            // Postmark token used to send the transaction-failed notification, is not provisioned —
            // rather than faulting on the first inbound call / first email send.
            QryptoCard.Sec.SecretsConfig.Preload("INT_CALLBACK_SHARED_SECRET", "POSTMARK_SERVER_TOKEN");
        }
    }
}
