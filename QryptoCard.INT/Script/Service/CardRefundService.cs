using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using Newtonsoft.Json;
using QryptoCard.INT.Model.Service;
using QryptoCard.INT.Model.WasabiCard;
using QryptoCard.INT.Script.Gateway.WasabiCard;

namespace QryptoCard.INT.Script.Service
{
    /// <summary>
    /// Admin-initiated card refund: cancels the whole WasabiCard at the provider and returns the card's
    /// remaining (unused) balance to the buyer's wallet, clawing back any referral commission paid on
    /// the card's orders. WasabiCard exposes no partial-withdraw, so a refund is always a WHOLE-CARD
    /// cancel — refunding from a top-up order id cancels the same card as refunding from the open order id.
    ///
    /// Finalizes SYNCHRONOUSLY from the cancelCard response (no inbound-webhook dependency): we cancel,
    /// read the outcome from the response, and credit in the same operation — deliberately unlike the
    /// buy flow, whose webhook dependence stranded orders when no callback was registered. Consistency
    /// mirrors CardSpendService, inverted:
    ///   - claim the open order Success -> RefundPending (one-row gate; a racing second refund finds 0
    ///     rows and stops, so a card cancels at most once);
    ///   - cancel at WasabiCard:
    ///       confirmed (code 200) -> credit the buyer + flip RefundPending -> Refunded atomically, deduped
    ///                               on the card number (one physical card == one refund);
    ///       definitive failure   -> revert RefundPending -> Success (no money moved);
    ///       ambiguous (null/throw) -> leave RefundPending for operator review; NEVER credit on an
    ///                               unconfirmed cancel.
    /// The buyer refund never hinges on the commission clawback: a referrer who already spent the
    /// commission yields "insufficient_balance", which is recorded (the wallet is never driven negative),
    /// not treated as a failure.
    /// </summary>
    public static class CardRefundService
    {
        // The per-card dedup unique index that makes the no-double-refund guarantee real (migration
        // 0010). Without it the dedup INSERT in CreditCardRefund cannot raise, so a re-triggered refund
        // could double-credit — so we fail closed when it is absent, exactly like ReferralCommissionService.
        const string DedupIndexName = "UIX_tblH_Partner_Webhook_ID_CardRefund_TXID";

        // Cache only the POSITIVE result: once the index exists it never disappears. While absent we
        // re-check every call so applying the migration takes effect without a service recycle.
        static volatile bool _dedupIndexVerified;

        static bool DedupIndexPresent(DBEntities db)
        {
            if (_dedupIndexVerified) return true;
            var n = db.Database.SqlQuery<int>(
                "SELECT COUNT(*) FROM sys.indexes WHERE name = @p0 AND object_id = OBJECT_ID('dbo.tblH_Partner_Webhook_ID')",
                DedupIndexName).FirstOrDefault();
            if (n > 0) { _dedupIndexVerified = true; return true; }
            return false;
        }

        public class RefundResult
        {
            public bool Success { get; set; }
            public string Outcome { get; set; }       // machine-readable outcome / failure reason
            public string CardNo { get; set; }
            public decimal RefundedAmount { get; set; }
            public decimal BuyerBalanceNew { get; set; }
            public int CommissionsReversed { get; set; }
            public string Message { get; set; }

            public static RefundResult Fail(string outcome, string message) =>
                new RefundResult { Success = false, Outcome = outcome, Message = message };
        }

