namespace QryptoCard.INT.Callback.Model.PGCrypto
{
    /// <summary>
    /// Runegate response envelope: { Status, Message, Data }. Callback-tier copy of the INT
    /// model. Status == "success" is the gateway's own success signal (separate from HTTP 200).
    /// </summary>
    public class PGOutputModel
    {
        public string Status { get; set; }
        public string Message { get; set; }
        public object Data { get; set; }
    }
}
