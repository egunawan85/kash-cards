using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace QryptoCard.API.Public
{
    public class ApiSecurity
    {
        public static bool VaidateAPI(string api, string sect)
        {
            AuthService.SecurityServiceClient ase = new AuthService.SecurityServiceClient();
            return ase.validateAPI(api, sect);
        }
    }
}