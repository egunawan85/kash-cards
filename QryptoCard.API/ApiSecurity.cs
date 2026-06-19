using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace QryptoCard.API
{
    public class ApiSecurity
    {
        public static bool VaidateUser(string email, string password)
        {
            AuthService.SecurityServiceClient ase = new AuthService.SecurityServiceClient();
            return ase.validateUser(email, password);
        }
    }
}