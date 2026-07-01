using QryptoCard.INT.Callback.Service;
using Xunit;

namespace QryptoCard.Tests.Unit
{
    /// <summary>
    /// Money-arithmetic guards for WasabiCard auto-funding: the spendable-net (deposit minus the
    /// platform margin) and the WasabiCard-fee gross-up. These are the easiest things to get subtly
    /// wrong on a money path, so they are pinned here.
    /// </summary>
    public class WasabiCardFundingMathTests
    {
        [Fact]
        public void SpendableNet_DepositMinusThreePercent()
        {
            // $3000 deposit, 3% platform margin -> customer-spendable $2910 (the locked example).
            Assert.Equal(2910m, WasabiCardFundingMath.SpendableNet(3000m, 3));
        }

        [Fact]
        public void SpendableNet_ZeroFee_IsIdentity()
        {
            Assert.Equal(1000m, WasabiCardFundingMath.SpendableNet(1000m, 0));
        }

        [Fact]
        public void GrossUpSend_LandsTheNetAfterWasabiFee()
        {
            // To land $2910 net after WasabiCard's 1.4% fee, send 2910 / 0.986.
            decimal send = WasabiCardFundingMath.GrossUpSend(2910m, 1.4);
            Assert.Equal(2951.318458m, send); // 2910 / 0.986, rounded to 6dp
            // And the round-trip lands (within rounding) the intended net.
            decimal landed = send * (1m - 0.014m);
            Assert.True(System.Math.Abs(landed - 2910m) < 0.01m);
        }

        [Fact]
        public void GrossUpSend_ZeroFee_IsIdentity()
        {
            Assert.Equal(700m, WasabiCardFundingMath.GrossUpSend(700m, 0));
        }

        [Theory]
        [InlineData(-5)]      // negative fee clamps to 0 (never amplifies the amount)
        [InlineData(150)]     // absurd fee clamps below 100 (never divide-by-zero / negative)
        public void GrossUpSend_ClampsFee_NoBlowup(double badFee)
        {
            decimal send = WasabiCardFundingMath.GrossUpSend(100m, badFee);
            Assert.True(send > 0m);
        }

        [Fact]
        public void EndToEnd_3000Deposit_LandsSpendable()
        {
            // Full locked flow: $3000 deposit -> forward $2910 spendable, grossed for the 1.4% fee.
            decimal net = WasabiCardFundingMath.SpendableNet(3000m, 3);
            decimal send = WasabiCardFundingMath.GrossUpSend(net, 1.4);
            decimal landed = send * (1m - 0.014m);
            Assert.Equal(2910m, net);
            Assert.True(System.Math.Abs(landed - 2910m) < 0.01m);
        }

        // ---- streaming per-card forward sizing (deposit-into-card) ----

        [Fact]
        public void CardDrawUsd_NewCardOpen_IsFacePlusOneDollarCreate_ZeroFee()
        {
            // Prod (2026-07-01): a new-card OPEN draws EXACTLY face + $1 create at 0% deposit fee.
            Assert.Equal(21m, WasabiCardFundingMath.CardDrawUsd(true, 20m, 1m, 0));
        }

        [Fact]
        public void CardDrawUsd_TopUp_HasNoCreateCost()
        {
            Assert.Equal(20m, WasabiCardFundingMath.CardDrawUsd(false, 20m, 1m, 0));
        }

        [Fact]
        public void CardDrawUsd_AppliesPerCardFeeWhenSet()
        {
            // If WasabiCard ever charges a per-card deposit fee, it's added on top of face + create.
            Assert.Equal(102.2m, WasabiCardFundingMath.CardDrawUsd(true, 100m, 1m, 1.2));
        }

        [Fact]
        public void ForwardUsdtForCard_ZeroFloatFee_EqualsDraw()
        {
            // With a 0% float top-up fee, the forward equals the card draw (no gross-up).
            Assert.Equal(21m, WasabiCardFundingMath.ForwardUsdtForCard(true, 20m, 1m, 0, 0));
        }

        [Fact]
        public void ForwardUsdtForCard_GrossesUpFloatFee_LandsTheDraw()
        {
            // A $20 open draws $21; grossed up for a 1.4% float top-up fee it lands ~$21.
            decimal send = WasabiCardFundingMath.ForwardUsdtForCard(true, 20m, 1m, 0, 1.4);
            Assert.True(send > 21m);
            decimal landed = send * (1m - 0.014m);
            Assert.True(System.Math.Abs(landed - 21m) < 0.01m);
        }

        [Fact]
        public void FloatDrawdown_ReconcilesTheObservedProd42DollarDrop()
        {
            // The observed prod float move (788.63 -> 746.63 = -$42.00) = sum of new-card draws for
            // faces $20,$10,$3,$5, each face + $1 create at 0% fee = 21+11+4+6 = 42.
            decimal total = 0m;
            foreach (decimal face in new[] { 20m, 10m, 3m, 5m })
                total += WasabiCardFundingMath.CardDrawUsd(true, face, 1m, 0);
            Assert.Equal(42m, total);
        }
    }
}
