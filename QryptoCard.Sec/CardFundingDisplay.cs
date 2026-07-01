using System;
using System.Collections.Generic;
using System.Globalization;

namespace QryptoCard.Sec
{
    /// <summary>
    /// PURE presentation helpers for the deposit-into-card customer UI (the Dashboard). No I/O, no
    /// money math — just the mapping from the intent's lifecycle status onto the THREE user-facing
    /// stages, terminal/failure classification, and the tolerant balance-sum + money formatting the
    /// "Total card balance" figure needs. Lives in Sec (dependency-light) so it is unit-tested and
    /// shared without dragging the web project into the test rig. Status strings come from
    /// <see cref="CardFundingStatuses"/> so the stage map can't drift from the intent vocabulary.
    /// </summary>
    public static class CardFundingDisplay
    {
        // The three stages the customer sees on the tracker, in order.
        public const int StageWaiting = 0; // "Waiting for your deposit"
        public const int StageFunding = 1; // "Funding your card"
        public const int StageReady   = 2; // "Your card is ready"

        public const string LabelWaiting = "Waiting for your deposit";
        public const string LabelFunding = "Funding your card";
        public const string LabelReady   = "Your card is ready";

        /// <summary>
        /// Map an intent status onto its user-facing stage index (0/1/2). A failure/expiry/cancel is
        /// NOT a stage — callers must check <see cref="IsFailure"/> first and show the honest failure
        /// copy; this returns StageWaiting for those so a mis-ordered caller can't imply progress.
        /// Case-insensitive; unknown/blank => StageWaiting.
        /// </summary>
        public static int StageOf(string status)
        {
            if (Is(status, CardFundingStatuses.Completed)) return StageReady;
            if (Is(status, CardFundingStatuses.Funding)
                || Is(status, CardFundingStatuses.Confirming)
                || Is(status, CardFundingStatuses.Issuing)) return StageFunding;
            // Pending, and any failure/unknown (failure is surfaced separately) sit at "waiting".
            return StageWaiting;
        }

        /// <summary>A terminal state — the app can stop polling (Completed or a failure/expiry/cancel).</summary>
        public static bool IsTerminal(string status)
        {
            return Is(status, CardFundingStatuses.Completed) || IsFailure(status);
        }

        /// <summary>A NON-success terminal state (Failed / Expired / Cancelled) — show honest failure copy.</summary>
        public static bool IsFailure(string status)
        {
            return Is(status, CardFundingStatuses.Failed)
                || Is(status, CardFundingStatuses.Expired)
                || Is(status, CardFundingStatuses.Cancelled);
        }

        /// <summary>An OPEN, still-in-flight intent (belongs in the card list's "In progress" section).</summary>
        public static bool IsOpen(string status)
        {
            return Is(status, CardFundingStatuses.Pending)
                || Is(status, CardFundingStatuses.Funding)
                || Is(status, CardFundingStatuses.Confirming)
                || Is(status, CardFundingStatuses.Issuing);
        }

        /// <summary>
        /// Parse the leading numeric amount out of a display balance string — tolerant of the app's
        /// "12.34 USD" / "12.34 USDT" shape, a bare "12.34", thousands separators, surrounding
        /// whitespace, or a leading currency glyph. Returns 0 for null/blank/unparseable. Never throws.
        /// </summary>
        public static decimal ParseAmount(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return 0m;
            var sb = new System.Text.StringBuilder();
            bool seenDigit = false, seenDot = false;
            foreach (char c in raw.Trim())
            {
                if (char.IsDigit(c)) { sb.Append(c); seenDigit = true; continue; }
                if (c == '.' && !seenDot && seenDigit) { sb.Append(c); seenDot = true; continue; }
                if (c == ',') continue;                 // thousands separator: skip
                if (!seenDigit && (c == '-' || c == '+')) continue; // leading sign already normalized away
                if (seenDigit) break;                   // hit the currency suffix -> stop
            }
            decimal v;
            if (decimal.TryParse(sb.ToString(), NumberStyles.Number, CultureInfo.InvariantCulture, out v))
                return v < 0m ? 0m : v;                  // a balance is never negative for display
            return 0m;
        }

        /// <summary>Sum a set of per-card display balances into the "Total card balance". Blank/bad rows count as 0.</summary>
        public static decimal SumBalances(IEnumerable<string> displayBalances)
        {
            decimal total = 0m;
            if (displayBalances == null) return total;
            foreach (var b in displayBalances) total += ParseAmount(b);
            return total;
        }

        /// <summary>Fixed two-decimal money formatting, invariant culture (e.g. 1234.5 => "1234.50").</summary>
        public static string FormatMoney(decimal amount)
        {
            return amount.ToString("0.00", CultureInfo.InvariantCulture);
        }

        private static bool Is(string a, string b)
        {
            return string.Equals((a ?? string.Empty).Trim(), b, StringComparison.OrdinalIgnoreCase);
        }
    }
}
