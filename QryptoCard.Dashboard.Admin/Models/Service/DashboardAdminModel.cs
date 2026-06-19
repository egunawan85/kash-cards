using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace QryptoCard.Dashboard.Admin.Models.Service
{
    public class DashboardAdminModel
    {
        public int TotalUsers { get; set; }
        public double TotalDeposit { get; set; }
        public int TotalCards { get; set; }
    }
}