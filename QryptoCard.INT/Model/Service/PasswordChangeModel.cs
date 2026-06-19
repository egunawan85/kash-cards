using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace QryptoCard.INT.Model.Service
{
    public class PasswordChangeModel
    {
        public string AdminID { get; set; }
        public string UserID { get; set; }
        public string CurrentPassword { get; set; }
        public string Password { get; set; }
    }
}