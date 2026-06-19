using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace QryptoCard.INT.Model.PGCrypto
{
    public class AddressStaticModel
    {
        public string AddressID { get; set; }
        public string CoinID { get; set; }
        public string Coin { get; set; }
        public string Chain { get; set; }
        public string ChainName { get; set; }
        public string ParentName { get; set; }
        public string Symbol { get; set; }
        public string Network { get; set; }
        public string Address { get; set; }
        public Nullable<System.DateTime> DateCreated { get; set; }
        public Nullable<int> isActive { get; set; }
    }
}