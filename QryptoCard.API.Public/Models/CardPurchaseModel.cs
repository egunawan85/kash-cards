using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace QryptoCard.API.Public.Models
{
    public class CardPurchaseModel
    {
        public string ID { get; set; }
        public Nullable<long> CardTypeId { get; set; }
        public Nullable<long> HolderID { get; set; }
        public string CardNumber { get; set; }
        public string Currency { get; set; }
        public Nullable<double> Price { get; set; }
        public Nullable<double> InitialDeposit { get; set; }
        public Nullable<double> Fee { get; set; }
        public Nullable<double> FeeInPercentage { get; set; }
        public Nullable<double> Total { get; set; }
        public Nullable<double> ReceivedAmount { get; set; }
        public string Status { get; set; }
        public Nullable<System.DateTime> DateCreated { get; set; }
        public Nullable<System.DateTime> DateExpired { get; set; }
        public Nullable<int> isActive { get; set; }
        public Nullable<System.DateTime> DateModified { get; set; }
        public Nullable<int> isNeedCardholder { get; set; }
        public string Address { get; set; }
        public string ReceiptURL { get; set; }
        public string Txhash { get; set; }
        public string UserReferenceID { get; set; }
    }
}