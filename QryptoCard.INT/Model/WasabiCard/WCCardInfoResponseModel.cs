using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace QryptoCard.INT.Model.WasabiCard
{
    public class WCCardInfoResponseModel
    {
        public bool success { get; set; }
        public int code { get; set; }
        public string msg { get; set; }
        public Data data { get; set; }

        public class BalanceInfo
        {
            public string cardNo { get; set; }
            public string amount { get; set; }
            public string usedAmount { get; set; }
            public string currency { get; set; }
        }

        public class Data
        {
            public int holderId { get; set; }
            public string cardNo { get; set; }
            public string cardNumber { get; set; }
            public string cvv { get; set; }
            public string validPeriod { get; set; }
            public string status { get; set; }
            public string statusStr { get; set; }
            public long bindTime { get; set; }
            public BalanceInfo balanceInfo { get; set; }
        }
    }
}