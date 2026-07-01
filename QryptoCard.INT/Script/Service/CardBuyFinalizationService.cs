using System;
using System.Linq;
using Newtonsoft.Json;
using QryptoCard.INT.Model.Service;
using QryptoCard.INT.Model.WasabiCard;
using QryptoCard.INT.Script.Gateway.WasabiCard;
using QryptoCard.Sec;

namespace QryptoCard.INT.Script.Service
{
    /// <summary>
    /// INT-tier (buy-path) twin of the callback's CardFinalizationService: finalizes a confirmed card
    /// OPEN synchronously, right after the provider returns success — so a purchase completes (card
    /// bound, activated, balance recorded, referral commission paid) WITHOUT depending SOLELY on the
    /// inbound WasabiCard webhook (which IS delivered — confirmed in prod 2026-07-01 — but must not be a
    /// single point of failure). It cross-checks the provider
    /// (getCardInfo) before writing, only acts on an order still InProgress/PendingProvider, and is
    /// idempotent — so if the callback webhook OR the reconciliation sweep later runs the same finalize,
    /// it is a safe no-op (the order is already Success). The per-order commission dedup index makes the
    /// no-double-pay guarantee hold even across the buy path and the callback both firing.
    ///
    /// Deliberately does NOT fetch the sensitive PAN/CVV — that stays a webhook-only enrichment, exactly
    /// as in the callback twin. Logic here mirrors CardFinalizationService.FinalizeOpenSuccess +
    /// ReferralCommissionService.PayForFinalizedSpend, re-pointed at INT's DBEntities / services.
    /// </summary>
    public static class CardBuyFinalizationService
    {
        public enum FinalizeOutcome { Confirmed, Unconfirmed, NotFound }

        // Fallback referral rate (fraction) when neither the per-user rate nor the global setting is set.
        const double DefaultCommissionRate = 0.1;
        // The per-order dedup unique index that makes the no-double-pay guarantee real.
        const string DedupIndexName = "UIX_tblH_Partner_Webhook_ID_ReferralCommission_TXID";
        // Cache only the POSITIVE result (an existing index never disappears); while absent, re-check
        // each payout so applying it post-deploy takes effect without a recycle (payouts fail closed
        // meanwhile).
        static volatile bool _dedupIndexVerified;

        /// <summary>
        /// Finalize a confirmed open: cross-check the provider card, bind it, activate, mark Success,
        /// record balance, and pay the referrer their commission. Returns NotFound if the order isn't
        /// awaiting finalization (already done / unknown), Unconfirmed if the provider doesn't confirm
        /// the card (nothing written — left for the sweep/webhook to retry).
        /// </summary>
        public static FinalizeOutcome FinalizeOpenSuccess(string orderId, string providerCardNo)
        {
            using (var db = new DBEntities())
            {
                var cr = db.tblT_Card.FirstOrDefault(p => p.ID == orderId &&
                    (p.Status == StatusModel.InProgress || p.Status == StatusModel.PendingProvider));
                if (cr == null) return FinalizeOutcome.NotFound;

                // Cross-check the provider FIRST, before writing any state. On an unconfirmed/unreachable
                // result, write NOTHING and leave the order at its current status so the sweep/webhook
                // can retry. The card balance also comes from this response, never a webhook body.
                var res = WasabiCardService.getCardInfo(new WCCardInfoRequestModel { cardNo = providerCardNo, onlySimpleInfo = false });
                if (res == null || res.data == null ||
                    WebhookCrossCheckEvaluator.EvaluateCardOpen(providerCardNo, res.data.cardNo) != CrossCheckOutcome.Confirmed)
                    return FinalizeOutcome.Unconfirmed;

                // Confirmed — bind the card, activate, and mark Success together.
                cr.CardNo = providerCardNo;
                cr.isActive = 1;
                cr.Status = StatusModel.Success;
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
                PayForFinalizedSpend(cr.UserID, cr.Fee, cr.ID);
                return FinalizeOutcome.Confirmed;
            }
        }

        /// <summary>
        /// Finalize a confirmed TOP-UP synchronously: mark the deposit Success and pay the referrer
        /// their commission — so a top-up completes at purchase time instead of stranding InProgress
        /// while waiting on the inbound WasabiCard webhook (which IS delivered, but is not a single
        /// point of failure). The card
        /// already exists, so (unlike the open finalize) there is no cardNo to bind or cross-check —
        /// the depositCard 200 is the provider's confirmation. Idempotent (only acts on an
        /// InProgress/PendingProvider deposit), so a later webhook/sweep finalize is a safe no-op and
        /// the per-order commission dedup index prevents any double-pay across both paths.
        /// </summary>
        public static FinalizeOutcome FinalizeTopUpSuccess(string orderId)
        {
            using (var db = new DBEntities())
            {
                var cr = db.tblT_Card_Deposit.FirstOrDefault(p => p.ID == orderId &&
                    (p.Status == StatusModel.InProgress || p.Status == StatusModel.PendingProvider));
                if (cr == null) return FinalizeOutcome.NotFound;

                cr.Status = StatusModel.Success;
                db.SaveChanges();

                // Pay the referrer their commission on this confirmed top-up (best-effort, idempotent,
                // never throws — must not roll back the finalization above).
                PayForFinalizedSpend(cr.UserID, cr.Fee, cr.ID);
                return FinalizeOutcome.Confirmed;
            }
        }

