using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace QryptoCard.INT.Model.WasabiCard
{
    public class WCDepositCardRequestModel
    {
        public string merchantOrderNo { get; set; }
        public string cardNo { get; set; }
        public int amount { get; set; }
    }
}