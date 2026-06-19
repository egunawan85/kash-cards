using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace QryptoCard.Dashboard.Admin.Models.Service
{
    public class AdminOTPModel
    {
        public string OTPID { get; set; }
        public string AdminID { get; set; }
        public string MerchantID { get; set; }
        public string Code { get; set; }
    }
}