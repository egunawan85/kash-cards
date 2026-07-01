namespace QryptoCard.Sec
{
    /// <summary>
    /// Single source of truth for the deposit-into-card INTENT lifecycle status strings, shared across
    /// both tiers so the two constant sets can't drift (the recurring bug class from RT rounds 3/4).
    /// These are the intent table's OWN vocabulary (capitalized) — distinct from the lowercase
    /// StatusModel order statuses. The OPEN set (Pending/Funding/Confirming/Issuing) is what the
    /// one-open-per-user unique index and the settlement matcher gate on.
    /// </summary>
    public static class CardFundingStatuses
    {
        public const string Pending = "Pending";
        public const string Funding = "Funding";
        public const string Confirming = "Confirming";
        public const string Issuing = "Issuing";
        public const string Completed = "Completed";
        public const string Expired = "Expired";
        public const string Cancelled = "Cancelled";
        public const string Failed = "Failed";
    }
}
