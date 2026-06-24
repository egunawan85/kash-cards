using QryptoCard.Sec;
using Xunit;

namespace QryptoCard.Tests.Unit
{
    // Unit coverage for the pure post-verify cross-check decision logic. These gate money-bearing
    // callback actions (deposit-fail refund, card-open credit) AFTER the signature has been
    // verified at the edge, so the cases below encode "what must the provider's canonical record
    // say before we move money."
    public class WebhookCrossCheckEvaluatorTests
    {
        // ---- EvaluateDepositRefund -------------------------------------------------------------

        [Theory]
        [InlineData("fail")]
        [InlineData("failed")]
        [InlineData("FAILED")]
        [InlineData(" Cancelled ")]
        [InlineData("rejected")]
        public void DepositRefund_ProviderConfirmsFailure_Confirmed(string canonicalStatus)
        {
            Assert.Equal(CrossCheckOutcome.Confirmed,
                WebhookCrossCheckEvaluator.EvaluateDepositRefund(canonicalStatus));
        }

        [Theory]
        [InlineData("success")]
        [InlineData("SUCCESS")]
        [InlineData("completed")]
        [InlineData("succeed")]
        public void DepositRefund_ProviderSaysSucceeded_Mismatch(string canonicalStatus)
        {
            // The webhook claimed "fail" (which would refund), but the provider says the deposit
            // actually succeeded — refunding here would be a double-spend. Reject.
            Assert.Equal(CrossCheckOutcome.Mismatch,
                WebhookCrossCheckEvaluator.EvaluateDepositRefund(canonicalStatus));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("processing")]
        [InlineData("wait_process")]
        [InlineData("something_new")]
        public void DepositRefund_NoRecordOrPendingOrUnknown_Unavailable(string canonicalStatus)
        {
            // Cannot confirm a failure (record missing / still pending / vocabulary we don't know)
            // — withhold the refund rather than credit on an unconfirmed claim.
            Assert.Equal(CrossCheckOutcome.Unavailable,
                WebhookCrossCheckEvaluator.EvaluateDepositRefund(canonicalStatus));
        }

        // ---- EvaluateCardOpen ------------------------------------------------------------------

        [Fact]
        public void CardOpen_SameCard_Confirmed()
        {
            Assert.Equal(CrossCheckOutcome.Confirmed,
                WebhookCrossCheckEvaluator.EvaluateCardOpen("4111111111111111", "4111111111111111"));
        }

        [Fact]
        public void CardOpen_DifferentCard_Mismatch()
        {
            Assert.Equal(CrossCheckOutcome.Mismatch,
                WebhookCrossCheckEvaluator.EvaluateCardOpen("4111111111111111", "5500000000000004"));
        }

        [Theory]
        [InlineData(null, "4111")]
        [InlineData("4111", null)]
        [InlineData("", "4111")]
        [InlineData("4111", "")]
        public void CardOpen_MissingEitherCard_Unavailable(string expected, string canonical)
        {
            Assert.Equal(CrossCheckOutcome.Unavailable,
                WebhookCrossCheckEvaluator.EvaluateCardOpen(expected, canonical));
        }

        // ---- AmountsMatch ----------------------------------------------------------------------

        [Theory]
        [InlineData("50", "50")]
        [InlineData("50.00", "50")]
        [InlineData("50.00", "50.000")]
        [InlineData(" 50.00 ", "50")]
        public void AmountsMatch_EqualValues_True(string a, string b)
        {
            Assert.True(WebhookCrossCheckEvaluator.AmountsMatch(a, b));
        }

        [Theory]
        [InlineData("50.00", "50.01")]
        [InlineData("50", "500")]
        [InlineData("50", null)]
        [InlineData("50", "")]
        [InlineData("50", "abc")]
        [InlineData(null, null)]
        public void AmountsMatch_UnequalOrUnparseable_False(string a, string b)
        {
            Assert.False(WebhookCrossCheckEvaluator.AmountsMatch(a, b));
        }
    }
}
