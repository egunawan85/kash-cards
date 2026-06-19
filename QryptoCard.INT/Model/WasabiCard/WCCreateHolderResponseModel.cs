using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace QryptoCard.INT.Model.WasabiCard
{
    public class WCCreateHolderResponseModel
    {
        public bool success { get; set; }
        public int code { get; set; }
        public string msg { get; set; }
        public Data data { get; set; }

        public class Data
        {
            public int holderId { get; set; }
            public string status { get; set; }
            public string statusStr { get; set; }
        }
    }
}