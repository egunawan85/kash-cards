using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace QryptoCard.INT.Model.Service
{
    public class DepositFilterModel
    {
        public string UserID { get; set; }
        public int CardTypeId { get; set; }
        public string CardNo { get; set; }
        public string Status { get; set; }
        public Nullable<System.DateTime> StartDate { get; set; }
        public Nullable<System.DateTime> EndDate { get; set; }
    }
}