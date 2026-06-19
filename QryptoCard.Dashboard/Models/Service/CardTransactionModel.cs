using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace QryptoCard.Dashboard.Models.Service
{
    public class CardTransactionModel
    {
        public long ID { get; set; }
        public string CardNo { get; set; }
        public string TradeNo { get; set; }
        public string OriginTradeNo { get; set; }
        public string Currency { get; set; }
        public Nullable<double> Amount { get; set; }
        public Nullable<double> AuthorizedAmount { get; set; }
        public string AuthorizedCurrency { get; set; }
        public Nullable<double> Fee { get; set; }
        public string FeeCurrency { get; set; }
        public Nullable<double> CrossBoardFee { get; set; }
        public string CrossBoardFeeCurrency { get; set; }
        public Nullable<double> SettleAmount { get; set; }
        public string SettleCurrency { get; set; }
        public Nullable<System.DateTime> SettleDate { get; set; }
        public string MerchantName { get; set; }
        public string Type { get; set; }
        public string TypeStr { get; set; }
        public string Status { get; set; }
        public string StatusStr { get; set; }
        public string Description { get; set; }
        public Nullable<System.DateTime> TransactionTime { get; set; }
        public string Payload { get; set; }
        public string Param1 { get; set; }
        public string Param2 { get; set; }
        public string Param3 { get; set; }
    }
}