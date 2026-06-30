using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace QryptoCard.INT.Model.WasabiCard
{
    public class WCOpenCardResponseModel
    {
        public bool success { get; set; }
        public int code { get; set; }
        public string msg { get; set; }
        public List<Datum> data { get; set; }

        public class Datum
        {
            public string cardNo { get; set; }
            public string orderNo { get; set; }
            public string merchantOrderNo { get; set; }
            public string currency { get; set; }
            public string amount { get; set; }
            public string fee { get; set; }
            public string receivedAmount { get; set; }
            public string receivedCurrency { get; set; }
            public string type { get; set; }
            public string status { get; set; }
            public long transactionTime { get; set; }
        }
    }
}