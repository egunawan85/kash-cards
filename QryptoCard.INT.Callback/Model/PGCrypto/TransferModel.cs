namespace QryptoCard.INT.Callback.Model.PGCrypto
{
    /// <summary>
    /// Body of Runegate POST /v1/transfer — send crypto OUT of our merchant balance. For our
    /// USDT-TRC20 auto-funding: CoinID = TRON, isToken = 1, TokenID = USDT-TRC20, Address = the
    /// WasabiCard deposit address. isFeeIncluded = 0 means the Runegate network fee is charged ON
    /// TOP of Amount (Amount lands on-chain in full; our balance is debited Amount + fee), which
    /// is what the gross-up math assumes. PartnerReferenceID is OUR idempotency key.
    /// </summary>
    public class TransferRequestModel
    {
        public string CoinID { get; set; }
        public int isToken { get; set; }
        public string TokenID { get; set; }
        public decimal Amount { get; set; }
        public string Address { get; set; }
        public int isFeeIncluded { get; set; }
        public string PartnerReferenceID { get; set; }
    }

    /// <summary>
    /// Permissive view of the Runegate transfer record (envelope Data). Field names vary by
    /// provider version and are best-effort only; correctness never depends on parsing this — our
    /// own PartnerReferenceID is the idempotency/lookup key and the full raw response is logged.
    /// </summary>
    public class TransferResultModel
    {
        public string TransferID { get; set; }
        public string ID { get; set; }
        public string Status { get; set; }
        public string PartnerReferenceID { get; set; }
        public decimal? Amount { get; set; }
        public string Address { get; set; }
        public string TxHash { get; set; }
    }

    /// <summary>
    /// Money-safety trichotomy for a submit-transfer attempt — mirrors the card-spend path's
    /// confirmed / definitive-failure / ambiguous handling. NEVER auto-retry an Ambiguous transfer
    /// without reconciliation confirming it did not execute: a timeout/exception can occur AFTER
    /// Runegate accepted and moved the funds, so retrying could double-send.
    /// </summary>
    public class TransferOutcome
    {
        /// <summary>Runegate accepted the transfer (HTTP 200 + envelope Status == "success").</summary>
        public bool Submitted { get; set; }
        /// <summary>Provider definitively rejected it with a parseable response — money did NOT move; safe to fail.</summary>
        public bool DefinitiveReject { get; set; }
        /// <summary>Neither Submitted nor DefinitiveReject (timeout/exception/5xx) — outcome UNKNOWN; money may have moved.</summary>
        public bool Ambiguous => !Submitted && !DefinitiveReject;

        public string EnvelopeStatus { get; set; }
        public string Raw { get; set; }
        public TransferResultModel Result { get; set; }
    }
}
