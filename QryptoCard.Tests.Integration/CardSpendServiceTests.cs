using System;
using System.Linq;
using QryptoCard.INT;
using QryptoCard.INT.Model.Service;
using QryptoCard.INT.Script.Service;
using QryptoCard.Tests.Fixtures.LocalDb;
using Xunit;

namespace QryptoCard.Tests.Integration
{
    // DB-backed coverage for the card spend path. The WasabiCard gateway is unreachable under test,
    // so a sufficient-balance spend exercises the debit-first + ambiguous-result (PendingProvider)
    // branch: the key money property is that the debit lands and is NOT auto-reversed on an
    // ambiguous provider outcome. Insufficient balance must fail closed with no debit.
    public class CardSpendServiceTests : IClassFixture<LocalDbFixture>
    {
        readonly LocalDbFixture _db;
        public CardSpendServiceTests(LocalDbFixture db) { _db = db; }

        static string Fresh(string tag) => "csit-" + tag + "-" + Guid.NewGuid().ToString("N").Substring(0, 8);

        decimal BalanceOf(string uid)
        {
            using (var ctx = _db.NewContext())
                return ctx.tblM_User_Balance
                    .Where(p => p.UserID == uid && p.Currency == "USDT")
                    .Select(p => p.Balance).FirstOrDefault() ?? 0m;
        }

        void Fund(string uid, decimal amount)
        {
            WalletService.EnsureWallet(uid);
            if (amount > 0)
                Assert.True(WalletService.Credit(uid, amount, 0m, 0d, WalletService.TypeCryptoDeposit,
                    "fund-" + Guid.NewGuid().ToString("N").Substring(0, 8)).Success);
        }

        tblT_Card NewCard(string uid, decimal total, decimal initialDeposit) => new tblT_Card
        {
            ID = "card-" + Guid.NewGuid().ToString("N").Substring(0, 10),
            UserID = uid,
            CardTypeId = 1,
            InitialDeposit = (double)initialDeposit,
            Fee = 0d,
            Total = (double)total,
            HolderID = null
        };

        [Fact]
        public void OpenCard_InsufficientBalance_FailsClosed_NoDebit()
        {
            var uid = Fresh("oc-insuff");
            Fund(uid, 10m);
            var card = NewCard(uid, total: 50m, initialDeposit: 50m);

            var r = CardSpendService.OpenCard(card);

            Assert.False(r.Success);
            Assert.True(r.InsufficientBalance);
            Assert.Equal(10m, BalanceOf(uid)); // unchanged
            using (var ctx = _db.NewContext())
            {
                Assert.Equal(StatusModel.Failed, ctx.tblT_Card.Single(p => p.ID == card.ID).Status);
                Assert.False(ctx.tblH_User_Balance.Any(p => p.TransactionID == card.ID)); // no debit ledger row
            }
        }

        [Fact]
        public void OpenCard_FractionalDeposit_RejectedNoDebitNoOrder()
        {
            var uid = Fresh("oc-frac");
            Fund(uid, 200m);
            var card = NewCard(uid, total: 50.5m, initialDeposit: 50.5m);

            var r = CardSpendService.OpenCard(card);

            Assert.False(r.Success);
            Assert.Contains("whole number", r.Message);
            Assert.Equal(200m, BalanceOf(uid)); // unchanged
            using (var ctx = _db.NewContext())
            {
                Assert.False(ctx.tblT_Card.Any(p => p.ID == card.ID));                  // no order persisted
                Assert.False(ctx.tblH_User_Balance.Any(p => p.TransactionID == card.ID)); // no debit
            }
        }

        [Fact]
        public void TopUp_FractionalAmount_RejectedNoDebitNoOrder()
        {
            var uid = Fresh("tu-frac");
            Fund(uid, 100m);
            var dep = new tblT_Card_Deposit
            {
                ID = "dep-" + Guid.NewGuid().ToString("N").Substring(0, 10),
                UserID = uid, CardNo = "4111111111111111",
                Amount = 40.5d, Fee = 0d, Total = 40.5d
            };

            var r = CardSpendService.TopUp(dep);

            Assert.False(r.Success);
            Assert.Contains("whole number", r.Message);
            Assert.Equal(100m, BalanceOf(uid));
            using (var ctx = _db.NewContext())
            {
                Assert.False(ctx.tblT_Card_Deposit.Any(p => p.ID == dep.ID));
                Assert.False(ctx.tblH_User_Balance.Any(p => p.TransactionID == dep.ID));
            }
        }

        [Fact]
        public void OpenCard_SufficientBalance_DebitsFirst_NotReversedOnAmbiguousProvider()
        {
            var uid = Fresh("oc-ok");
            Fund(uid, 200m);
            var card = NewCard(uid, total: 50m, initialDeposit: 50m);

            var r = CardSpendService.OpenCard(card);

            // Provider unreachable under test => ambiguous => pending, debit stands (NOT reversed).
            Assert.True(r.Success);
            Assert.True(r.ProviderPending);
            Assert.Equal(150m, BalanceOf(uid));
            using (var ctx = _db.NewContext())
            {
                Assert.Equal(StatusModel.PendingProvider, ctx.tblT_Card.Single(p => p.ID == card.ID).Status);
                var ledger = ctx.tblH_User_Balance.Single(p => p.TransactionID == card.ID);
                Assert.Equal(-50m, ledger.Amount.Value);
                Assert.Equal(WalletService.TypeCardOpen, ledger.Type);
                Assert.Equal(ledger.Balance.Value, ledger.BalancePrevious.Value + ledger.Amount.Value);
            }
        }

        [Fact]
        public void TopUp_InsufficientBalance_FailsClosed_NoDebit()
        {
            var uid = Fresh("tu-insuff");
            Fund(uid, 5m);
            var dep = new tblT_Card_Deposit
            {
                ID = "dep-" + Guid.NewGuid().ToString("N").Substring(0, 10),
                UserID = uid, CardNo = "4111111111111111",
                Amount = 40d, Fee = 0d, Total = 40d
            };

            var r = CardSpendService.TopUp(dep);

            Assert.False(r.Success);
            Assert.True(r.InsufficientBalance);
            Assert.Equal(5m, BalanceOf(uid));
            using (var ctx = _db.NewContext())
                Assert.Equal(StatusModel.Failed, ctx.tblT_Card_Deposit.Single(p => p.ID == dep.ID).Status);
        }

        [Fact]
        public void TopUp_SufficientBalance_DebitsFirst_NotReversedOnAmbiguousProvider()
        {
            var uid = Fresh("tu-ok");
            Fund(uid, 100m);
            var dep = new tblT_Card_Deposit
            {
                ID = "dep-" + Guid.NewGuid().ToString("N").Substring(0, 10),
                UserID = uid, CardNo = "4111111111111111",
                Amount = 40d, Fee = 0d, Total = 40d
            };

            var r = CardSpendService.TopUp(dep);

            Assert.True(r.Success);
            Assert.True(r.ProviderPending);
            Assert.Equal(60m, BalanceOf(uid));
            using (var ctx = _db.NewContext())
            {
                Assert.Equal(StatusModel.PendingProvider, ctx.tblT_Card_Deposit.Single(p => p.ID == dep.ID).Status);
                var ledger = ctx.tblH_User_Balance.Single(p => p.TransactionID == dep.ID);
                Assert.Equal(-40m, ledger.Amount.Value);
                Assert.Equal(WalletService.TypeCardTopup, ledger.Type);
            }
        }
    }
}
