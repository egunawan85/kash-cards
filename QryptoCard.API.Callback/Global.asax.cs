using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Http;
using System.Web.Routing;

namespace QryptoCard.API.Callback
{
    public class WebApiApplication : System.Web.HttpApplication
    {
        protected void Application_Start()
        {
            // Pin outbound TLS to 1.2 (no weak-protocol fallback); certificate validation is the
            // framework default now that the accept-all cert-bypass has been removed process-wide.
            System.Net.ServicePointManager.SecurityProtocol = System.Net.SecurityProtocolType.Tls12;

            // Fail fast if the webhook-verification secrets are not provisioned, rather than
            // returning 500 on every inbound callback.
            QryptoCard.Sec.SecretsConfig.Preload("WASABICARD_WSBPUBLIC_KEY", "PGCRYPTO_WEBHOOK_SECRET", "INT_CALLBACK_SHARED_SECRET", "SCHEDULER_SHARED_SECRET");

            GlobalConfiguration.Configure(WebApiConfig.Register);
        }
    }
}
