using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace QryptoCard.Dashboard.Models.Service
{
    public class PasswordChangeModel
    {
        public string UserID { get; set; }
        public string CurrentPassword { get; set; }
        public string Password { get; set; }
    }
}