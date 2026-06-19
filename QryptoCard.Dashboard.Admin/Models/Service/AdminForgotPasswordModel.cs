using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace QryptoCard.Dashboard.Admin.Models.Service
{
    public class AdminForgotPasswordModel
    {
        public long ID { get; set; }
        public string AdminID { get; set; }
        public string Hash { get; set; }
        public Nullable<int> isVerified { get; set; }
        public Nullable<System.DateTime> DateCreated { get; set; }
        public string CreatedBy { get; set; }
        public Nullable<int> isActive { get; set; }
        public string DeletedBy { get; set; }
        public Nullable<System.DateTime> DateDeleted { get; set; }
        public string Param1 { get; set; }
        public string Param2 { get; set; }
        public string Param3 { get; set; }
    }
}