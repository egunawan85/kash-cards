using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace QryptoCard.INT.Model.WasabiCard
{
    public class WCCityListRequestModel
    {
        public bool success { get; set; }
        public int code { get; set; }
        public string msg { get; set; }
        public List<Datum> data { get; set; }

        public class Child
        {
            public string code { get; set; }
            public string name { get; set; }
            public string parentCode { get; set; }
            public string country { get; set; }
            public string countryStandardCode { get; set; }
            public List<object> children { get; set; }
        }

        public class Datum
        {
            public string code { get; set; }
            public string name { get; set; }
            public string parentCode { get; set; }
            public string country { get; set; }
            public string countryStandardCode { get; set; }
            public List<Child> children { get; set; }
        }
    }
}