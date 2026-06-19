using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace QryptoCard.INT.Model.WasabiCard
{
    public class WCCreateHolderRequestModel
    {
        public long cardTypeId { get; set; }
        public string areaCode { get; set; }
        public string mobile { get; set; }
        public string email { get; set; }
        public string firstName { get; set; }
        public string lastName { get; set; }
        public string birthday { get; set; }
        public string country { get; set; }
        public string town { get; set; }
        public string address { get; set; }
        public string postCode { get; set; }
    }
}