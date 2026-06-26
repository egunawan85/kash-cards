using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace QryptoCard.Dashboard.Models.Service
{
    // Shape of the JSON returned by GET /v1/user/deposit/address. The static per-user TRC20
    // USDT deposit address (Plan 07), plus the network/coin metadata needed to render a QR.
    public class DepositAddressModel
    {
        public string Address { get; set; }
        public string NetworkID { get; set; }
        public string Network { get; set; }
        public string Coin { get; set; }
    }
}
