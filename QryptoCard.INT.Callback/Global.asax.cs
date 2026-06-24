using System;
using System.Web;

namespace QryptoCard.INT.Callback
{
    public class Global : HttpApplication
    {
        protected void Application_Start(object sender, EventArgs e)
        {
            // Fail fast if the shared secret protecting the money-tier WCF operations is not
            // provisioned, rather than faulting on the first inbound call.
            QryptoCard.Sec.SecretsConfig.Preload("INT_CALLBACK_SHARED_SECRET");
        }
    }
}
