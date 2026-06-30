using System;
using System.Data.SqlClient;
using System.Linq;
using QryptoCard.INT.Model.Service;
using QryptoCard.INT.Model.WasabiCard;
using QryptoCard.INT.Script.Gateway.WasabiCard;

namespace QryptoCard.INT.Script.Service
{
    /// <summary>
    /// Funds a card from the user's prepaid balance. The mechanic is debit-first, then provision,
    /// then reconcile — ported from the runegate withdrawal path:
    ///
    ///   1. Persist the order (so every step has a durable audit row).
    ///   2. Debit the balance atomically and COMMIT before any provider call (C3.b): never call
    ///      WasabiCard against an undebited balance. A debit that would overdraw fails closed.
    ///   3. Call WasabiCard.
    ///   4. Reconcile on the provider outcome:
    ///        - confirmed (code == 200): order InProgress, debit stands, the WasabiCard webhook
    ///          finalizes the card.
    ///        - definitive failure (non-null, non-200 business error): reverse the debit with a
    ///          compensating credit and fail the order.
    ///        - ambiguous (null / timeout): do NOT auto-reverse (the card may actually exist —
    ///          reversing would hand out a free card, C3.c); leave the debit, mark the order
    ///          PendingProvider, and reconcile via the WasabiCard webhook / a sweep.
    ///
    /// Provisioning lives here (factored out of the old deposit callback, C3.a); the callback no
    /// longer provisions from a deposit.
    /// </summary>
    public static class CardSpendService
    {
        public class SpendResult
        {
            public bool Success { get; set; }            // debit landed and an order exists (confirmed or pending)
            public bool InsufficientBalance { get; set; }
            public bool ProviderConfirmed { get; set; }
            public bool ProviderPending { get; set; }
            public bool ProviderFailed { get; set; }
            public string Status { get; set; }           // final order status
            public string Message { get; set; }
        }

