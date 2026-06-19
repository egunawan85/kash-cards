using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace QryptoCard.Dashboard.Admin.Models.Service
{
    public class PasswordChangeModel
    {
        public string AdminID { get; set; }
        public string CurrentPassword { get; set; }
        public string Password { get; set; }
    }
}