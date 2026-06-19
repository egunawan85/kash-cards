using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace QryptoCard.API.Public.Models
{
    public class CardActiveModel
    {
        public string ID { get; set; }
        public Nullable<long> CardTypeId { get; set; }
        public Nullable<long> HolderID { get; set; }
        public string CardNumber { get; set; }
        public string Currency { get; set; }
        public string Status { get; set; }
        public Nullable<System.DateTime> DateCreated { get; set; }
        public Nullable<int> isActive { get; set; }
        public Nullable<int> isNeedCardholder { get; set; }
        public string UserReferenceID { get; set; }
    }
}