using System;
using System.Linq;
using QryptoCard.INT.Callback.Model.PGCrypto;
using QryptoCard.INT.Callback.Model.WasabiCard;
using QryptoCard.INT.Callback.Service.Gateway.WasabiCard;
using QryptoCard.Sec;

namespace QryptoCard.INT.Callback.Service
{
    /// <summary>
    /// Money-critical success finalization for card spends, shared by the WasabiCard webhook and the
    /// reconciliation sweep so the "provider confirmed it" path lives in exactly one place. It binds
    /// the card, cross-checks against the provider (defense-in-depth), activates + marks Success, and
    /// records the card balance. It deliberately does NOT fetch the sensitive PAN/CVV — that stays a
    /// webhook-only enrichment layered on top. Only ever acts on an order still InProgress/
    /// PendingProvider, so re-running (webhook + sweep racing) is a safe no-op once finalized.
    /// </summary>
    public static class CardFinalizationService
    {
        public enum FinalizeOutcome { Confirmed, Unconfirmed, NotFound }

        public static FinalizeOutcome FinalizeOpenSuccess(string orderId, string providerCardNo)
        {
            using (var db = new DBEntities())
            {
                var cr = db.tblT_Card.FirstOrDefault(p => p.ID == orderId &&
                    (p.Status == PGStatusModel.InProgress || p.Status == PGStatusModel.PendingProvider));
                if (cr == null) return FinalizeOutcome.NotFound;

                // Cross-check the provider FIRST (using the card number the provider reported), before
                // writing any state. On an unconfirmed/unreachable result the order is left at its
                // current status — PendingProvider stays sweep-retryable; we never write an intermediate
                // OpenCard state that neither the sweep nor the webhook would revisit (the stranding the
                // red-team caught). The card balance also comes from this response, never a webhook body.
                var res = WasabiCardService.getCardInfo(new WCCardInfoRequestModel { cardNo = providerCardNo, onlySimpleInfo = false });
                if (res == null || res.data == null ||
                    WebhookCrossCheckEvaluator.EvaluateCardOpen(providerCardNo, res.data.cardNo) != CrossCheckOutcome.Confirmed)
                    return FinalizeOutcome.Unconfirmed;

                // Confirmed — bind the card, activate, and mark Success together.
                cr.CardNo = providerCardNo;
                cr.isActive = 1;
                cr.Status = PGStatusModel.Success;
                db.SaveChanges();

                var ckb = db.tblT_Card_Balance.FirstOrDefault(p => p.CardNo == cr.CardNo);
                if (ckb != null)
                {
                    ckb.Amount = Convert.ToDouble(res.data.balanceInfo.amount);
                }
                else
                {
                    db.tblT_Card_Balance.Add(new tblT_Card_Balance
                    {
                        ID = cr.ID,
                        CardNo = cr.CardNo,
                        Currency = cr.Currency,
                        Amount = Convert.ToDouble(res.data.balanceInfo.amount)
                    });
                }
                db.SaveChanges();

                // Pay the referrer their commission on this confirmed buy (best-effort, idempotent,
                // never throws — must not roll back the finalization above).
                ReferralCommissionService.PayForFinalizedSpend(cr.UserID, cr.Fee, cr.ID);
                return FinalizeOutcome.Confirmed;
            }
        }

        public static FinalizeOutcome FinalizeTopUpSuccess(string orderId)
        {
            using (var db = new DBEntities())
            {
                var cr = db.tblT_Card_Deposit.FirstOrDefault(p => p.ID == orderId &&
                    (p.Status == PGStatusModel.InProgress || p.Status == PGStatusModel.PendingProvider));
                if (cr == null) return FinalizeOutcome.NotFound;

                cr.Status = PGStatusModel.Success;
                db.SaveChanges();

                // Pay the referrer their commission on this confirmed top-up (best-effort,
                // idempotent, never throws — must not roll back the finalization above).
                ReferralCommissionService.PayForFinalizedSpend(cr.UserID, cr.Fee, cr.ID);
                return FinalizeOutcome.Confirmed;
            }
        }
    }
}
