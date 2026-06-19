using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace QryptoCard.Dashboard.Admin.Models.Service
{
    public class UserFeeModel
    {
        public long ID { get; set; }
        public string FeeID { get; set; }
        public string UserID { get; set; }
        public string Email { get; set; }
        public Nullable<double> Fee { get; set; }
        public Nullable<System.DateTime> DateCreated { get; set; }
        public string CreatedBy { get; set; }
        public Nullable<System.DateTime> DateUpdated { get; set; }
        public string UpdatedBy { get; set; }
        public Nullable<System.DateTime> DateDeleted { get; set; }
        public string DeletedBy { get; set; }
        public string Param1 { get; set; }
        public string Param2 { get; set; }
        public string Param3 { get; set; }
        public string Param4 { get; set; }
        public string Param5 { get; set; }
    }
}