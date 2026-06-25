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

            // 1. Persist the order up front.
            x.Status = StatusModel.Created;
            x.isActive = 0;
            if (x.DateCreated == null) x.DateCreated = DateTime.Now;
            using (var ctx = new DBEntities())
            {
                ctx.tblT_Card.Add(x);
                ctx.SaveChanges();
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
                return new SpendResult { Success = true, ProviderConfirmed = true, Status = StatusModel.InProgress, Message = "Card opening in progress" };
            }
            if (definitiveFailure)
            {
                // Reverse the debit. If the reversal itself fails (transient DB error / throw), do NOT
                // mark Failed — leave the order PendingProvider (where the debit atomically landed) so
                // the reconciler retries the reversal, and log it as money-affecting.
                if (TryReverse(x.UserID, total, WalletService.TypeCardOpenReversal, x.ID))
                {
                    SetCardStatus(x.ID, StatusModel.Failed);
                    return new SpendResult { Success = false, ProviderFailed = true, Status = StatusModel.Failed, Message = "Card provider rejected the request; balance refunded" };
                }
                System.Diagnostics.Trace.TraceError("Card-open reversal failed for order " + x.ID + " — left pending-provider for reconciliation (money-affecting)");
                return new SpendResult { Success = false, ProviderFailed = true, Status = StatusModel.PendingProvider, Message = "Card provider rejected the request; refund pending reconciliation" };
            }
            // Ambiguous: the debit already landed and the order is already PendingProvider (set
            // atomically with the debit). Leave it; reconcile later. Never auto-reverse.
            return new SpendResult { Success = true, ProviderPending = true, Status = StatusModel.PendingProvider, Message = "Card opening pending provider confirmation" };
        }

        // Attempt a compensating reversal credit; true only if it definitively succeeded. A throw
        // (transient DB error) or a non-success result returns false so the caller can leave the
        // order in a reconcilable state rather than falsely marking it refunded.
        private static bool TryReverse(string userId, decimal amount, string type, string orderId)
        {
            try
            {
                var rev = WalletService.Credit(userId, amount, 0m, 0d, type, orderId);
                return rev.Success;
            }
            catch
            {
                return false;
            }
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
                return new SpendResult { Success = true, ProviderConfirmed = true, Status = StatusModel.InProgress, Message = "Top-up in progress" };
            }
            if (definitiveFailure)
            {
                if (TryReverse(x.UserID, total, WalletService.TypeCardTopupReversal, x.ID))
                {
                    SetDepositStatus(x.ID, StatusModel.Failed);
                    return new SpendResult { Success = false, ProviderFailed = true, Status = StatusModel.Failed, Message = "Card provider rejected the top-up; balance refunded" };
                }
                System.Diagnostics.Trace.TraceError("Top-up reversal failed for order " + x.ID + " — left pending-provider for reconciliation (money-affecting)");
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
