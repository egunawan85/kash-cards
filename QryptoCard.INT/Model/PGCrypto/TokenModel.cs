using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace QryptoCard.INT.Model.PGCrypto
{
    public class TokenModel
    {
        public string TokenID { get; set; }
        public string Name { get; set; }
        public string Symbol { get; set; }
        public string Coin { get; set; }
        public string Type { get; set; }
    }
}