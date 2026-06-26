using System.Globalization;
using QryptoCard.Sec;
using Xunit;

namespace QryptoCard.Tests.Unit
{
    // Pure money-math for referral commission. The load-bearing rule is the cap: a payout can never
    // exceed the fee the platform earned, so the feature can never run at a loss on any single payout.
    public class ReferralMathTests
    {
        static decimal D(string s) => decimal.Parse(s, CultureInfo.InvariantCulture);

        [Theory]
        // rate (fraction), fee, expected commission
        [InlineData(0.1, 2.50, "0.25")]   // 10% of a $2.50 fee = 25c (the headline example)
        [InlineData(0.1, 10.00, "1.00")]  // 10% of a $10 fee
        [InlineData(0.25, 2.50, "0.63")]  // 25% of $2.50 = 0.625 -> rounds to 0.63 (away from zero)
        [InlineData(0.5, 4.00, "2.00")]   // 50% of $4 fee
        [InlineData(0.1, 2.555, "0.26")]  // 0.2555 -> 0.26 (round half away from zero)
        public void Commission_IsRateTimesFee_Rounded(double rate, double fee, string expected)
        {
            Assert.Equal(D(expected), ReferralMath.Commission(rate, fee));
        }

        [Theory]
        // A misconfigured rate >= 100% can never pay out more than the fee itself.
        [InlineData(1.0, 2.50, "2.50")]   // exactly the fee (break-even worst case)
        [InlineData(2.0, 2.50, "2.50")]   // 200% requested -> capped at the $2.50 fee
        [InlineData(10.0, 1.00, "1.00")]  // absurd rate -> capped at the $1 fee
        public void Commission_NeverExceedsTheFee(double rate, double fee, string expected)
        {
            var c = ReferralMath.Commission(rate, fee);
            Assert.Equal(D(expected), c);
            Assert.True(c <= (decimal)fee);
        }

        [Theory]
        [InlineData(0.0, 2.50)]    // no rate -> no payout
        [InlineData(0.1, 0.0)]     // no fee -> no payout
        [InlineData(-0.1, 2.50)]   // negative rate -> no payout
        [InlineData(0.1, -5.0)]    // negative fee -> no payout
        [InlineData(0.1, 0.04)]    // sub-cent (0.004) rounds to 0 -> no payout
        public void Commission_NonPositiveInputsOrSubCent_PayNothing(double rate, double fee)
        {
            Assert.Equal(0m, ReferralMath.Commission(rate, fee));
        }
    }
}
