using QryptoCard.Sec;
using Xunit;

namespace QryptoCard.Tests.Unit
{
    /// <summary>
    /// Customer-facing pricing math for the deposit-into-card flow: the % deposit fee and the
    /// ExpectedTotal a customer must send (Price + Face + PercentageFee + FixedFee). Pinned here
    /// because a wrong quote either short-funds the card or overcharges the customer.
    /// </summary>
    public class CardFundingMathTests
    {
        [Fact]
        public void PercentageFee_ThreePercentOfFace()
        {
            Assert.Equal(3.00m, CardFundingMath.PercentageFee(3, 100m));
            Assert.Equal(0.60m, CardFundingMath.PercentageFee(3, 20m));
        }

        [Theory]
        [InlineData(0)]      // zero rate -> no fee
        [InlineData(-5)]     // negative rate clamps to 0
        [InlineData(double.NaN)]
        [InlineData(double.PositiveInfinity)]
        public void PercentageFee_NonPositiveOrNonFinite_IsZero(double rate)
        {
            Assert.Equal(0m, CardFundingMath.PercentageFee(rate, 100m));
        }

        [Fact]
        public void ExpectedTotal_NewCard_SumsAllComponents()
        {
            // $5 card price + $100 face + 3% ($3) + $1.50 fixed fee = $109.50.
            Assert.Equal(109.50m, CardFundingMath.ExpectedTotal(5m, 100m, 3, 1.5m));
        }

        [Fact]
        public void ExpectedTotal_TopUp_NoPrice()
        {
            // Top-up: no card price. $20 face + 3% ($0.60) + $1.50 fixed = $22.10.
            Assert.Equal(22.10m, CardFundingMath.ExpectedTotal(0m, 20m, 3, 1.5m));
        }

        [Fact]
        public void ExpectedTotal_NegativeComponentsClamped_NeverBelowFace()
        {
            // A corrupted price/fixedFee can't drag the total below face + healthy components.
            decimal total = CardFundingMath.ExpectedTotal(-5m, 100m, 3, -2m);
            Assert.Equal(103.00m, total);       // 0 + 100 + 3 + 0
            Assert.True(total >= 100m);
        }

        [Fact]
        public void ExpectedTotal_ZeroFeeAndFixed_IsPricePlusFace()
        {
            Assert.Equal(120m, CardFundingMath.ExpectedTotal(20m, 100m, 0, 0m));
        }

        [Fact]
        public void GrossOnChain_IsNetPlusCommission()
        {
            // Real prod shape: customer sent 28.50; Runegate kept 0.1425 (0.5%); we netted 28.3575.
            // The gross the customer sent — what coverage is measured against — is net + commission.
            Assert.Equal(28.50m, CardFundingMath.GrossOnChain(28.3575m, 0.1425m));
        }

        [Fact]
        public void GrossOnChain_ExactlyCoversExpectedTotal_UnderPointFivePercentCommission()
        {
            // Customer pays the clean sticker (ExpectedTotal = 100); Runegate takes 0.5% (0.50); we net
            // 99.50. Coverage is decided on gross, so 99.50 + 0.50 = 100.00 exactly covers ExpectedTotal —
            // the intent advances, and the 0.5% is absorbed by our margin (the float nets less), never
            // charged on top of the customer's 100.
            decimal net = 99.50m, commission = 0.50m, expectedTotal = 100m;
            Assert.Equal(expectedTotal, CardFundingMath.GrossOnChain(net, commission));
            Assert.True(CardFundingMath.GrossOnChain(net, commission) >= expectedTotal);
        }

        [Fact]
        public void GrossOnChain_NegativeInputsClamped()
        {
            Assert.Equal(0m, CardFundingMath.GrossOnChain(-5m, -1m));
            Assert.Equal(10m, CardFundingMath.GrossOnChain(10m, -1m));
            Assert.Equal(2m, CardFundingMath.GrossOnChain(-1m, 2m));
        }
    }
}
