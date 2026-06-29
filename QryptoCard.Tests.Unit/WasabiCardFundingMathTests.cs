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
    }
}