        /// <summary>
        /// Refund the card associated with <paramref name="orderId"/> — a tblT_Card open-order id OR a
        /// tblT_Card_Deposit top-up id (both resolve to the same physical card). <paramref name="actor"/>
        /// is the acting admin email, recorded in the dedup/forensic payloads.
        /// </summary>
        public static RefundResult RefundByOrder(string orderId, string actor)
        {
            if (string.IsNullOrWhiteSpace(orderId))
                return RefundResult.Fail("missing_order", "An order id is required.");

            // 1. Resolve the order id to the card's OPEN order (the lifecycle anchor), cardNo + buyer.
            string openOrderId, cardNo, buyerId, openStatus;
            using (var db = new DBEntities())
            {
                var open = db.tblT_Card.FirstOrDefault(p => p.ID == orderId);
                if (open == null)
                {
                    // Not an open-order id — maybe a top-up id; resolve to its card's open order.
                    var top = db.tblT_Card_Deposit.FirstOrDefault(p => p.ID == orderId);
                    if (top == null)
                        return RefundResult.Fail("order_not_found", "No card order found for that id.");
                    open = db.tblT_Card.FirstOrDefault(p => p.CardNo != null && p.CardNo == top.CardNo);
                    if (open == null)
                        return RefundResult.Fail("card_open_not_found", "Could not locate the card's open order.");
                }
                openOrderId = open.ID;
                cardNo = open.CardNo;
                buyerId = open.UserID;
                openStatus = open.Status;
            }

            if (string.IsNullOrEmpty(cardNo))
                return RefundResult.Fail("card_not_issued", "The card has no provider card number — nothing to cancel.");

            // Fail closed if the per-card dedup index is missing — refuse BEFORE any provider cancel so we
            // never cancel a card whose buyer credit we could not safely dedup (double-refund risk).
            using (var db = new DBEntities())
            {
                if (!DedupIndexPresent(db))
                {
                    Trace.TraceError("Refund: dedup index " + DedupIndexName + " is missing — refusing to refund card " +
                        cardNo + ". Apply migration 0010-card-refund-dedup-indexes.sql.");
                    return RefundResult.Fail("dedup_index_missing", "Refund is unavailable until the dedup index is deployed.");
                }
            }

            // RESUME path: a prior attempt cancelled the card (order left RefundPending with a persisted
            // intent) but the buyer credit did not apply. Complete it from the persisted amount — no second
            // cancel, no getCardInfo (the card is cancelled now); the cardNo dedup still guarantees one
            // credit. With NO intent, the prior cancel was never confirmed, so we cannot safely resume.
            if (string.Equals(openStatus, StatusModel.RefundPending, StringComparison.OrdinalIgnoreCase))
            {
                decimal? intent = ReadRefundIntent(cardNo);
                if (intent == null)
                    return RefundResult.Fail("refund_pending_unconfirmed",
                        "Refund is pending an unconfirmed provider cancel — needs manual review (this call moved no funds).");
                return CompleteCredit(openOrderId, cardNo, buyerId, intent.Value, actor);
            }

            if (!string.Equals(openStatus, StatusModel.Success, StringComparison.OrdinalIgnoreCase))
                return RefundResult.Fail("not_refundable", "Card is not in a refundable (success) state; current=" + openStatus + ".");

            // 2. Read the card's available balance — a GATE (must be > 0) and an upper BOUND on the credit.
            // It is NOT the authoritative credit figure: that comes from the cancel response below, so
            // spend (or a cancel fee) in the getCardInfo->cancel window can never mint money.
            var info = WasabiCardService.getCardInfo(new WCCardInfoRequestModel { cardNo = cardNo, onlySimpleInfo = false });
            if (info == null || info.data == null || info.data.balanceInfo == null)
                return RefundResult.Fail("card_lookup_failed", "Could not read the card from the provider.");
            decimal availableBefore = ParseAmount(info.data.balanceInfo.amount);
            if (availableBefore <= 0m)
                return RefundResult.Fail("no_balance", "The card has no refundable balance (already spent or empty).");

            // 3. Concurrency gate: claim the open order Success -> RefundPending (exactly one winner).
            if (ClaimCardStatus(openOrderId, StatusModel.Success, StatusModel.RefundPending) != 1)
                return RefundResult.Fail("refund_in_progress", "A refund for this card is already in progress or done.");

            // 4. Cancel the whole card at WasabiCard. The cancel needs its OWN merchantOrderNo —
            // WasabiCard rejects reusing the open order's id with "Duplicate order number" (the card
            // was created under that id). Deterministic (open id + "-RFD") so a resumed/retried cancel
            // reuses the same id rather than minting a fresh one each attempt.
            WCCancelCardResponseModel cancel;
            try
            {
                cancel = WasabiCardService.cancelCard(new WCCancelCardRequestModel { cardNo = cardNo, merchantOrderNo = openOrderId + "-RFD" });
            }
            catch (Exception ex)
            {
                // Throw == ambiguous: leave RefundPending, never credit. (No intent persisted: the cancel
                // is unconfirmed, so the RefundPending branch will route to manual review, not auto-credit.)
                Trace.TraceError("Refund: cancelCard threw for card " + cardNo + " order " + openOrderId + " — left RefundPending: " + ex.Message);
                return RefundResult.Fail("cancel_ambiguous", "Provider cancel could not be confirmed; left pending for review. No funds moved.");
            }

            if (cancel == null)
            {
                Trace.TraceError("Refund: cancelCard returned null (ambiguous) for card " + cardNo + " order " + openOrderId + " — left RefundPending.");
                return RefundResult.Fail("cancel_ambiguous", "Provider cancel could not be confirmed; left pending for review. No funds moved.");
            }
            if (cancel.code != 200)
            {
                // Definitive rejection: revert the claim, no money moved.
                ClaimCardStatus(openOrderId, StatusModel.RefundPending, StatusModel.Success);
                return RefundResult.Fail("cancel_rejected", "Provider rejected the cancel: " + (cancel.msg ?? "unknown") + ".");
            }

            // 5. Cancel confirmed. Credit the amount the provider ACTUALLY returned to the merchant wallet
            // (authoritative), capped at the pre-cancel available balance — never credit a value the cancel
            // did not confirm. This closes the getCardInfo->cancel spend / cancel-fee race.
            decimal returned = CancelReturnedAmount(cancel);
            decimal credited = Math.Min(availableBefore, returned);
            if (credited <= 0m)
            {
                // Cancel succeeded but reported no funds returned — do NOT credit a stale figure; leave
                // pending for review (no intent persisted => the resume path routes to manual review).
                Trace.TraceError("Refund: card " + cardNo + " cancelled but provider returned <= 0 (returned=" + returned +
                    ", availableBefore=" + availableBefore + ") for order " + openOrderId + " — left RefundPending for review.");
                return RefundResult.Fail("cancel_zero_return", "Card cancelled but the provider returned no funds; left pending for review.");
            }

            // Persist the confirmed amount BEFORE the credit, so a credit failure is RESUMABLE: re-running
            // this refund reads the intent (RefundPending branch above) and completes without re-cancelling.
            WriteRefundIntent(cardNo, credited, buyerId, openOrderId, actor);

            return CompleteCredit(openOrderId, cardNo, buyerId, credited, actor);
        }

