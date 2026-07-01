using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq;

namespace QryptoCard.INT.Script.Service
{
    /// <summary>
    /// Deposit-into-card issuance tick (INT tier). Once the Callback-tier pump has confirmed a card's
    /// funds landed at WasabiCard (intent Status=Issuing), this issues the card by building the order
    /// from the intent's pricing snapshot and delegating to the proven CardSpendService (debit-first,
    /// open/top-up WasabiCard, finalize, pay commission). Idempotent: the order's UserReferenceID is
    /// the IntentID, so a re-run replays the original order (no second debit / double open).
    ///
    /// Also runs the expiry sweep (Pending past ExpiryDate -> Expired; funds already received remain
    /// as internal-wallet residual). Gated by CardFundingStreamingEnabled.
    /// </summary>
    public static class CardFundingIssuanceService
    {
        private const int Batch = 10;

        public static string RunTick()
        {
            if (!CardFundingIntentService.Enabled()) return "{\"skipped\":\"disabled\"}";

            int expired = ExpireStalePending();
            int issued = 0, failed = 0;
            try
            {
                foreach (var it in LoadByStatus(CardFundingIntentService.StIssuing, Batch))
                {
                    bool isNew = string.Equals(it.Kind, CardFundingIntentService.KindNew, StringComparison.OrdinalIgnoreCase);
                    var outcome = isNew ? IssueNew(it) : IssueTopUp(it);
                    if (outcome == Outcome.Completed) issued++;
                    else if (outcome == Outcome.Failed) failed++;
                    // Pending/ambiguous: leave in Issuing; the order reconciler finalizes and a later
                    // tick replays (idempotently) to Complete.
                }
            }
            catch (Exception ex)
            {
                Trace.TraceError("CardFundingIssuanceService.RunTick failed: " + ex.GetType().FullName);
            }

            return "{\"issued\":" + issued + ",\"failed\":" + failed + ",\"expired\":" + expired + "}";
        }

        private enum Outcome { Completed, Failed, Pending }

        private static Outcome IssueNew(IntentRow it)
        {
            var data = it.CardTypeId.HasValue ? CardCatalogService.GetById(it.CardTypeId.Value) : null;
            if (data == null) return Fail(it, "card_unavailable");

            var x = new tblT_Card
            {
                ID = "QRYCRDBUY" + CounterService.Next(1).ToString("000000000000"),
                UserID = it.UserID,
                CardTypeId = it.CardTypeId,
                HolderID = it.HolderID,
                InitialDeposit = (double)it.Face,
                Price = (double)it.Price,
                FeeInPercentage = it.FeeInPercentage,
                Fee = (double)it.PercentageFee,
                // Debit the FULL amount the customer deposited for this card (price + face + % fee +
                // fixed fee). Only `face` funds the card at WasabiCard; the rest is our margin/cost,
                // so the buffer nets to ~0 and the fixed fee is never gifted back as user residual.
                Total = (double)it.ExpectedTotal,
                ReceivedAmount = (double)it.Face,
                Currency = "USD",
                ReceivedCurrency = "USD",
                isNeedCardholder = data.NeedCardHolder,
                DateExpired = DateTime.Now.AddHours(1),
                UserReferenceID = it.IntentID, // idempotency: a re-run replays, never double-issues
            };

            CardSpendService.SpendResult spend;
            try { spend = CardSpendService.OpenCard(x); }
            catch (Exception ex) { Trace.TraceError("IssueNew OpenCard threw for intent " + it.IntentID + ": " + ex.GetType().FullName); return Outcome.Pending; }

            if (spend != null && spend.ProviderConfirmed) return CompleteWithCard(it, x.ID, x.CardNo);
            if (spend != null && spend.ProviderFailed) return Fail(it, "open_failed");
            return Outcome.Pending; // ambiguous / insufficient — leave Issuing for reconcile + replay
        }

