using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Optimization;
using System.Web.Routing;
using System.Web.Security;
using System.Web.SessionState;

namespace QryptoCard.Dashboard
{
    public class Global : HttpApplication
    {
        void Application_Start(object sender, EventArgs e)
        {
            // Pin outbound TLS to 1.2 (no weak-protocol fallback); certificate validation is the
            // framework default now that the accept-all cert-bypass has been removed process-wide.
            System.Net.ServicePointManager.SecurityProtocol = System.Net.SecurityProtocolType.Tls12;

            // Fail fast at startup if required secrets are not provisioned in the environment
            // (this tier encrypts credentials with APPKEY). Missing values throw with the full list.
            QryptoCard.Sec.SecretsConfig.Preload("APPKEY");

            // Code that runs on application startup
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);
        }
    }
}