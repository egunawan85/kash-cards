using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace QryptoCard.INT.Model.WasabiCard
{
    public class WCAccountInfoResponseModel
    {
        public bool success { get; set; }
        public int code { get; set; }
        public string msg { get; set; }
        public List<Datum> data { get; set; }
        
        public class Datum
        {
            // String, not int: the live merchant accountId is a 19-digit value that overflows Int32
            // (an identifier we never compute on) — int makes account/info fail to deserialize.
            public string accountId { get; set; }
            public string currency { get; set; }
            public string totalBalance { get; set; }
            public string availableBalance { get; set; }
            public string frozenBalance { get; set; }
            public int digital { get; set; }
        }


    }
}