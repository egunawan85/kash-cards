using System;
using System.Globalization;

namespace QryptoCard.Sec
{
    // Outcome of an independent, post-signature-verification cross-check of a money-bearing
    // webhook claim against the provider's canonical record. The signature gate (verified at the
    // callback edge) already rejects forgery and body tampering; this second gate is
    // defense-in-depth and replay mitigation, so that even a validly-signed-but-replayed or
    // otherwise-untrustworthy webhook cannot move money unless the provider's own records agree.
    //
    // Pure logic — no DB, no HTTP — so it is unit-testable in isolation.
    public enum CrossCheckOutcome
    {
        // The provider's canonical record confirms the webhook's claim. Safe to act.
        Confirmed,

        // The canonical record contradicts the claim (e.g. the webhook says a deposit "failed"
        // but the provider says it succeeded, or the amounts disagree). Treat as forgery/tamper:
        // do NOT credit.
        Mismatch,

        // The claim could not be confirmed: no canonical record yet, provider unreachable, or the
        // operation is still pending/unknown. Do NOT credit; leave the item in place for the
        // provider's retry / operator reconciliation.
        Unavailable
    }

    public static class WebhookCrossCheckEvaluator
    {
        // Statuses WasabiCard reports for an operation that terminated unsuccessfully.
        private static bool IsFailureStatus(string status)
        {
            if (string.IsNullOrWhiteSpace(status)) return false;
            switch (status.Trim().ToLowerInvariant())
            {
                case "fail":
                case "failed":
                case "cancel":
                case "canceled":
                case "cancelled":
                case "reject":
                case "rejected":
                    return true;
                default:
                    return false;
            }
        }

        // Statuses WasabiCard reports for an operation that terminated successfully.
        private static bool IsSuccessStatus(string status)
        {
            if (string.IsNullOrWhiteSpace(status)) return false;
            switch (status.Trim().ToLowerInvariant())
            {
                case "success":
                case "succeed":
                case "succeeded":
                case "completed":
                    return true;
                default:
                    return false;
            }
        }

        // Decimal-equal comparison of two provider amount strings (invariant culture). A value
        // that does not parse never matches — an unreadable amount is never treated as equal.
        public static bool AmountsMatch(string a, string b)
        {
            decimal da, db;
            if (!decimal.TryParse(a, NumberStyles.Number, CultureInfo.InvariantCulture, out da)) return false;
            if (!decimal.TryParse(b, NumberStyles.Number, CultureInfo.InvariantCulture, out db)) return false;
            return da == db;
        }

        // Gate a deposit-failure refund. The webhook claims a card deposit FAILED, which would
        // credit a refund back to the user's platform balance. The refund VALUE is taken from our
        // own deposit record (not from the webhook or the provider response), so the load-bearing
        // question is purely whether the deposit really failed at the provider (fail-closed):
        //   - provider confirms a failure state            -> Confirmed
        //   - provider says the deposit actually succeeded -> Mismatch  (forged/replayed "fail")
        //   - no record / pending / unknown / unreachable  -> Unavailable
        //
        // Amount is deliberately NOT part of this gate: because the refund value comes from our
        // record, a provider/webhook amount discrepancy cannot change what is credited, and making
        // it blocking would risk silently withholding legitimate refunds if the provider's webhook
        // and REST amount fields ever differ in basis. Callers log such a discrepancy (AmountsMatch)
        // as an advisory anomaly instead.
        public static CrossCheckOutcome EvaluateDepositRefund(string canonicalStatus)
        {
            if (IsSuccessStatus(canonicalStatus)) return CrossCheckOutcome.Mismatch;
            if (IsFailureStatus(canonicalStatus)) return CrossCheckOutcome.Confirmed;
            return CrossCheckOutcome.Unavailable;
        }

        /// <summary>
        /// Purpose-named provider-status classifier for the reconciliation sweep (decoupled from the
        /// deposit-refund framing): true = provider reports success, false = provider reports a
        /// definitive failure, null = unknown / pending / empty / unreachable (fail-closed → no action).
        /// </summary>
        public static bool? ClassifyProviderStatus(string status)
        {
            if (IsSuccessStatus(status)) return true;
            if (IsFailureStatus(status)) return false;
            return null;
        }

        // Gate a card-open credit. The webhook claims a card was created/funded under our order.
        // The credited balance is read independently from the provider's card-info response (never
        // from the webhook), so confirmation here is simply that the provider returns the SAME card
        // we recorded for the order:
        //   - provider returns the expected card number -> Confirmed
        //   - provider returns a different card number   -> Mismatch
        //   - no card returned / unreachable             -> Unavailable
        public static CrossCheckOutcome EvaluateCardOpen(string expectedCardNo, string canonicalCardNo)
        {
            if (string.IsNullOrWhiteSpace(expectedCardNo) || string.IsNullOrWhiteSpace(canonicalCardNo))
                return CrossCheckOutcome.Unavailable;
            return string.Equals(expectedCardNo.Trim(), canonicalCardNo.Trim(), StringComparison.Ordinal)
                ? CrossCheckOutcome.Confirmed
                : CrossCheckOutcome.Mismatch;
        }
    }
}
