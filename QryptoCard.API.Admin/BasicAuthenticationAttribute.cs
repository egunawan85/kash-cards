using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net;
using System.Text;
using System.Web;
using System.Web.Http.Filters;

namespace QryptoCard.API.Admin
{
    public class BasicAuthenticationAttribute : AuthorizationFilterAttribute
    {
        public override void OnAuthorization(System.Web.Http.Controllers.HttpActionContext actionContext)
        {
            if (actionContext.Request.Headers.Authorization == null)
            {
                actionContext.Response = actionContext.Request.CreateResponse(HttpStatusCode.Unauthorized);
            }
            else
            {
                // Gets header parameters  
                string authenticationString = actionContext.Request.Headers.Authorization.Parameter;
                string originalString = Encoding.UTF8.GetString(Convert.FromBase64String(authenticationString));

                //byte[] toencodeasbytes = ASCIIEncoding.ASCII.GetBytes("akbarmc:akbarmc");
                //string returnvalue = Convert.ToBase64String(toencodeasbytes);

                // Gets username and password  
                string username = originalString.Split(':')[0];
                string password = originalString.Split(':')[1];
                //string key = originalString.Replace(":", "");

                // Validate username and password  
                //if (!ApiSecurity.VaidateUser(username, password))
                if (!ApiSecurity.VaidateAdmin(username, password))
                {
                    //returns unauthorized error
                    actionContext.Response = actionContext.Request.CreateResponse(HttpStatusCode.Unauthorized);
                    actionContext.Response.Content = new StringContent("{\"invalid_grant\" : \"Your credential is incorrect.\"}", Encoding.UTF8, "application/json");
                }
            }

            base.OnAuthorization(actionContext);
        }
    }
}