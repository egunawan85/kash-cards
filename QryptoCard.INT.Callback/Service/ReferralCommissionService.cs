using System;
using System.Linq;
using Newtonsoft.Json;

namespace QryptoCard.INT.Callback.Service
{
    /// <summary>
    /// Pays a referrer their commission when a user they referred completes a card buy or top-up.
    /// Invoked from the single confirmed-success finalize path (CardFinalizationService), so it only
    /// ever fires on a transaction that actually went through — a failed/abandoned spend never pays,
    /// which is also why there is nothing to claw back.
    ///
    /// The commission is a share of the PLATFORM FEE earned on the spend (referrer-rate * fee), NOT a
    /// share of the gross amount — so a payout can never exceed what the platform made. It is credited
    /// to the referrer's wallet balance (spendable on their own top-ups) via the verified WalletService
    /// credit path, and recorded in tblT_Commission for the dashboard "commission history".
    ///
    /// Best-effort and idempotent: it never throws (a referral hiccup must not roll back the
    /// money-critical card finalization) and de-dupes per referee order, so a redelivered webhook or
    /// the reconciliation sweep racing the webhook cannot double-pay.
    /// </summary>
    public static class ReferralCommissionService
    {
        // Fallback rate (as a fraction, e.g. 0.1 = 10%) if neither the referrer's per-user rate nor
        // the global default setting is present. Mirrors UserProvisioningService's seed default.
        const double DefaultCommissionRate = 0.1;

        /// <summary>
        /// Pay the referrer (if any) for a referee's just-finalized buy/top-up.
        /// <paramref name="orderId"/> is the referee's order id (tblT_Card / tblT_Card_Deposit ID) and
        /// is the idempotency key. <paramref name="fee"/> is the platform fee earned on that order.
        /// No-ops cleanly when there is no referrer, a self-referral, a missing/zero fee, or a replay.
        /// </summary>
        public static void PayForFinalizedSpend(string refereeUserId, double? fee, string orderId)
        {
            try
            {
                if (string.IsNullOrEmpty(refereeUserId) || string.IsNullOrEmpty(orderId)) return;
                if (!fee.HasValue || fee.Value <= 0d) return;

                using (var db = new DBEntities())
                {
                    // The referrer is the referee's InvitedBy (the referrer's UserID, bound at
                    // registration). No referrer => nobody to pay.
                    var referrer = db.tblM_User
                        .Where(p => p.UserID == refereeUserId)
                        .Select(p => p.InvitedBy)
                        .FirstOrDefault();
                    if (string.IsNullOrEmpty(referrer)) return;
                    if (referrer == refereeUserId) return; // self-referral guard

                    double rate = GetCommissionRate(db, referrer);
                    decimal feeDec = (decimal)fee.Value;
                    decimal commission = Math.Round((decimal)rate * feeDec, 2);

                    // Hard safety rail: never pay a referrer more than the fee the platform actually
                    // earned on this transaction, no matter how the rate is configured. Guarantees the
                    // feature can never run at a loss on any single payout.
                    if (commission > feeDec) commission = feeDec;
                    if (commission <= 0m) return;

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
                    // "duplicate_event" (this order already paid out) is a safe no-op, and so is any
                    // other non-success — the wallet balance is the source of truth either way.
                    if (res.Success)
                        WriteCommissionLedger(db, referrer, orderId, commission);
                }
            }
            catch
            {
                // Swallow: a referral-commission failure must never break or roll back the card
                // finalization (the money-critical path). The wallet credit, when it happened, is
                // durably recorded in the balance ledger regardless.
            }
        }

        // Referrer's rate (fraction): per-user rate first, then the global default setting, then a
        // constant. Read by raw SQL because tblM_User_Commission is not in the Callback EF model and
        // dragging it in would mean a model-regen for one scalar read.
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
