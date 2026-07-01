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

    /// <summary>
    /// The Runegate INVOICE payment statuses (as delivered on the deposit webhook) — a SEPARATE
    /// vocabulary from the intent lifecycle above. Centralized here (with the covered-predicate) so
    /// the one place that decides "does this webhook credit the wallet + advance the intent" can't
    /// drift between the two tiers.
    /// </summary>
    public static class InvoicePaymentStatus
    {
        public const string PartiallyPaid = "PartiallyPaid";
        public const string OverPaid = "OverPaid";
        public const string Completed = "Completed";

        /// <summary>
        /// TRUE iff the invoice is fully covered — i.e. the deposit should credit the wallet and
        /// advance the intent (Completed = exact, OverPaid = paid-and-then-some). A PartiallyPaid
        /// (or any unknown/blank status) is NOT covered: record received-so-far, stay Pending, do
        /// not credit. Case-insensitive; null/empty is safely not-covered.
        /// </summary>
        public static bool IsCovered(string status)
        {
            if (string.IsNullOrWhiteSpace(status)) return false;
            return string.Equals(status, Completed, System.StringComparison.OrdinalIgnoreCase)
                || string.Equals(status, OverPaid, System.StringComparison.OrdinalIgnoreCase);
        }
    }
}