        // Credit the buyer (claim RefundPending -> Refunded, deduped per card), then mark top-ups refunded
        // and claw back commissions. Shared by the fresh-cancel and resume paths; idempotent via the cardNo
        // dedup. The caller has already cancelled the card and persisted the confirmed amount.
        static RefundResult CompleteCredit(string openOrderId, string cardNo, string buyerId, decimal amount, string actor)
        {
            WalletService.EnsureWallet(buyerId); // an absent buyer wallet would otherwise fail closed (wallet_missing)

            string claimSql = "UPDATE dbo.tblT_Card SET Status = '" + StatusModel.Refunded +
                              "', DateModified = GETDATE() WHERE ID = @id AND Status = '" + StatusModel.RefundPending + "'";
            var credit = WalletService.CreditCardRefund(
                buyerUserId: buyerId,
                amount: amount,
                orderId: openOrderId,
                dedupKey: cardNo,
                claimSql: claimSql,
                claimParams: new[] { new SqlParameter("@id", openOrderId) },
                dedupRequest: JsonConvert.SerializeObject(new { source = "admin_refund", by = actor, cardNo, amount }, Formatting.None));

            if (!credit.Success)
            {
                if (credit.FailureReason == "duplicate_event" || credit.FailureReason == "claim_lost")
                    // Already refunded (per-card dedup) or the order already left RefundPending — idempotent no-op.
                    return RefundResult.Fail("already_refunded", "This card has already been refunded.");

                // Provider cancel committed but the buyer credit did not apply (e.g. a transient DB error).
                // The funds are back in the merchant wallet and the confirmed amount is persisted as an
                // intent, so re-running this refund RESUMES and completes the credit (idempotent on the card).
                Trace.TraceError("Refund: card " + cardNo + " cancelled but buyer credit failed (" + credit.FailureReason +
                    ") for order " + openOrderId + " — RefundPending with persisted intent; re-run to complete.");
                return RefundResult.Fail("credit_failed_after_cancel",
                    "Card was cancelled but the wallet credit did not apply (" + credit.FailureReason +
                    "). Funds are safe in the merchant wallet; re-run this refund to complete.");
            }

            MarkTopUpsRefunded(cardNo);
            int reversed = ReverseCommissionsForCard(openOrderId, cardNo, actor);

            return new RefundResult
            {
                Success = true,
                Outcome = "refunded",
                CardNo = cardNo,
                RefundedAmount = amount,
                BuyerBalanceNew = credit.BalanceNew,
                CommissionsReversed = reversed,
                Message = "Refunded " + amount.ToString("0.00", CultureInfo.InvariantCulture) + " USDT to the buyer; card cancelled."
            };
        }

