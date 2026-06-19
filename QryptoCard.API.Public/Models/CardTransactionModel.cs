using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace QryptoCard.API.Public.Models
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
        public string Status { get; set; }
        public string Description { get; set; }
        public Nullable<System.DateTime> TransactionTime { get; set; }
    }
}