        /// <summary>
        /// Open a card from balance. <paramref name="x"/> must already have ID, UserID, CardTypeId,
        /// InitialDeposit, Fee, Total, and (optionally) HolderID set by the caller.
        /// </summary>
        public static SpendResult OpenCard(tblT_Card x)
        {
            decimal total = Convert.ToDecimal(x.Total);

            // 0. Reject a fractional provider amount: WasabiCard is funded in integer units, so a
            // fractional deposit would under-fund the card vs. the (decimal) wallet debit. Fail closed.
            decimal deposit = Convert.ToDecimal(x.InitialDeposit);
            if (deposit != decimal.Truncate(deposit))
                return new SpendResult { Success = false, Status = StatusModel.Failed, Message = "Deposit amount must be a whole number" };

            // 1. Persist the order up front. IDEMPOTENCY: a double-click / two-tab / Back-resubmit
            // carries the SAME per-attempt UserReferenceID, which collides on the filtered unique
            // index UIX_tblT_Card_User_Ref (UserID, UserReferenceID). Catch that and REPLAY the
            // original order — no second insert, no second debit, no second card. (A check-then-insert
            // in app code races; the DB constraint is the authority, matching the webhook/referral
            // dedup pattern.)
            x.Status = StatusModel.Created;
            x.isActive = 0;
            if (x.DateCreated == null) x.DateCreated = DateTime.Now;
            try
            {
                using (var ctx = new DBEntities())
                {
                    ctx.tblT_Card.Add(x);
                    ctx.SaveChanges();
                }
            }
            catch (Exception dupEx) when (!string.IsNullOrWhiteSpace(x.UserReferenceID) && WalletService.IsDuplicateKey(dupEx))
            {
                // The winning submit already created (and owns the money path for) this order.
                // Return ITS outcome, pointing the caller's x.ID at the original order. No debit here.
                using (var re = new DBEntities())
                {
                    var existing = re.tblT_Card
                        .Where(p => p.UserID == x.UserID && p.UserReferenceID == x.UserReferenceID)
                        .OrderBy(p => p.DateCreated)
                        .FirstOrDefault();
                    if (existing != null)
                    {
                        x.ID = existing.ID;
                        x.Status = existing.Status;
                        return new SpendResult
                        {
                            // A still-Created original hasn't run its debit yet — don't report success to
                            // a racing replay before the winner's money path resolves; Failed is terminal.
                            Success = existing.Status != StatusModel.Failed && existing.Status != StatusModel.Created,
                            Status = existing.Status,
                            Message = "Request already submitted"
                        };
                    }
                }
                // Re-read found nothing (extremely rare race) — fail closed, never debit.
                return new SpendResult { Success = false, Status = StatusModel.Failed, Message = "Request already submitted" };
            }

            // 2. Debit-first, atomically transitioning the order Created -> PendingProvider in the
            // SAME transaction. A crash can then never strand a committed debit on a Created order
            // (which nothing reconciles). Ledger TransactionID = order ID.
            string claimSql = "UPDATE dbo.tblT_Card SET Status = '" + StatusModel.PendingProvider +
                              "' WHERE ID = @id AND Status = '" + StatusModel.Created + "'";
            var debit = WalletService.DebitForOrder(x.UserID, total, WalletService.TypeCardOpen, x.ID,
                claimSql, new[] { new SqlParameter("@id", x.ID) });
            if (!debit.Success)
            {
                SetCardStatus(x.ID, StatusModel.Failed);
                bool insufficient = debit.FailureReason == "insufficient_balance";
                return new SpendResult
                {
                    Success = false,
                    InsufficientBalance = insufficient,
                    Status = StatusModel.Failed,
                    Message = insufficient ? "Insufficient balance" : "Could not debit balance"
                };
            }
            // The order is now PendingProvider, atomic with the debit.

            // 3. Provision via WasabiCard. A throw (timeout / unreachable / config) is treated the
            // same as a null result: ambiguous, never auto-reverse — see the reconcile step.
            int amount = Convert.ToInt32(x.InitialDeposit);
            WCOpenCardResponseModel res = null;
            WCOpenCardWithHolderResponseModel resH = null;
            try
            {
                if (x.HolderID == null)
                {
                    res = WasabiCardService.openCard(new WCOpenCardRequestModel
                    {
                        merchantOrderNo = x.ID,
                        cardTypeId = x.CardTypeId.ToString(),
                        amount = amount
                    });
                }
                else
                {
                    resH = WasabiCardService.openCardWithHolder(new WCOpenCardWithHolderRequestModel
                    {
                        merchantOrderNo = x.ID,
                        cardTypeId = Convert.ToInt32(x.CardTypeId),
                        holderId = Convert.ToInt32(x.HolderID),
                        amount = amount
                    });
                }
            }
            catch
            {
                res = null;
                resH = null;
            }

            bool confirmed = (res != null && res.code == 200) || (resH != null && resH.code == 200);
            bool definitiveFailure = (res != null && res.code != 200) || (resH != null && resH.code != 200);

            // 4. Reconcile.
            if (confirmed)
            {
                StampOpenSuccess(x.ID, res, resH);

                // Finalize synchronously from the provider's open response (it carries the cardNo): bind
                // the card, activate, mark Success, record balance, and pay the referral commission NOW,
                // instead of depending on the inbound WasabiCard webhook (not delivered in this
                // deployment). Best-effort: the cardNo is already stamped above, so if this can't confirm
                // yet the order stays InProgress and is recoverable; a later webhook/sweep finalize is an
                // idempotent no-op (the order is already Success, and commission is deduped per order).
                try
                {
                    var cardNo = res?.data?.FirstOrDefault()?.cardNo ?? resH?.data?.FirstOrDefault()?.cardNo;
                    if (!string.IsNullOrEmpty(cardNo))
                        CardBuyFinalizationService.FinalizeOpenSuccess(x.ID, cardNo);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Trace.TraceError(
                        "Synchronous open finalize failed for order " + x.ID +
                        " (left InProgress for sweep/webhook): " + ex.Message);
                }
                return new SpendResult { Success = true, ProviderConfirmed = true, Status = StatusModel.InProgress, Message = "Card opening in progress" };
            }
            if (definitiveFailure)
            {
                // Reverse the debit atomically with the order's -> Failed transition (claim-gated), so
                // there is no window in which the refund is issued while the order still reads
                // PendingProvider (which the deposit-fail webhook / reTopup could double-act on), and a
                // second reversal is a no-op. Only if the reversal cannot apply at all do we leave the
                // order PendingProvider for reconciliation.
                if (ReverseFailedSpend("tblT_Card", x.UserID, total, WalletService.TypeCardOpenReversal, x.ID))
                    return new SpendResult { Success = false, ProviderFailed = true, Status = StatusModel.Failed, Message = "Card provider rejected the request; balance refunded" };
                System.Diagnostics.Trace.TraceError("Card-open reversal could not apply for order " + x.ID + " — left pending-provider for reconciliation (money-affecting)");
                return new SpendResult { Success = false, ProviderFailed = true, Status = StatusModel.PendingProvider, Message = "Card provider rejected the request; refund pending reconciliation" };
            }
            // Ambiguous: the debit already landed and the order is already PendingProvider (set
            // atomically with the debit). Leave it; reconcile later. Never auto-reverse.
            return new SpendResult { Success = true, ProviderPending = true, Status = StatusModel.PendingProvider, Message = "Card opening pending provider confirmation" };
        }

