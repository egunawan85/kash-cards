using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Http;
using System.Web.Routing;

namespace QryptoCard.API.Scheduler
{
    public class WebApiApplication : System.Web.HttpApplication
    {
        protected void Application_Start()
        {
            // Pin outbound TLS to 1.2 (no weak-protocol fallback); certificate validation is the
            // framework default now that the accept-all cert-bypass has been removed process-wide.
            System.Net.ServicePointManager.SecurityProtocol = System.Net.SecurityProtocolType.Tls12;

            GlobalConfiguration.Configure(WebApiConfig.Register);
        }
    }
}
