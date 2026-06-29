using System.Collections.Generic;

namespace QryptoCard.INT.Callback.Model.WasabiCard
{
    /// <summary>
    /// Response of POST /merchant/core/mcb/wallet/v2/addressList — the crypto deposit addresses
    /// created under our merchant account. The auto-fund pre-flight reads this to confirm the
    /// configured WasabiCard deposit address is still on our account before any money-out transfer.
    /// Same envelope as the other WasabiCard responses (success/code/msg/data); a body we cannot
    /// parse into this shape deserializes with success=false / null data, which the guard treats as
    /// "unverifiable" so the caller fails CLOSED.
    /// Callback-tier copy (same gateway-copy convention as WasabiCardService / WCAccountInfoResponseModel).
    /// </summary>
    public class WCAddressListResponseModel
    {
        public bool success { get; set; }
        public int code { get; set; }
        public string msg { get; set; }
        public List<Datum> data { get; set; }

        public class Datum
        {
            public string coinKey { get; set; }
            public string chain { get; set; }
            public string coinName { get; set; }
            public string address { get; set; }
        }
    }
}