        // Reverse a failed spend's debit, atomically flipping the order PendingProvider/InProgress ->
        // Failed in the SAME transaction (claim-gated, idempotent). Returns true when the refund has
        // definitively landed — either this call committed it, or a prior reversal already finalized
        // the order ("claim_lost" => order no longer reversible => refund already issued, a safe
        // no-op). Returns false only when the reversal could not apply at all (e.g. no wallet row), so
        // the caller leaves the order PendingProvider for the reconciler rather than claiming a refund.
        private static bool ReverseFailedSpend(string table, string userId, decimal amount, string type, string orderId)
        {
            string claimSql = "UPDATE dbo." + table + " SET Status = '" + StatusModel.Failed +
                              "' WHERE ID = @id AND Status IN ('" + StatusModel.PendingProvider +
                              "', '" + StatusModel.InProgress + "')";
            WalletService.BalanceMutationResult rev;
            try
            {
                rev = WalletService.ReverseForOrder(userId, amount, type, orderId,
                    claimSql, new[] { new SqlParameter("@id", orderId) });
            }
            catch
            {
                return false; // transient error — leave PendingProvider for reconciliation
            }
            return rev.Success || rev.FailureReason == "claim_lost";
        }

        /// <summary>
        /// Top up an existing card from balance. <paramref name="x"/> must already have ID, UserID,
        /// CardNo, Amount, Fee, and Total set by the caller.
        /// </summary>
        public static SpendResult TopUp(tblT_Card_Deposit x)
        {
            decimal total = Convert.ToDecimal(x.Total);

            // Reject a fractional provider amount (integer-funded; see OpenCard).
            decimal topup = Convert.ToDecimal(x.Amount);
            if (topup != decimal.Truncate(topup))
                return new SpendResult { Success = false, Status = StatusModel.Failed, Message = "Top-up amount must be a whole number" };

            x.Status = StatusModel.Created;
            if (x.DateTransaction == null) x.DateTransaction = DateTime.Now;
            using (var ctx = new DBEntities())
            {
                ctx.tblT_Card_Deposit.Add(x);
                ctx.SaveChanges();
            }

            // Debit-first, atomically transitioning the order Created -> PendingProvider (see OpenCard).
            string claimSql = "UPDATE dbo.tblT_Card_Deposit SET Status = '" + StatusModel.PendingProvider +
                              "' WHERE ID = @id AND Status = '" + StatusModel.Created + "'";
            var debit = WalletService.DebitForOrder(x.UserID, total, WalletService.TypeCardTopup, x.ID,
                claimSql, new[] { new SqlParameter("@id", x.ID) });
            if (!debit.Success)
            {
                SetDepositStatus(x.ID, StatusModel.Failed);
                bool insufficient = debit.FailureReason == "insufficient_balance";
                return new SpendResult
                {
                    Success = false,
                    InsufficientBalance = insufficient,
                    Status = StatusModel.Failed,
                    Message = insufficient ? "Insufficient balance" : "Could not debit balance"
                };
            }
            // The order is now PendingProvider, atomic with the debit.

            int amount = Convert.ToInt32(x.Amount);
            WCDepositCardResponseModel res = null;
            try
            {
                res = WasabiCardService.depositCard(new WCDepositCardRequestModel
                {
                    merchantOrderNo = x.ID,
                    cardNo = x.CardNo,
                    amount = amount
                });
            }
            catch
            {
                res = null;
            }

            bool confirmed = res != null && res.code == 200;
            bool definitiveFailure = res != null && res.code != 200;

            if (confirmed)
            {
                StampTopUpSuccess(x.ID, res);

                // Finalize the top-up synchronously (mark Success + pay the referral commission NOW)
                // instead of depending on the inbound WasabiCard webhook, which is not delivered in this
                // deployment — without this the top-up strands InProgress with no commission. Best-effort:
                // the deposit row is already stamped, so a finalize hiccup leaves it recoverable, and a
                // later webhook/sweep finalize is an idempotent no-op (commission is deduped per order).
                try
                {
                    CardBuyFinalizationService.FinalizeTopUpSuccess(x.ID);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Trace.TraceError(
                        "Synchronous top-up finalize failed for order " + x.ID +
                        " (left InProgress for sweep/webhook): " + ex.Message);
                }
                return new SpendResult { Success = true, ProviderConfirmed = true, Status = StatusModel.InProgress, Message = "Top-up in progress" };
            }
            if (definitiveFailure)
            {
                // Claim-gated, atomic reversal (see OpenCard) — closes the double-refund/double-fund
                // window where the deposit-fail webhook or reTopup could act on a PendingProvider order
                // whose refund had already been issued.
                if (ReverseFailedSpend("tblT_Card_Deposit", x.UserID, total, WalletService.TypeCardTopupReversal, x.ID))
                    return new SpendResult { Success = false, ProviderFailed = true, Status = StatusModel.Failed, Message = "Card provider rejected the top-up; balance refunded" };
                System.Diagnostics.Trace.TraceError("Top-up reversal could not apply for order " + x.ID + " — left pending-provider for reconciliation (money-affecting)");
                return new SpendResult { Success = false, ProviderFailed = true, Status = StatusModel.PendingProvider, Message = "Card provider rejected the top-up; refund pending reconciliation" };
            }
            // Ambiguous: debit landed; order already PendingProvider (set atomically with the debit).
            return new SpendResult { Success = true, ProviderPending = true, Status = StatusModel.PendingProvider, Message = "Top-up pending provider confirmation" };
        }

