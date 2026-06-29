using System;
using System.Data.SqlClient;
using System.Linq;
using QryptoCard.INT;
using QryptoCard.INT.Model.Service;
using QryptoCard.INT.Script.Service;
using QryptoCard.Tests.Fixtures.LocalDb;
using Xunit;

namespace QryptoCard.Tests.Integration
{
    // Money-critical refund primitives + CardRefundService validation gates, against LocalDb.
    // The full cancel -> credit happy path needs a live WasabiCard (cancelCard/getCardInfo) and is
    // verified in the environment; here we lock the DB-only invariants that make the money safe:
    // atomic claim-gated credit, per-card dedup (no double refund), commission clawback + fail-closed
    // insufficient handling, and the pre-provider validation gates.
    public class CardRefundTests : IClassFixture<LocalDbFixture>
    {
        readonly LocalDbFixture _db;
        public CardRefundTests(LocalDbFixture db) { _db = db; }

        static string Fresh(string t) => "rf-" + t + "-" + Guid.NewGuid().ToString("N").Substring(0, 8);

        void SeedUser(string uid)
        {
            using (var ctx = _db.NewContext())
            {
                ctx.tblM_User.Add(new tblM_User { UserID = uid, Email = uid + "@t.test", isActive = 1 });
                ctx.SaveChanges();
            }
        }

        void SeedWallet(string uid, decimal bal)
        {
            using (var ctx = _db.NewContext())
            {
                ctx.tblM_User_Balance.Add(new tblM_User_Balance
                {
                    UserID = uid,
                    Currency = "USDT",
                    BalanceID = Guid.NewGuid().ToString(),
                    Balance = bal,
                    isActive = 1,
                    DateCreated = DateTime.Now,
                    CreatedBy = "test"
                });
                ctx.SaveChanges();
            }
        }

        void SeedCardOrder(string id, string uid, string status, string cardNo)
        {
            using (var ctx = _db.NewContext())
            {
                ctx.tblT_Card.Add(new tblT_Card { ID = id, UserID = uid, Status = status, CardNo = cardNo, DateCreated = DateTime.Now });
                ctx.SaveChanges();
            }
        }

        decimal BalOf(string uid)
        {
            using (var ctx = _db.NewContext())
                return ctx.tblM_User_Balance.Where(p => p.UserID == uid && p.Currency == "USDT")
                    .Select(p => p.Balance).FirstOrDefault() ?? 0m;
        }

        string StatusOf(string id)
        {
            using (var ctx = _db.NewContext())
                return ctx.tblT_Card.Where(p => p.ID == id).Select(p => p.Status).FirstOrDefault();
        }

        static SqlParameter[] IdParam(string id) => new[] { new SqlParameter("@id", id) };

        static string RefundedClaim() =>
            "UPDATE dbo.tblT_Card SET Status='" + StatusModel.Refunded +
            "' WHERE ID=@id AND Status='" + StatusModel.RefundPending + "'";

        // ---- CreditCardRefund: atomic claim-gated credit + per-card dedup ----

        [Fact]
        public void CreditCardRefund_CreditsBuyer_FlipsOrder_AndReplayDedupes()
        {
            var uid = Fresh("buyer"); SeedUser(uid); SeedWallet(uid, 0m);
            var order = Fresh("ord"); var card = Fresh("card");
            SeedCardOrder(order, uid, StatusModel.RefundPending, card);

            var r = WalletService.CreditCardRefund(uid, 20m, order, card, RefundedClaim(), IdParam(order), "{}");
            Assert.True(r.Success);
            Assert.Equal(20m, BalOf(uid));
            Assert.Equal(StatusModel.Refunded, StatusOf(order));

            // Replay (same card) dedupes -> no double credit, balance unchanged.
            var r2 = WalletService.CreditCardRefund(uid, 20m, order, card, RefundedClaim(), IdParam(order), "{}");
            Assert.False(r2.Success);
            Assert.Equal(20m, BalOf(uid));
        }

        [Fact]
        public void CreditCardRefund_ClaimLost_WhenOrderNotRefundPending_NoCredit()
        {
            var uid = Fresh("buyer"); SeedUser(uid); SeedWallet(uid, 0m);
            var order = Fresh("ord"); var card = Fresh("card");
            SeedCardOrder(order, uid, StatusModel.Success, card); // not refund-pending

            var r = WalletService.CreditCardRefund(uid, 20m, order, card, RefundedClaim(), IdParam(order), "{}");
            Assert.False(r.Success);
            Assert.Equal("claim_lost", r.FailureReason);
            Assert.Equal(0m, BalOf(uid));
            Assert.Equal(StatusModel.Success, StatusOf(order));
        }

        // ---- ReverseReferralCommission: clawback debit + dedup + fail-closed ----

