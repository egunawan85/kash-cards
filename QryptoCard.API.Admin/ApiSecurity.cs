using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace QryptoCard.API.Admin
{
    public class ApiSecurity
    {
        public static bool VaidateAdmin(string phone, string password)
        {
            AuthService.SecurityServiceClient ase = new AuthService.SecurityServiceClient();
            return ase.validateAdmin(phone, password);
        }
        public static bool ValidateUserAID(string phone, string pass)
        {
            //HttpContext httpContext = HttpContext.Current;
            //string aid = httpContext.Request.Headers["AID"];
            //if (aid == null || aid == "")
            //    return false;

            //AuthService.SecurityClient ase = new AuthService.SecurityClient();
            //return ase.validateAID(phone, aid);
            return true;
        }

        public static bool checkAID()
        {
            //AuthService.SecurityClient ase = new AuthService.SecurityClient();
            //// Gets header parameters  
            //HttpContext httpContext = HttpContext.Current;
            //string aid = httpContext.Request.Headers["AID"];
            //if (aid == null || aid == "")
            //    return false;

            //string authenticationString = httpContext.Request.Headers["Authorization"];
            //string originalString = Encoding.UTF8.GetString(Convert.FromBase64String(authenticationString.Split(' ')[1]));
            //string phone = originalString.Split(':')[0];

            //var a = ase.validateAID(phone, aid);
            //return a;

            return true;
        }
    }
}