        // ---- referral commission (ported from the callback's ReferralCommissionService) -------------

        /// <summary>
        /// Pay the referrer (if any) for a referee's just-finalized buy. <paramref name="orderId"/> is
        /// the referee's order id and is the idempotency key; <paramref name="fee"/> is the platform fee
        /// earned on that order. No-ops cleanly on no referrer / self-referral / missing-zero fee / replay.
        /// Never throws — a referral hiccup must not roll back the money-critical finalization.
        /// </summary>
        static void PayForFinalizedSpend(string refereeUserId, double? fee, string orderId)
        {
            try
            {
                if (string.IsNullOrEmpty(refereeUserId) || string.IsNullOrEmpty(orderId)) return;
                if (!fee.HasValue || fee.Value <= 0d) return;

                using (var db = new DBEntities())
                {
                    // The referrer is the referee's InvitedBy (bound at registration). No referrer => nobody to pay.
                    var referrer = db.tblM_User
                        .Where(p => p.UserID == refereeUserId)
                        .Select(p => p.InvitedBy)
                        .FirstOrDefault();
                    if (string.IsNullOrEmpty(referrer)) return;
                    if (referrer == refereeUserId) return; // self-referral guard

                    double rate = GetCommissionRate(db, referrer);
                    if (double.IsNaN(rate) || double.IsInfinity(rate) || rate < 0d || rate > 1d)
                    {
                        // The cap keeps it loss-proof, but a rate outside [0,1] is almost certainly a
                        // percent-vs-fraction misconfiguration; surface it instead of hiding it.
                        System.Diagnostics.Trace.TraceWarning(
                            "ReferralCommission: rate " + rate + " for referrer " + referrer +
                            " is outside [0,1] — likely a percent-vs-fraction misconfiguration; payout is capped at the fee.");
                    }

                    decimal commission = QryptoCard.Sec.ReferralMath.Commission(rate, fee.Value);
                    if (commission <= 0m) return;

                    // Fail-closed on the dedup index: this per-order unique index is the ENTIRE
                    // replay-safety mechanism. If it is missing, refuse to pay rather than risk a double
                    // credit on a redelivered/raced finalize (buy path + callback + sweep can all run).
                    if (!DedupIndexPresent(db))
                    {
                        System.Diagnostics.Trace.TraceError(
                            "ReferralCommission: dedup index " + DedupIndexName + " is missing — skipping payout for order " +
                            orderId + ". Apply deploy/sql/create-referral-commission-dedup-index.sql before enabling payouts.");
                        return;
                    }

                    WalletService.EnsureWallet(referrer);
                    var res = WalletService.CreditReferralCommission(
                        referrer, commission, orderId,
                        JsonConvert.SerializeObject(new
                        {
                            source = "referral_commission",
                            refereeUserId,
                            orderId,
                            fee = fee.Value,
                            rate
                        }));

                    // Only write the earnings/display row on a fresh successful credit. A
                    // "duplicate_event" (already paid out) is a safe no-op, as is any other non-success —
                    // the wallet balance is the source of truth either way.
                    if (res.Success)
                    {
                        try
                        {
                            WriteCommissionLedger(db, referrer, orderId, commission);
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Trace.TraceError(
                                "ReferralCommission: balance credited but commission-history write failed for order " +
                                orderId + ": " + ex.Message);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Never rethrow: a referral-commission failure must not roll back the card finalization.
                System.Diagnostics.Trace.TraceError(
                    "ReferralCommission payout error for order " + orderId + ": " + ex);
            }
        }

        static bool DedupIndexPresent(DBEntities db)
        {
            if (_dedupIndexVerified) return true;
            var n = db.Database.SqlQuery<int>(
                "SELECT COUNT(*) FROM sys.indexes WHERE name = @p0 AND object_id = OBJECT_ID('dbo.tblH_Partner_Webhook_ID')",
                DedupIndexName).FirstOrDefault();
            if (n > 0) { _dedupIndexVerified = true; return true; }
            return false;
        }

        // Referrer's rate (fraction): per-user rate first, then the global default setting, then a constant.
        static double GetCommissionRate(DBEntities db, string referrerUserId)
        {
            var perUser = db.Database.SqlQuery<double?>(
                "SELECT TOP 1 Commission FROM dbo.tblM_User_Commission WHERE UserID = @p0",
                referrerUserId).FirstOrDefault();
            if (perUser.HasValue) return perUser.Value;

            var setting = db.Database.SqlQuery<double?>(
                "SELECT TOP 1 [Value] FROM dbo.tblM_Setting WHERE ID = 2").FirstOrDefault();
            return setting ?? DefaultCommissionRate;
        }

        static void WriteCommissionLedger(DBEntities db, string referrerUserId, string orderId, decimal commission)
        {
            db.tblT_Commission.Add(new tblT_Commission
            {
                CommisionID = Guid.NewGuid().ToString(),
                UserID = referrerUserId,
                TransactionID = orderId,
                Commission = (double)commission,
                DateCreated = DateTime.Now,
                CreatedBy = "system"
            });
            db.SaveChanges();
        }
    }
}
