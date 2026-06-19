using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace QryptoCard.INT.Model.WasabiCard
{
    public class WCOpenCardRequestModel
    {
        public string merchantOrderNo { get; set; }
        public string cardTypeId { get; set; }
        public int amount { get; set; }
    }
}