using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace QryptoCard.INT.Callback.Model.WasabiCard
{
    public class WCTransactionModel
    {
        public string orderNo { get; set; }
        public string merchantOrderNo { get; set; }
        public string cardNo { get; set; }
        public string currency { get; set; }
        public string amount { get; set; }
        public string fee { get; set; }
        public string receivedAmount { get; set; }
        public string receivedCurrency { get; set; }
        public string type { get; set; }
        public string status { get; set; }
        public object remark { get; set; }
        public long transactionTime { get; set; }
    }
}