        // The amount the cancel actually returned to the merchant wallet: prefer receivedAmount (net of any
        // provider cancel fee), fall back to amount; 0 when the response carries no data.
        static decimal CancelReturnedAmount(WCCancelCardResponseModel cancel)
        {
            if (cancel == null || cancel.data == null) return 0m;
            if (cancel.data.receivedAmount > 0) return cancel.data.receivedAmount;
            if (cancel.data.amount > 0) return cancel.data.amount;
            return 0m;
        }

        // Persist the cancel-confirmed refund amount for a card (idempotent: at most one per card). Lets a
        // credit failure after a successful cancel be resumed without re-cancelling or re-reading the (now
        // cancelled) card. Stored in the shared partner-webhook journal, keyed by card number.
        const string IntentType = "CardRefundIntent";

        static void WriteRefundIntent(string cardNo, decimal amount, string buyerId, string openOrderId, string actor)
        {
            try
            {
                using (var db = new DBEntities())
                {
                    var existing = db.Database.SqlQuery<int>(
                        "SELECT COUNT(*) FROM dbo.tblH_Partner_Webhook_ID WHERE Type = @p0 AND TXID = @p1",
                        new SqlParameter("@p0", IntentType), new SqlParameter("@p1", cardNo)).FirstOrDefault();
                    if (existing > 0) return;
                    db.Database.ExecuteSqlCommand(
                        "INSERT INTO dbo.tblH_Partner_Webhook_ID (Type, TXID, Request, RequestDate) VALUES (@t, @x, @r, GETDATE())",
                        new SqlParameter("@t", IntentType), new SqlParameter("@x", cardNo),
                        new SqlParameter("@r", JsonConvert.SerializeObject(new { amount, buyerId, openOrderId, by = actor }, Formatting.None)));
                }
            }
            catch (Exception ex) { Trace.TraceError("Refund: writing refund intent for card " + cardNo + " failed: " + ex.Message); }
        }

