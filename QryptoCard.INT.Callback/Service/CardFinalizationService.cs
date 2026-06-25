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

                cr.CardNo = providerCardNo;
                cr.Status = PGStatusModel.OpenCard;
                db.SaveChanges();

                var res = WasabiCardService.getCardInfo(new WCCardInfoRequestModel { cardNo = cr.CardNo, onlySimpleInfo = false });
                // The card balance comes from the provider's card-info response, never a webhook body;
                // on an unconfirmed/unreachable result the order stays OpenCard for a later retry.
                if (res == null || res.data == null ||
                    WebhookCrossCheckEvaluator.EvaluateCardOpen(cr.CardNo, res.data.cardNo) != CrossCheckOutcome.Confirmed)
                    return FinalizeOutcome.Unconfirmed;

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
                return FinalizeOutcome.Confirmed;
            }
        }
    }
}