        // ---- order-row updates (short-lived contexts) ----------------------------

        private static void SetCardStatus(string id, string status)
        {
            using (var ctx = new DBEntities())
            {
                var row = ctx.tblT_Card.FirstOrDefault(p => p.ID == id);
                if (row != null) { row.Status = status; row.DateModified = DateTime.Now; ctx.SaveChanges(); }
            }
        }

        private static void SetDepositStatus(string id, string status)
        {
            using (var ctx = new DBEntities())
            {
                var row = ctx.tblT_Card_Deposit.FirstOrDefault(p => p.ID == id);
                if (row != null) { row.Status = status; ctx.SaveChanges(); }
            }
        }

        private static void StampOpenSuccess(string id, WCOpenCardResponseModel res, WCOpenCardWithHolderResponseModel resH)
        {
            using (var ctx = new DBEntities())
            {
                var row = ctx.tblT_Card.FirstOrDefault(p => p.ID == id);
                if (row == null) return;
                if (res != null && res.data != null && res.data.Count > 0)
                {
                    var d = res.data[0];
                    row.CardNo = d.cardNo;
                    row.OrderNo = d.orderNo;
                    row.BaseAmount = ToDoubleOrNull(d.amount);
                    row.BaseFee = ToDoubleOrNull(d.fee);
                    row.ReceivedCurrency = d.receivedCurrency;
                    row.Currency = d.currency;
                    row.Param4 = d.status;
                    row.Param5 = d.type;
                }
                else if (resH != null && resH.data != null && resH.data.Count > 0)
                {
                    var d = resH.data[0];
                    row.CardNo = d.cardNo;
                    row.OrderNo = d.orderNo;
                    row.BaseAmount = ToDoubleOrNull(d.amount);
                    row.BaseFee = ToDoubleOrNull(d.fee);
                    row.ReceivedCurrency = d.receivedCurrency;
                    row.Currency = d.currency;
                    row.Param4 = d.status;
                    row.Param5 = d.type;
                }
                row.Status = StatusModel.InProgress;
                row.DateModified = DateTime.Now;
                ctx.SaveChanges();
            }
        }

        private static void StampTopUpSuccess(string id, WCDepositCardResponseModel res)
        {
            using (var ctx = new DBEntities())
            {
                var row = ctx.tblT_Card_Deposit.FirstOrDefault(p => p.ID == id);
                if (row == null) return;
                if (res.data != null)
                {
                    row.OrderNo = res.data.orderNo;
                    row.BaseFee = ToDoubleOrNull(res.data.fee);
                    row.Currency = res.data.currency;
                    row.Param4 = res.data.status;
                    row.Param5 = res.data.type;
                }
                row.Status = StatusModel.InProgress;
                ctx.SaveChanges();
            }
        }

        private static double? ToDoubleOrNull(string s)
        {
            double v;
            return double.TryParse(s, out v) ? (double?)v : null;
        }
    }
}
