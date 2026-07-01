using QryptoCard.Sec;
using Xunit;

namespace QryptoCard.Tests.Unit
{
    /// <summary>
    /// Pins the PURE presentation logic behind the deposit-into-card customer UI: the status->stage
    /// mapping the tracker renders, the terminal/failure/open classification the poller + card list
    /// depend on, and the tolerant balance parse/sum behind "Total card balance". A wrong stage map
    /// mis-tells the user where their money is; a wrong sum mis-states their balance.
    /// </summary>
    public class CardFundingDisplayTests
    {
        [Theory]
        [InlineData("Pending", CardFundingDisplay.StageWaiting)]
        [InlineData("Funding", CardFundingDisplay.StageFunding)]
        [InlineData("Confirming", CardFundingDisplay.StageFunding)]
        [InlineData("Issuing", CardFundingDisplay.StageFunding)]
        [InlineData("Completed", CardFundingDisplay.StageReady)]
        [InlineData("completed", CardFundingDisplay.StageReady)] // case-insensitive
        // Failures are surfaced via IsFailure, not as a stage — must NOT imply funding/ready progress.
        [InlineData("Failed", CardFundingDisplay.StageWaiting)]
        [InlineData("Expired", CardFundingDisplay.StageWaiting)]
        [InlineData("Cancelled", CardFundingDisplay.StageWaiting)]
        [InlineData("", CardFundingDisplay.StageWaiting)]
        [InlineData(null, CardFundingDisplay.StageWaiting)]
        [InlineData("bogus", CardFundingDisplay.StageWaiting)]
        public void StageOf_MapsStatusToStage(string status, int expected)
        {
            Assert.Equal(expected, CardFundingDisplay.StageOf(status));
        }

        [Theory]
        [InlineData("Completed", true)]
        [InlineData("Failed", true)]
        [InlineData("Expired", true)]
        [InlineData("Cancelled", true)]
        [InlineData("Pending", false)]
        [InlineData("Funding", false)]
        [InlineData("Confirming", false)]
        [InlineData("Issuing", false)]
        [InlineData(null, false)]
        public void IsTerminal_TrueOnceNothingMoreWillChange(string status, bool expected)
        {
            Assert.Equal(expected, CardFundingDisplay.IsTerminal(status));
        }

        [Theory]
        [InlineData("Failed", true)]
        [InlineData("Expired", true)]
        [InlineData("Cancelled", true)]
        [InlineData("Completed", false)] // success is terminal but NOT a failure
        [InlineData("Pending", false)]
        public void IsFailure_OnlyNonSuccessTerminals(string status, bool expected)
        {
            Assert.Equal(expected, CardFundingDisplay.IsFailure(status));
        }

        [Theory]
        [InlineData("Pending", true)]
        [InlineData("Funding", true)]
        [InlineData("Confirming", true)]
        [InlineData("Issuing", true)]
        [InlineData("Completed", false)]
        [InlineData("Failed", false)]
        [InlineData("Expired", false)]
        public void IsOpen_TrueWhileInFlight(string status, bool expected)
        {
            Assert.Equal(expected, CardFundingDisplay.IsOpen(status));
        }

        [Theory]
        [InlineData("12.34 USD", "12.34")]
        [InlineData("12.34 USDT", "12.34")]
        [InlineData("12.34", "12.34")]
        [InlineData("  1,234.50 USD ", "1234.50")]
        [InlineData("$99.00", "99.00")]
        [InlineData("0", "0")]
        [InlineData("", "0")]
        [InlineData(null, "0")]
        [InlineData("abc", "0")]
        [InlineData("-5.00 USD", "5.00")] // display balance never negative
        public void ParseAmount_ExtractsLeadingNumeric(string raw, string expected)
        {
            Assert.Equal(decimal.Parse(expected, System.Globalization.CultureInfo.InvariantCulture),
                CardFundingDisplay.ParseAmount(raw));
        }

        [Fact]
        public void SumBalances_AddsTolerantOfBadRows()
        {
            var rows = new[] { "10.00 USD", "5.50 USD", null, "", "bogus", "1,000.00 USD" };
            Assert.Equal(1015.50m, CardFundingDisplay.SumBalances(rows));
        }

        [Fact]
        public void SumBalances_NullInputIsZero()
        {
            Assert.Equal(0m, CardFundingDisplay.SumBalances(null));
        }

        [Theory]
        [InlineData("1234.5", "1234.50")]
        [InlineData("0", "0.00")]
        [InlineData("99.999", "100.00")]
        public void FormatMoney_TwoDecimalsInvariant(string amount, string expected)
        {
            Assert.Equal(expected, CardFundingDisplay.FormatMoney(
                decimal.Parse(amount, System.Globalization.CultureInfo.InvariantCulture)));
        }
    }
}
