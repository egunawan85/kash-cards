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
            // String, not int: the live merchant accountId is a 19-digit value that overflows Int32
            // (and it's an identifier we never compute on) — typing it int makes the whole
            // account/info response fail to deserialize, so the float read returns null (read_fail).
            public string accountId { get; set; }
            public string currency { get; set; }
            public string totalBalance { get; set; }
            public string availableBalance { get; set; }
            public string frozenBalance { get; set; }
            public int digital { get; set; }
        }
    }
}
