using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace QryptoCard.INT.Model.WasabiCard
{
    public class WCCardInfoSensitiveResponseModel
    {
        public bool success { get; set; }
        public int code { get; set; }
        public string msg { get; set; }
        public Data data { get; set; }


        public class Data
        {
            public string cardNumber { get; set; }
            public string cvv { get; set; }
            public string expireDate { get; set; }
            public string activeUrl { get; set; }
        }
    }
}