        private static Outcome IssueTopUp(IntentRow it)
        {
            if (string.IsNullOrEmpty(it.CardNo)) return Fail(it, "missing_card");

            var x = new tblT_Card_Deposit
            {
                ID = "QRYCRDPST" + CounterService.Next(2).ToString("000000000000"),
                UserID = it.UserID,
                CardNo = it.CardNo,
                Amount = (double)it.Face,
                FeeInPercentage = it.FeeInPercentage,
                Fee = (double)it.PercentageFee,
                Total = (double)it.ExpectedTotal, // see IssueNew: full deposit, so no residual gifting
                ReceivedAmount = (double)it.Face,
                Currency = "USD",
                Status = "Created",
                DateTransaction = DateTime.Now,
                DateExpired = DateTime.Now.AddHours(1),
                UserReferenceID = it.IntentID,
            };

            CardSpendService.SpendResult spend;
            try { spend = CardSpendService.TopUp(x); }
            catch (Exception ex) { Trace.TraceError("IssueTopUp TopUp threw for intent " + it.IntentID + ": " + ex.GetType().FullName); return Outcome.Pending; }

            if (spend != null && spend.ProviderConfirmed) return CompleteWithCard(it, x.ID, it.CardNo);
            if (spend != null && spend.ProviderFailed) return Fail(it, "topup_failed");
            return Outcome.Pending;
        }

        private static Outcome CompleteWithCard(IntentRow it, string orderId, string cardNo)
        {
            using (var db = new DBEntities())
            {
                db.Database.ExecuteSqlCommand(
                    "UPDATE dbo.tblT_Card_Funding_Intent SET Status = 'Completed', OrderID = @oid, " +
                    "CardNo = ISNULL(@card, CardNo), UpdatedDate = @now WHERE IntentID = @id AND Status = 'Issuing'",
                    P("@oid", orderId), P("@card", (object)cardNo), P("@now", DateTime.Now), P("@id", it.IntentID));
            }
            return Outcome.Completed;
        }

        private static Outcome Fail(IntentRow it, string note)
        {
            using (var db = new DBEntities())
            {
                db.Database.ExecuteSqlCommand(
                    "UPDATE dbo.tblT_Card_Funding_Intent SET Status = 'Failed', Note = @note, UpdatedDate = @now " +
                    "WHERE IntentID = @id AND Status = 'Issuing'",
                    P("@note", note), P("@now", DateTime.Now), P("@id", it.IntentID));
            }
            return Outcome.Failed;
        }

        private static int ExpireStalePending()
        {
            try
            {
                using (var db = new DBEntities())
                {
                    return db.Database.ExecuteSqlCommand(
                        "UPDATE dbo.tblT_Card_Funding_Intent SET Status = 'Expired', UpdatedDate = @now " +
                        "WHERE Status = 'Pending' AND ExpiryDate IS NOT NULL AND ExpiryDate < @now",
                        P("@now", DateTime.Now));
                }
            }
            catch (Exception ex)
            {
                Trace.TraceError("CardFundingIssuanceService.ExpireStalePending failed: " + ex.GetType().FullName);
                return 0;
            }
        }

        // ---- intent row access (raw SQL) ------------------------------------

        private class IntentRow
        {
            public string IntentID { get; set; }
            public string UserID { get; set; }
            public string Kind { get; set; }
            public long? CardTypeId { get; set; }
            public long? HolderID { get; set; }
            public string CardNo { get; set; }
            public decimal Face { get; set; }
            public decimal Price { get; set; }
            public double FeeInPercentage { get; set; }
            public decimal PercentageFee { get; set; }
            public decimal ExpectedTotal { get; set; }
        }

        private static List<IntentRow> LoadByStatus(string status, int top)
        {
            using (var db = new DBEntities())
            {
                return db.Database.SqlQuery<IntentRow>(
                    "SELECT TOP (" + top + ") IntentID, UserID, Kind, CardTypeId, HolderID, CardNo, " +
                    "Face, Price, FeeInPercentage, PercentageFee, ExpectedTotal " +
                    "FROM dbo.tblT_Card_Funding_Intent WHERE Status = @st ORDER BY ID ASC",
                    P("@st", status)).ToList();
            }
        }

        private static SqlParameter P(string n, object v) { return new SqlParameter(n, v ?? DBNull.Value); }
    }
}
