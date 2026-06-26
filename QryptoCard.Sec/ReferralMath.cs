using System;

namespace QryptoCard.Sec
{
    /// <summary>
    /// Pure money-math for referral commissions, kept in this dependency-light library (no EF/edmx)
    /// so the loss-proof rules are unit-testable in isolation. The referral payout is a share of the
    /// platform FEE earned on a referee's card buy/top-up — never a share of the gross amount.
    /// </summary>
    public static class ReferralMath
    {
        /// <summary>
        /// Commission owed to a referrer: round(rate * fee, 2), then **capped at the fee itself** so a
        /// payout can never exceed what the platform earned on the transaction (the invariant that
        /// makes the feature impossible to run at a loss on any single payout), and floored at 0. A
        /// non-positive rate or fee yields 0 (no payout). <paramref name="rate"/> is a fraction
        /// (e.g. 0.1 = 10%).
        /// </summary>
        public static decimal Commission(double rate, double fee)
        {
            if (rate <= 0d || fee <= 0d) return 0m;

            decimal feeDec = (decimal)fee;
            decimal commission = Math.Round((decimal)rate * feeDec, 2, MidpointRounding.AwayFromZero);

            if (commission > feeDec) commission = feeDec; // never more than the fee earned
            if (commission < 0m) commission = 0m;
            return commission;
        }
    }
}
