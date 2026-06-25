using System;
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

            // 1. Persist the order up front.
            x.Status = StatusModel.Created;
            x.isActive = 0;
            if (x.DateCreated == null) x.DateCreated = DateTime.Now;
            using (var ctx = new DBEntities())
            {
                ctx.tblT_Card.Add(x);
                ctx.SaveChanges();
            }

            // 2. Debit-first (commits before the provider call). Ledger TransactionID = order ID.
            var debit = WalletService.Debit(x.UserID, total, WalletService.TypeCardOpen, x.ID);
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
                WalletService.Credit(x.UserID, total, 0m, 0d, WalletService.TypeCardOpenReversal, x.ID);
                SetCardStatus(x.ID, StatusModel.Failed);
                return new SpendResult { Success = false, ProviderFailed = true, Status = StatusModel.Failed, Message = "Card provider rejected the request; balance refunded" };
            }
            // Ambiguous: leave the debit, reconcile later. Never auto-reverse.
            SetCardStatus(x.ID, StatusModel.PendingProvider);
            return new SpendResult { Success = true, ProviderPending = true, Status = StatusModel.PendingProvider, Message = "Card opening pending provider confirmation" };
        }

        /// <summary>
        /// Top up an existing card from balance. <paramref name="x"/> must already have ID, UserID,
        /// CardNo, Amount, Fee, and Total set by the caller.
        /// </summary>
        public static SpendResult TopUp(tblT_Card_Deposit x)
        {
            decimal total = Convert.ToDecimal(x.Total);

            x.Status = StatusModel.Created;
            if (x.DateTransaction == null) x.DateTransaction = DateTime.Now;
            using (var ctx = new DBEntities())
            {
                ctx.tblT_Card_Deposit.Add(x);
                ctx.SaveChanges();
            }

            var debit = WalletService.Debit(x.UserID, total, WalletService.TypeCardTopup, x.ID);
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
                WalletService.Credit(x.UserID, total, 0m, 0d, WalletService.TypeCardTopupReversal, x.ID);
                SetDepositStatus(x.ID, StatusModel.Failed);
                return new SpendResult { Success = false, ProviderFailed = true, Status = StatusModel.Failed, Message = "Card provider rejected the top-up; balance refunded" };
            }
            SetDepositStatus(x.ID, StatusModel.PendingProvider);
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
