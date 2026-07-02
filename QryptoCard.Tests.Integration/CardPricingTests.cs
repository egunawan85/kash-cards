using QryptoCard.INT.Script.Service;
using Xunit;

namespace QryptoCard.Tests.Integration
{
    // Pure markup math for per-card pricing: CardPrice = ceil(wholesale * (1 + markup%)), so the
    // customer price is never below WasabiCard's wholesale cost and always lands on a whole dollar.
    public class CardPricingTests
    {
        [Theory]
        [InlineData(12, 0, 12)]    // markup 0 => break-even at wholesale (the loss the change closes)
        [InlineData(1, 0, 1)]
        [InlineData(12, 20, 15)]   // 14.40 rounds UP to 15
        [InlineData(1, 20, 2)]     // 1.20 rounds UP to 2
        [InlineData(12, 25, 15)]   // exactly 15.00, no rounding
        [InlineData(12, 1, 13)]    // 12.12 rounds UP to 13
        [InlineData(0, 30, 0)]     // free card stays free
        [InlineData(-5, 10, 0)]    // negative wholesale floored to 0
        [InlineData(10, -5, 10)]   // negative markup floored to 0 => wholesale
        public void MarkupPrice_RoundsUpToWholeDollar(double wholesale, double markupPct, double expected)
        {
            Assert.Equal(expected, CardCatalogService.MarkupPrice(wholesale, markupPct));
        }

        // Catalog allowlist: we only offer programs our funding flow can actually issue — no cardholder
        // needed, or a B2B holder model. B2C (heavy document KYC we don't collect) and any unknown/missing
        // model on a holder-required card are hidden, so the buy flow never dead-ends at createHolder.
        [Theory]
        [InlineData(false, "B2C", true)]    // no holder needed -> model irrelevant, always fulfillable
        [InlineData(false, null, true)]
        [InlineData(true, "B2B", true)]     // B2B holder = the lightweight shape we send
        [InlineData(true, "b2b", true)]     // case-insensitive
        [InlineData(true, " B2B ", true)]   // trimmed
        [InlineData(true, "B2C", false)]    // B2C = full document KYC -> not offered
        [InlineData(true, null, false)]     // fail-closed: unknown model on a holder-required card
        [InlineData(true, "", false)]
        [InlineData(true, "SOMETHING_NEW", false)]
        public void IsFulfillable_OnlyNoHolderOrB2B(bool needCardHolder, string model, bool expected)
        {
            Assert.Equal(expected, CardCatalogService.IsFulfillable(needCardHolder, model));
        }
    }
}
