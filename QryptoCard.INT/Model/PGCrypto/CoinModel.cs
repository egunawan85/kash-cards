using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace QryptoCard.INT.Model.PGCrypto
{
    public class CoinModel
    {
        public string CoinID { get; set; }
        public string ChainName { get; set; }
        public string ParentName { get; set; }
        public string Symbol { get; set; }
        public Nullable<int> isHaveToken { get; set; }
        public string Network { get; set; }
    }
}