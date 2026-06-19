using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace QryptoCard.INT.Callback.Model.WasabiCard
{
    public class WCCardInfoRequestModel
    {
        public string cardNo { get; set; }
        public bool onlySimpleInfo { get; set; }
    }
}