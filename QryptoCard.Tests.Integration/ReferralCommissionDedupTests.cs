using System;
using System.Linq;
using QryptoCard.INT.Script.Service;
using QryptoCard.Tests.Fixtures.LocalDb;
using Xunit;

namespace QryptoCard.Tests.Integration
{
    // Proves the referral-commission credit is idempotent against the REAL dedup index
    // (UIX_..._ReferralCommission_TXID, filtered to Type='ReferralCommission'). Exercises the INT
    // WalletService copy, which is kept in parity with the Callback copy the finalize actually calls
    // (the Callback EF context can't be co-loaded with the INT context in one test process, so the
    // shared atomic dedup mechanism is verified here once).
    public class ReferralCommissionDedupTests : IClassFixture<LocalDbFixture>
    {
        readonly LocalDbFixture _db;
        public ReferralCommissionDedupTests(LocalDbFixture db) { _db = db; }

        static string Fresh(string tag) =>
            "refc-" + tag + "-" + Guid.NewGuid().ToString("N").Substring(0, 8);

        decimal BalanceOf(string uid)
        {
            using (var ctx = _db.NewContext())
                return ctx.tblM_User_Balance
                    .Where(p => p.UserID == uid && p.Currency == "USDT")
                    .Select(p => p.Balance).FirstOrDefault() ?? 0m;
        }

        [Fact]
        public void Credits_Once_Then_ReplaySameOrder_Dedupes_NoDoublePay()
        {
            var referrer = Fresh("ref");
            var orderId = Fresh("order");
            WalletService.EnsureWallet(referrer);

            var first = WalletService.CreditReferralCommission(referrer, 0.25m, orderId, "{}");
            Assert.True(first.Success);
            Assert.Equal(0.25m, BalanceOf(referrer));

            // Same referee order id => already paid => dedupe, balance unchanged.
            var replay = WalletService.CreditReferralCommission(referrer, 0.25m, orderId, "{}");
            Assert.False(replay.Success);
            Assert.Equal("duplicate_event", replay.FailureReason);
            Assert.Equal(0.25m, BalanceOf(referrer));
        }

        [Fact]
        public void DifferentOrders_EachPayIndependently()
        {
            var referrer = Fresh("ref");
            WalletService.EnsureWallet(referrer);

            Assert.True(WalletService.CreditReferralCommission(referrer, 0.25m, Fresh("o1"), "{}").Success);
            Assert.True(WalletService.CreditReferralCommission(referrer, 0.50m, Fresh("o2"), "{}").Success);
            Assert.Equal(0.75m, BalanceOf(referrer));
        }

        [Fact]
        public void NonPositiveAmount_FailsClosed_NoCredit()
        {
            var referrer = Fresh("ref");
            WalletService.EnsureWallet(referrer);

            var r = WalletService.CreditReferralCommission(referrer, 0m, Fresh("order"), "{}");
            Assert.False(r.Success);
            Assert.Equal("non_positive_commission", r.FailureReason);
            Assert.Equal(0m, BalanceOf(referrer));
        }
    }
}
