using QryptoCard.Sec;
using Xunit;

namespace QryptoCard.Tests.Unit
{
    /// <summary>
    /// Pins the deposit-into-card status string VALUES. These strings are also written as literals in
    /// raw SQL (the intent state machine) and in migration 0011 (the filtered index + one-open guard),
    /// which can't reference the C# constants. This test is the drift guard: renaming a value here fails
    /// the build until the author has updated every coupled raw-SQL literal and the migration to match.
    /// (Prior red-team rounds were triggered by exactly this class of status-string drift.)
    /// </summary>
    public class CardFundingStatusesTests
    {
        [Fact]
        public void IntentStatusValues_ArePinned()
        {
            Assert.Equal("Pending", CardFundingStatuses.Pending);
            Assert.Equal("Funding", CardFundingStatuses.Funding);
            Assert.Equal("Confirming", CardFundingStatuses.Confirming);
            Assert.Equal("Issuing", CardFundingStatuses.Issuing);
            Assert.Equal("Completed", CardFundingStatuses.Completed);
            Assert.Equal("Expired", CardFundingStatuses.Expired);
            Assert.Equal("Cancelled", CardFundingStatuses.Cancelled);
            Assert.Equal("Failed", CardFundingStatuses.Failed);
        }

        [Fact]
        public void GateKeys_ArePinned()
        {
            // The DB setting name is also referenced in migration 0011's seed row.
            Assert.Equal("CardFundingStreamingEnabled", CardFundingGate.SettingEnabled);
            Assert.Equal("CARD_FUNDING_STREAMING_ENABLED", CardFundingGate.EnvEnabled);
        }
    }
}
