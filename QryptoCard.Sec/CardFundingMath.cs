using System;

namespace QryptoCard.Sec
{
    /// <summary>
    /// Pure customer-facing pricing math for the deposit-into-card flow, kept in this
    /// dependency-light library (no EF/edmx) so the money arithmetic is unit-testable in
    /// isolation — same rationale as <see cref="ReferralMath"/> and WasabiCardFundingMath.
    ///
    /// The amount a customer must deposit to fund a card is:
    ///     ExpectedTotal = Price + Face + PercentageFee + FixedFee
    /// where
    ///     Face          = the amount that must LAND spendable on the card,
    ///     Price         = the card price (wholesale + our markup; new cards only, 0 for a top-up),
    ///     PercentageFee = the % deposit fee on the face (our margin; e.g. 3% of Face),
    ///     FixedFee      = a flat per-funding fee (USDT) that passes through the Runegate transfer cost.
    /// All percentages are whole numbers (e.g. 3 = 3%). Every input is clamped defensively so a
    /// corrupted setting can never make the customer owe a negative amount or crash the quote.
    /// </summary>
    public static class CardFundingMath
    {
        /// <summary>
        /// The % deposit fee charged on the face: round(feePct/100 * face, 2), floored at 0.
        /// Non-finite or non-positive inputs yield 0.
        /// </summary>
        public static decimal PercentageFee(double feePct, decimal face)
        {
            if (double.IsNaN(feePct) || double.IsInfinity(feePct)) return 0m;
            if (feePct <= 0d || face <= 0m) return 0m;
            decimal fee = Math.Round((decimal)feePct / 100m * face, 2, MidpointRounding.AwayFromZero);
            return fee < 0m ? 0m : fee;
        }

        /// <summary>
        /// The total the customer must deposit to fund a card, rounded to 2dp. Each component is
        /// clamped to &gt;= 0 first so no single corrupted input can drag the total below the sum of
        /// the healthy ones (in particular the total is always &gt;= Face, so the card can be funded).
        /// </summary>
        public static decimal ExpectedTotal(decimal price, decimal face, double feePct, decimal fixedFee)
        {
            if (price < 0m) price = 0m;
            if (face < 0m) face = 0m;
            if (fixedFee < 0m) fixedFee = 0m;
            decimal total = price + face + PercentageFee(feePct, face) + fixedFee;
            return Math.Round(total, 2, MidpointRounding.AwayFromZero);
        }

        /// <summary>
        /// The GROSS on-chain amount the customer actually sent for a settled deposit event =
        /// net-received + gateway-commission. Runegate's deposit webhook reports the NET it credited us
        /// (<paramref name="netReceived"/>) and the commission it kept (<paramref name="commission"/>)
        /// separately; the customer's obligation is measured against what they SENT (the gross), so
        /// coverage of ExpectedTotal is decided on this value. The gateway commission is therefore
        /// absorbed by our margin (our real float nets less), never charged on top of the customer's
        /// ExpectedTotal sticker. Both inputs are clamped to &gt;= 0 so a malformed event can't credit or
        /// accrue a negative amount.
        /// </summary>
        public static decimal GrossOnChain(decimal netReceived, decimal commission)
        {
            if (netReceived < 0m) netReceived = 0m;
            if (commission < 0m) commission = 0m;
            return netReceived + commission;
        }
    }
}
