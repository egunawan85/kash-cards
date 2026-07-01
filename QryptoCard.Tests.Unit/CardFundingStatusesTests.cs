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

        [Fact]
        public void InvoicePaymentStatusValues_ArePinned()
        {
            // These are Runegate's invoice webhook status strings — the settlement matcher compares
            // against them to decide whether to credit; a rename must be deliberate.
            Assert.Equal("PartiallyPaid", InvoicePaymentStatus.PartiallyPaid);
            Assert.Equal("OverPaid", InvoicePaymentStatus.OverPaid);
            Assert.Equal("Completed", InvoicePaymentStatus.Completed);
        }

        // The covered-predicate is the single decision that lets a landed deposit credit the wallet and
        // advance the intent. Getting it wrong is a money bug in both directions (credit on a partial =
        // over-issue; no-credit on a completed = stuck funds), so it is pinned exhaustively.
        [Theory]
        [InlineData("Completed", true)]
        [InlineData("OverPaid", true)]
        [InlineData("completed", true)]     // case-insensitive
        [InlineData("OVERPAID", true)]
        [InlineData("PartiallyPaid", false)] // funds arrived but not enough — do NOT credit yet
        [InlineData("Pending", false)]
        [InlineData("Expired", false)]
        [InlineData("Failed", false)]
        [InlineData("", false)]
        [InlineData("   ", false)]
        [InlineData(null, false)]
        [InlineData("Complete", false)]      // near-miss must not match
        public void IsCovered_CreditsOnlyOnFullyPaidStatuses(string status, bool expected)
        {
            Assert.Equal(expected, InvoicePaymentStatus.IsCovered(status));
        }
    }
}
