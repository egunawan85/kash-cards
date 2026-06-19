using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace QryptoCard.INT.Callback.Model.WasabiCard
{
    public class WCDepositCardResponseModel
    {
        public bool success { get; set; }
        public int code { get; set; }
        public string msg { get; set; }
        public Data data { get; set; }

        public class Data
        {
            public string orderNo { get; set; }
            public string merchantOrderNo { get; set; }
            public string cardNo { get; set; }
            public string currency { get; set; }
            public string amount { get; set; }
            public string fee { get; set; }
            public string type { get; set; }
            public string status { get; set; }
            public string remark { get; set; }
            public long transactionTime { get; set; }
        }
    }
}