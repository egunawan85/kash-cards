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
            // Fail fast at startup if required secrets are not provisioned in the environment
            // (this tier encrypts credentials with APPKEY). Missing values throw with the full list.
            QryptoCard.Sec.SecretsConfig.Preload("APPKEY");

            // Code that runs on application startup
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);
        }
    }
}