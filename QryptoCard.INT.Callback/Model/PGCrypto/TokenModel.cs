namespace QryptoCard.INT.Callback.Model.PGCrypto
{
    /// <summary>Runegate master-data token (GET /v1/master/data/token/{network}). Used to resolve
    /// the USDT TokenID on TRC20 (Symbol == "USDT") for outbound transfers. Callback-tier copy.</summary>
    public class TokenModel
    {
        public string TokenID { get; set; }
        public string Name { get; set; }
        public string Symbol { get; set; }
        public string Coin { get; set; }
        public string Type { get; set; }
    }
}
