using System;

namespace QryptoCard.INT.Callback.Model.PGCrypto
{
    /// <summary>Runegate master-data coin (GET /v1/master/data/coin). Used to resolve the TRON
    /// CoinID (Network == "TRC20") for outbound USDT-TRC20 transfers. Callback-tier copy.</summary>
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
