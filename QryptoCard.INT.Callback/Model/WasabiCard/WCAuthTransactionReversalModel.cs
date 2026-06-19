using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace QryptoCard.INT.Callback.Model.WasabiCard
{
    public class WCAuthTransactionReversalModel
    {
        public string cardNo { get; set; }
        public string tradeNo { get; set; }
        public string originTradeNo { get; set; }
        public string currency { get; set; }
        public string amount { get; set; }
        public string type { get; set; }
        public string deductionSourceFunds { get; set; }
        public string status { get; set; }
        public string statusStr { get; set; }
        public long transactionTime { get; set; }
    }
}