        [Fact]
        public void ReverseReferralCommission_DebitsReferrer_AndReplayDedupes()
        {
            var uid = Fresh("ref"); SeedUser(uid); SeedWallet(uid, 1m);
            var order = Fresh("ord");

            var r = WalletService.ReverseReferralCommission(uid, 0.06m, order, "{}");
            Assert.True(r.Success);
            Assert.Equal(0.94m, BalOf(uid));

            var r2 = WalletService.ReverseReferralCommission(uid, 0.06m, order, "{}");
            Assert.False(r2.Success); // duplicate_event
            Assert.Equal(0.94m, BalOf(uid));
        }

        [Fact]
        public void ReverseReferralCommission_Insufficient_FailsClosed_NeverNegative()
        {
            var uid = Fresh("ref"); SeedUser(uid); SeedWallet(uid, 0.01m);
            var r = WalletService.ReverseReferralCommission(uid, 0.06m, Fresh("ord"), "{}");
            Assert.False(r.Success);
            Assert.Equal("insufficient_balance", r.FailureReason);
            Assert.Equal(0.01m, BalOf(uid));
        }

        // ---- CardRefundService validation (pre-provider, DB-only) ----

        [Fact]
        public void RefundByOrder_MissingId_Fails()
        {
            var r = CardRefundService.RefundByOrder("  ", "admin@t.test");
            Assert.False(r.Success);
            Assert.Equal("missing_order", r.Outcome);
        }

        [Fact]
        public void RefundByOrder_OrderNotFound_Fails()
        {
            var r = CardRefundService.RefundByOrder(Fresh("ghost"), "admin@t.test");
            Assert.False(r.Success);
            Assert.Equal("order_not_found", r.Outcome);
        }

        [Fact]
        public void RefundByOrder_NotSuccessStatus_NotRefundable()
        {
            var uid = Fresh("u"); SeedUser(uid);
            var order = Fresh("ord");
            SeedCardOrder(order, uid, StatusModel.InProgress, Fresh("card"));
            var r = CardRefundService.RefundByOrder(order, "admin@t.test");
            Assert.False(r.Success);
            Assert.Equal("not_refundable", r.Outcome);
        }

        [Fact]
        public void RefundByOrder_SuccessButNoCardNo_CardNotIssued()
        {
            var uid = Fresh("u"); SeedUser(uid);
            var order = Fresh("ord");
            SeedCardOrder(order, uid, StatusModel.Success, null);
            var r = CardRefundService.RefundByOrder(order, "admin@t.test");
            Assert.False(r.Success);
            Assert.Equal("card_not_issued", r.Outcome);
        }

        // ---- resume path (cancel committed + amount persisted, credit had failed) ----

        void SeedRefundIntent(string cardNo, decimal amount, string buyerId, string openOrderId)
        {
            using (var ctx = _db.NewContext())
                ctx.Database.ExecuteSqlCommand(
                    "INSERT INTO dbo.tblH_Partner_Webhook_ID (Type, TXID, Request, RequestDate) VALUES ('CardRefundIntent', @x, @r, GETDATE())",
                    new SqlParameter("@x", cardNo),
                    new SqlParameter("@r", "{\"amount\":" + amount.ToString(System.Globalization.CultureInfo.InvariantCulture) +
                        ",\"buyerId\":\"" + buyerId + "\",\"openOrderId\":\"" + openOrderId + "\"}"));
        }

        [Fact]
        public void RefundByOrder_ResumesFromRefundPending_WithIntent_CreditsBuyerOnce()
        {
            var uid = Fresh("buyer"); SeedUser(uid); SeedWallet(uid, 0m);
            var order = Fresh("ord"); var card = Fresh("card");
            SeedCardOrder(order, uid, StatusModel.RefundPending, card); // prior cancel left it pending
            SeedRefundIntent(card, 20m, uid, order);                    // confirmed amount persisted

            var r = CardRefundService.RefundByOrder(order, "admin@t.test");
            Assert.True(r.Success);
            Assert.Equal("refunded", r.Outcome);
            Assert.Equal(20m, r.RefundedAmount);
            Assert.Equal(20m, BalOf(uid));
            Assert.Equal(StatusModel.Refunded, StatusOf(order));

            // Re-run after completion: no double credit.
            var r2 = CardRefundService.RefundByOrder(order, "admin@t.test");
            Assert.False(r2.Success);
            Assert.Equal(20m, BalOf(uid));
        }

        [Fact]
        public void RefundByOrder_RefundPending_NoIntent_NeedsManualReview_NoCredit()
        {
            var uid = Fresh("buyer"); SeedUser(uid); SeedWallet(uid, 0m);
            var order = Fresh("ord"); var card = Fresh("card");
            SeedCardOrder(order, uid, StatusModel.RefundPending, card); // pending, but NO persisted intent

            var r = CardRefundService.RefundByOrder(order, "admin@t.test");
            Assert.False(r.Success);
            Assert.Equal("refund_pending_unconfirmed", r.Outcome);
            Assert.Equal(0m, BalOf(uid));
            Assert.Equal(StatusModel.RefundPending, StatusOf(order));
        }
    }
}
