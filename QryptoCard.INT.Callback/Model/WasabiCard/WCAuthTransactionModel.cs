using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace QryptoCard.INT.Callback.Model.WasabiCard
{
    public class WCAuthTransactionModel
    {
        public string cardNo { get; set; }
        public string tradeNo { get; set; }
        public object originTradeNo { get; set; }
        public string currency { get; set; }
        public string amount { get; set; }
        public string authorizedAmount { get; set; }
        public string authorizedCurrency { get; set; }
        public string fee { get; set; }
        public string feeCurrency { get; set; }
        public string crossBoardFee { get; set; }
        public object crossBoardFeeCurrency { get; set; }
        public string settleAmount { get; set; }
        public object settleCurrency { get; set; }
        public object settleDate { get; set; }
        public string merchantName { get; set; }
        public string type { get; set; }
        public string typeStr { get; set; }
        public string status { get; set; }
        public string statusStr { get; set; }
        public long transactionTime { get; set; }
        public string description { get; set; }
    }
}