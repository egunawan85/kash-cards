using System.Collections.Generic;

namespace QryptoCard.INT.Callback.Model.WasabiCard
{
    /// <summary>
    /// Response of POST /merchant/core/mcb/account/info. The merchant USD wallet float that
    /// card opens/top-ups draw against. availableBalance/frozenBalance arrive as decimal strings.
    /// Callback-tier copy of the INT-tier model (same gateway-copy convention as WasabiCardService).
    /// </summary>
    public class WCAccountInfoResponseModel
    {
        public bool success { get; set; }
        public int code { get; set; }
        public string msg { get; set; }
        public List<Datum> data { get; set; }

        public class Datum
        {
            public int accountId { get; set; }
            public string currency { get; set; }
            public string totalBalance { get; set; }
            public string availableBalance { get; set; }
            public string frozenBalance { get; set; }
            public int digital { get; set; }
        }
    }
}
