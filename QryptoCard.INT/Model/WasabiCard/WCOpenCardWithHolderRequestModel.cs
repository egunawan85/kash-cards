using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace QryptoCard.INT.Model.WasabiCard
{
    public class WCOpenCardWithHolderRequestModel
    {
        public string merchantOrderNo { get; set; }
        public int cardTypeId { get; set; }
        public int holderId { get; set; }
        public int amount { get; set; }
    }
}