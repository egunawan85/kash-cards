using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace QryptoCard.API.Models
{
    public class UserReferralModel
    {
        public string UserID { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public Nullable<System.DateTime> DateJoin { get; set; }

    }
}