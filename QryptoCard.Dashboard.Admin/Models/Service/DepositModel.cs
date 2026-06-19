using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace QryptoCard.Dashboard.Admin.Models.Service
{
    public class DepositModel
    {
        public string ID { get; set; }
        public string UserID { get; set; }
        public string Email { get; set; }
        public string OrderNo { get; set; }
        public string CardNo { get; set; }
        public string Network { get; set; }
        public string Symbol { get; set; }
        public string Currency { get; set; }
        public Nullable<double> BaseFee { get; set; }
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
        public string AddressID { get; set; }
        public string Address { get; set; }
        public string ReceiptURL { get; set; }
        public string Txhash { get; set; }
        public string PGCryptoID { get; set; }
        public string Param1 { get; set; }
        public string Param2 { get; set; }
        public string Param3 { get; set; }
        public string Param4 { get; set; }
        public string Param5 { get; set; }
        public string CardNumber { get; set; }
        public Nullable<long> CardTypeId { get; set; }
        public string Organization { get; set; }
        public string CardName { get; set; }
    }
}