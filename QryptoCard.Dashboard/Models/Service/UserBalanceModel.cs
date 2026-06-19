using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace QryptoCard.Dashboard.Models.Service
{
    public class UserBalanceModel
    {
        public long ID { get; set; }
        public string BalanceID { get; set; }
        public string UserID { get; set; }
        public string Currency { get; set; }
        public Nullable<decimal> Balance { get; set; }
        public Nullable<int> isActive { get; set; }
        public Nullable<System.DateTime> DateCreated { get; set; }
        public string CreatedBy { get; set; }
        public Nullable<System.DateTime> DateUpdated { get; set; }
    }
}