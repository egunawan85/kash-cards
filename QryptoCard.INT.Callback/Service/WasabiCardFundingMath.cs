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

        /// <summary>
        /// The USD a single card funding will DRAW from the WasabiCard merchant float:
        ///   face (lands spendable) + (new card ? createCostUsd : 0) + WasabiCard's per-card deposit fee.
        /// Prod data (2026-07-01) shows a new-card OPEN draws EXACTLY face + $1 create at a 0% deposit
        /// fee, so <paramref name="cardDepositFeePct"/> is 0 for opens. DO NOT pass the (unverified)
        /// top-up rate for an open, or the forward over-draws and the surplus is trapped in the
        /// one-directional float (there is no WasabiCard payout API). Inputs clamped defensively.
        /// </summary>
        public static decimal CardDrawUsd(bool isNewCard, decimal face, decimal createCostUsd, double cardDepositFeePct)
        {
            if (face < 0m) face = 0m;
            if (createCostUsd < 0m) createCostUsd = 0m;
            decimal draw = face;
            if (isNewCard) draw += createCostUsd;
            decimal f = ClampFee(cardDepositFeePct);
            if (f > 0m) draw += Math.Round(face * f / 100m, 6, MidpointRounding.AwayFromZero);
            return draw;
        }

        /// <summary>
        /// The USDT to send to WasabiCard's merchant deposit address to fund one card: the float
        /// <see cref="CardDrawUsd"/> the card will consume, grossed up by the inbound float-top-up fee
        /// (<paramref name="floatTopupFeePct"/>) so exactly that draw lands. Sized to the single card
        /// only — never speculative — so the streaming float drains back toward ~zero.
        /// </summary>
        public static decimal ForwardUsdtForCard(bool isNewCard, decimal face, decimal createCostUsd,
            double cardDepositFeePct, double floatTopupFeePct)
        {
            return GrossUpSend(CardDrawUsd(isNewCard, face, createCostUsd, cardDepositFeePct), floatTopupFeePct);
        }

        /// <summary>
        /// True when the amount that ACTUALLY landed in the float (from the wallet_transaction webhook)
        /// covers the card's required draw — the gate for advancing an intent to issuance on confirmed
        /// per-forward evidence (vs. a pooled-balance guess). A tiny epsilon absorbs 6dp USDT rounding so
        /// an exact-to-the-cent landing is not treated as short. A negative landing never covers.
        /// </summary>
        public static bool LandedCoversDraw(decimal landedUsd, decimal drawUsd)
        {
            if (landedUsd < 0m) return false;
            return landedUsd + 0.0001m >= drawUsd;
        }

        private static decimal ClampFee(double pct)
        {
            if (pct < 0) pct = 0;
            if (pct > 99.999) pct = 99.999; // never divide by zero / go negative
            return (decimal)pct;
        }
    }
}
