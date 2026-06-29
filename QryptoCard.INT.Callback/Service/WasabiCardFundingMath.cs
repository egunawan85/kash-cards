using System;

namespace QryptoCard.INT.Callback.Service
{
    /// <summary>
    /// Pure (no DB / no gateway) money arithmetic for WasabiCard auto-funding, factored out so the
    /// fee math — the easiest thing to get subtly wrong on a money path — is unit-testable in
    /// isolation. All percentages are whole numbers (e.g. 3 = 3%, 1.4 = 1.4%).
    /// </summary>
    public static class WasabiCardFundingMath
    {
        /// <summary>
        /// The customer's spendable / card-face portion of a deposit = deposit minus our platform
        /// margin. This is the amount WasabiCard must ultimately cover (the 3% is our margin and
        /// never reaches WasabiCard). Fee is clamped to [0,100) defensively.
        /// </summary>
        public static decimal SpendableNet(decimal depositAmount, double platformFeePct)
        {
            decimal f = ClampFee(platformFeePct);
            return depositAmount * (1m - f / 100m);
        }

        /// <summary>
        /// Gross up a net USD target by WasabiCard's deposit fee so that exactly <paramref name="netUsd"/>
        /// lands after the fee is taken: send = net / (1 - fee). Rounded to 6 dp (USDT precision).
        /// </summary>
        public static decimal GrossUpSend(decimal netUsd, double wcFeePct)
        {
            decimal f = ClampFee(wcFeePct);
            return Math.Round(netUsd / (1m - f / 100m), 6, MidpointRounding.AwayFromZero);
        }

        private static decimal ClampFee(double pct)
        {
            if (pct < 0) pct = 0;
            if (pct > 99.999) pct = 99.999; // never divide by zero / go negative
            return (decimal)pct;
        }
    }
}
