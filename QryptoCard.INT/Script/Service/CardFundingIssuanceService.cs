using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq;
using QryptoCard.INT.Model.Service;

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
        // A money-moving intent (Funding/Confirming/Issuing) older than this is flagged for operator
        // review — a real forward/issue takes minutes, so this only trips on a genuinely stuck pipeline.
        private const int StuckAlertMinutes = 180;

        public static string RunTick()
        {
            if (!CardFundingIntentService.Enabled()) return "{\"skipped\":\"disabled\"}";

            int expired = ExpireStalePending();
            AlertStuckStreamingIntents();
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
            // Do NOT re-fetch the live catalog here: by this point the money is committed (the float is
            // already forwarded), and CardCatalogService.GetById returns null on a TRANSIENT WasabiCard
            // catalog hiccup — which would permanently fail a paid intent. The card type / holder need
            // was already resolved at intent creation; isNeedCardholder is purely cosmetic on the order
            // row and derives from the resolved HolderID.
            var x = new tblT_Card
            {
                ID = "QRYCRDBUY" + CounterService.Next(1).ToString("000000000000"),
                UserID = it.UserID,
                CardTypeId = it.CardTypeId,
                HolderID = it.HolderID,
                InitialDeposit = (double)it.Face,
                Price = (double)it.Price,
                FeeInPercentage = (double)it.FeeInPercentage,
                Fee = (double)it.PercentageFee,
                // Debit the FULL amount the customer deposited for this card (price + face + % fee +
                // fixed fee). Only `face` funds the card at WasabiCard; the rest is our margin/cost,
                // so the buffer nets to ~0 and the fixed fee is never gifted back as user residual.
                Total = (double)it.ExpectedTotal,
                ReceivedAmount = (double)it.Face,
                Currency = "USD",
                ReceivedCurrency = "USD",
                isNeedCardholder = it.HolderID.HasValue ? 1 : 0,
                DateExpired = DateTime.Now.AddHours(1),
                UserReferenceID = it.IntentID, // idempotency: a re-run replays, never double-issues
            };

            CardSpendService.SpendResult spend;
            try { spend = CardSpendService.OpenCard(x); }
            catch (Exception ex) { Trace.TraceError("IssueNew OpenCard threw for intent " + it.IntentID + ": " + ex.GetType().FullName); return Outcome.Pending; }

            // Read CardNo back from the STAMPED order row, not the stale in-memory x (OpenCard's
            // StampOpenSuccess updates a fresh DB entity, never x.CardNo) — else the intent completes
            // with CardNo=null and the app can't show the new card. TryCompleteNewFromOrder does this
            // and is idempotent.
            if (spend != null && spend.ProviderConfirmed) return TryCompleteNewFromOrder(it);
            // The card's float was already forwarded (Confirming) before this debit/open; a provider
            // reject leaves it orphaned, so route through FailReleasingSlot to emit the reconcile trace.
            if (spend != null && spend.ProviderFailed) return FailReleasingSlot(it, "open_failed", x.ID);
            // Definitive NON-provider failure (insufficient balance / debit fail, or a replay of an
            // order already terminally Failed): Success=false and NOT ProviderPending. Release the
            // intent AND the single-Issuing slot so one bad order can't block the whole pipeline. A
            // ProviderPending/ambiguous replay keeps Success=true and is NOT caught here.
            // Release ONLY on a genuinely terminal failure (Status == failed): insufficient balance /
            // debit fail, or a replay of an order already Failed. A still-Created replay (an overlapping
            // tick whose debit hasn't committed) also has Success=false but is NOT terminal — it must
            // stay Pending (retry), never be auto-failed while the sibling attempt is live.
            if (spend != null && !spend.Success && !spend.ProviderPending
                && string.Equals(spend.Status, StatusModel.Failed, StringComparison.OrdinalIgnoreCase))
                return FailReleasingSlot(it, spend.InsufficientBalance ? "insufficient_balance" : "debit_failed", x.ID);
            // Ambiguous, or a replay of an order a sweep/webhook has since finalized (a replay never
            // re-sets ProviderConfirmed). Reconcile against the order's OWN terminal state so the intent
            // still completes (Success) or releases (Failed) instead of sticking in Issuing forever.
            return TryCompleteNewFromOrder(it);
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
                FeeInPercentage = (double)it.FeeInPercentage,
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
            // Release ONLY on a genuinely terminal failure (Status == failed): insufficient balance /
            // debit fail, or a replay of an order already Failed. A still-Created replay (an overlapping
            // tick whose debit hasn't committed) also has Success=false but is NOT terminal — it must
            // stay Pending (retry), never be auto-failed while the sibling attempt is live.
            if (spend != null && !spend.Success && !spend.ProviderPending
                && string.Equals(spend.Status, StatusModel.Failed, StringComparison.OrdinalIgnoreCase))
                return FailReleasingSlot(it, spend.InsufficientBalance ? "insufficient_balance" : "debit_failed", x.ID);
            return TryCompleteTopUpFromOrder(it);
        }

        // Reconcile a new-card intent against its order's terminal state (handles the ambiguous-open /
        // replay case where the live SpendResult never carries ProviderConfirmed but a sweep/webhook has
        // since marked the order Success). Completes + binds CardNo when the order is Success; else Pending.
        private static Outcome TryCompleteNewFromOrder(IntentRow it)
        {
            try
            {
                using (var db = new DBEntities())
                {
                    var o = db.tblT_Card.FirstOrDefault(c => c.UserID == it.UserID && c.UserReferenceID == it.IntentID);
                    // Order status is a StatusModel value ("success"/"failed", lowercase) — compare
                    // case-insensitively against the constants, NOT a capitalized literal.
                    if (o != null && string.Equals(o.Status, StatusModel.Success, StringComparison.OrdinalIgnoreCase) && !string.IsNullOrEmpty(o.CardNo))
                        return CompleteWithCard(it, o.ID, o.CardNo);
                    // Order reconciled to a terminal failure (e.g. an ambiguous open the sweep resolved as
                    // failed + refunded the wallet): release the intent and the single-Issuing slot.
                    if (o != null && string.Equals(o.Status, StatusModel.Failed, StringComparison.OrdinalIgnoreCase))
                        return FailReleasingSlot(it, "order_failed", o.ID);
                }
            }
            catch (Exception ex) { Trace.TraceError("TryCompleteNewFromOrder failed for intent " + it.IntentID + ": " + ex.GetType().FullName); }
            return Outcome.Pending;
        }

        private static Outcome TryCompleteTopUpFromOrder(IntentRow it)
        {
            try
            {
                using (var db = new DBEntities())
                {
                    var o = db.tblT_Card_Deposit.FirstOrDefault(d => d.UserID == it.UserID && d.UserReferenceID == it.IntentID);
                    if (o != null && string.Equals(o.Status, StatusModel.Success, StringComparison.OrdinalIgnoreCase))
                        return CompleteWithCard(it, o.ID, it.CardNo);
                    if (o != null && string.Equals(o.Status, StatusModel.Failed, StringComparison.OrdinalIgnoreCase))
                        return FailReleasingSlot(it, "order_failed", o.ID);
                }
            }
            catch (Exception ex) { Trace.TraceError("TryCompleteTopUpFromOrder failed for intent " + it.IntentID + ": " + ex.GetType().FullName); }
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

        // Same as Fail, but for the case where the card's float was ALREADY forwarded to WasabiCard
        // (Confirming) and issuance then failed to draw it — the forward is stranded in the one-way
        // float with no card. Releasing the intent unblocks the single-Issuing pipeline; the loud trace
        // flags the orphaned forward for operator reconciliation. (Should be rare: the deposit covered
        // the full ExpectedTotal, so this needs the balance to have been drained after Funding.)
        private static Outcome FailReleasingSlot(IntentRow it, string note, string orderId)
        {
            Trace.TraceError("CardFundingIssuance: intent " + it.IntentID + " released as Failed (" + note +
                ") after its float was forwarded — order " + orderId + " did not draw it. Reconcile the " +
                "orphaned WasabiCard forward.");
            return Fail(it, note);
        }

        // Defense-in-depth: an intent stuck in a money-moving state (Funding/Confirming/Issuing) past
        // StuckAlertMinutes almost certainly needs manual reconciliation (ambiguous forward, or a
        // provider order that never reached a terminal state). Alert-only — NEVER auto-transition, since
        // money may be in flight and a wrong transition could double-move or strand it.
        private static void AlertStuckStreamingIntents()
        {
            try
            {
                using (var db = new DBEntities())
                {
                    // Status list built from the shared constants (not raw literals) so a rename can't
                    // silently drop stuck intents from this alert. Constants only — no user input.
                    string openMoving = "'" + CardFundingIntentService.StFunding + "','" +
                        CardFundingIntentService.StConfirming + "','" + CardFundingIntentService.StIssuing + "'";
                    var rows = db.Database.SqlQuery<string>(
                        "SELECT IntentID FROM dbo.tblT_Card_Funding_Intent " +
                        "WHERE Status IN (" + openMoving + ") AND UpdatedDate IS NOT NULL AND UpdatedDate < @cutoff",
                        P("@cutoff", DateTime.Now.AddMinutes(-StuckAlertMinutes))).ToList();
                    if (rows.Count > 0)
                        Trace.TraceError("CardFundingIssuance: " + rows.Count + " intent(s) stuck in a money-moving state > " +
                            StuckAlertMinutes + " min — reconcile: " + string.Join(",", rows.Take(20)));
                }
            }
            catch (Exception ex) { Trace.TraceError("AlertStuckStreamingIntents failed: " + ex.GetType().FullName); }
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
            // MUST be decimal to match the FeeInPercentage decimal(9,4) column: EF6 SqlQuery<T> does NOT
            // widen decimal->double, it throws on materialization. Reading it as double silently killed
            // the whole issuance tick every run (RT round 6). Narrowed to double at the assignment.
            public decimal FeeInPercentage { get; set; }
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
