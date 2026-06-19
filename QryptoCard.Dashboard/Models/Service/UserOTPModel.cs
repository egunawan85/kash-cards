using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace QryptoCard.Dashboard.Models.Service
{
    public class UserOTPModel
    {
        public string OTPID { get; set; }
        public string UserID { get; set; }
        public string Code { get; set; }
    }
}