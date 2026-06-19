using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace QryptoCard.INT.Callback.Model.WasabiCard
{
    public class WCTransaction3DSModel
    {
        public string cardNo { get; set; }
        public string tradeNo { get; set; }
        public object originTradeNo { get; set; }
        public string currency { get; set; }
        public string amount { get; set; }
        public string merchantName { get; set; }
        public string values { get; set; }
        public string type { get; set; }
        public long transactionTime { get; set; }
        public object description { get; set; }
        public long expirationTime { get; set; }
    }
}