        // The persisted refund amount for a card, or null if none (the cancel was never confirmed).
        static decimal? ReadRefundIntent(string cardNo)
        {
            try
            {
                using (var db = new DBEntities())
                {
                    var json = db.Database.SqlQuery<string>(
                        "SELECT TOP 1 Request FROM dbo.tblH_Partner_Webhook_ID WHERE Type = @p0 AND TXID = @p1 ORDER BY RequestDate DESC",
                        new SqlParameter("@p0", IntentType), new SqlParameter("@p1", cardNo)).FirstOrDefault();
                    if (string.IsNullOrEmpty(json)) return null;
                    var amt = Newtonsoft.Json.Linq.JObject.Parse(json).Value<decimal?>("amount");
                    return (amt.HasValue && amt.Value > 0m) ? amt : (decimal?)null;
                }
            }
            catch (Exception ex) { Trace.TraceError("Refund: reading refund intent for card " + cardNo + " failed: " + ex.Message); return null; }
        }

        // ---- helpers -------------------------------------------------------------

        // Conditional one-row status transition on the open order; returns rows affected (1 = claimed).
        static int ClaimCardStatus(string orderId, string from, string to)
        {
            using (var db = new DBEntities())
                return db.Database.ExecuteSqlCommand(
                    "UPDATE dbo.tblT_Card SET Status = @to, DateModified = GETDATE() WHERE ID = @id AND Status = @from",
                    new SqlParameter("@to", to), new SqlParameter("@id", orderId), new SqlParameter("@from", from));
        }

        static void MarkTopUpsRefunded(string cardNo)
        {
            try
            {
                // Include in-flight top-ups: a deposit that was InProgress when the card was cancelled
                // would otherwise stay InProgress forever against a now-cancelled card.
                using (var db = new DBEntities())
                    db.Database.ExecuteSqlCommand(
                        "UPDATE dbo.tblT_Card_Deposit SET Status = @to WHERE CardNo = @c AND Status IN ('" +
                        StatusModel.Success + "', '" + StatusModel.InProgress + "', '" + StatusModel.PendingProvider + "')",
                        new SqlParameter("@to", StatusModel.Refunded), new SqlParameter("@c", cardNo));
            }
            catch (Exception ex) { Trace.TraceError("Refund: marking top-ups refunded failed for card " + cardNo + ": " + ex.Message); }
        }

        // Reverse every referral commission paid on this card's orders. A card's order ids are its open
        // order plus all its top-ups; each commission row's TransactionID is one of those order ids.
        static int ReverseCommissionsForCard(string openOrderId, string cardNo, string actor)
        {
            int reversed = 0;
            try
            {
                using (var db = new DBEntities())
                {
                    var orderIds = new List<string> { openOrderId };
                    orderIds.AddRange(db.tblT_Card_Deposit.Where(p => p.CardNo == cardNo).Select(p => p.ID).ToList());

                    var comms = db.tblT_Commission.Where(c => orderIds.Contains(c.TransactionID)).ToList();
                    foreach (var c in comms)
                    {
                        decimal amt = (decimal)(c.Commission ?? 0d);
                        if (amt <= 0m || string.IsNullOrEmpty(c.UserID)) continue;
                        WalletService.EnsureWallet(c.UserID);
                        var rev = WalletService.ReverseReferralCommission(c.UserID, amt, c.TransactionID,
                            JsonConvert.SerializeObject(new { source = "admin_refund_commission_reversal", by = actor, cardNo }, Formatting.None));
                        if (rev.Success) reversed++;
                        else if (rev.FailureReason != "duplicate_event")
                            Trace.TraceError("Refund: commission clawback for order " + c.TransactionID + " referrer " +
                                c.UserID + " did not apply (" + rev.FailureReason + ") — refund stands, shortfall noted.");
                    }
                }
            }
            catch (Exception ex) { Trace.TraceError("Refund: commission reversal sweep failed for card " + cardNo + ": " + ex.Message); }
            return reversed;
        }

        static decimal ParseAmount(string s)
        {
            decimal v;
            return decimal.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out v) ? v : 0m;
        }
    }
}
