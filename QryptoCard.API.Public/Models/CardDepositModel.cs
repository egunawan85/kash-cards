using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace QryptoCard.API.Public.Models
{
    public class CardDepositModel
    {
        public string ID { get; set; }
        public string Currency { get; set; }
        public Nullable<double> Amount { get; set; }
        public Nullable<double> Fee { get; set; }
        public Nullable<double> FeeInPercentage { get; set; }
        public Nullable<double> Total { get; set; }
        public Nullable<double> ReceivedAmount { get; set; }
        public string Type { get; set; }
        public string Status { get; set; }
        public string Remark { get; set; }
        public Nullable<System.DateTime> DateTransaction { get; set; }
        public Nullable<System.DateTime> DateReceived { get; set; }
        public Nullable<System.DateTime> DateExpired { get; set; }
        public string Address { get; set; }
        public string ReceiptURL { get; set; }
        public string Txhash { get; set; }
        public string UserReferenceID { get; set; }
    }
}