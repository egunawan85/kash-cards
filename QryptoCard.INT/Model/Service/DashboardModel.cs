using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace QryptoCard.INT.Model.Service
{
    public class DashboardModel
    {
        public double CommissionRate { get; set; }
        public double TotalCommission { get; set; }
        public int TotalCards { get; set; }
        public double TotalTopupTransaction { get; set; }
        public double AmountSpentThisMonth { get; set; }
    }
}