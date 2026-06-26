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

        // The per-order dedup unique index that makes the no-double-pay guarantee real.
        const string DedupIndexName = "UIX_tblH_Partner_Webhook_ID_ReferralCommission_TXID";

        // Caches only the POSITIVE result: once the index exists it never disappears, so we stop
        // checking. While absent we re-check every payout, so applying the index post-deploy takes
        // effect without a service recycle (and meanwhile payouts fail closed, never double).
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
                    if (double.IsNaN(rate) || double.IsInfinity(rate) || rate < 0d || rate > 1d)
                    {
                        // The cap keeps it loss-proof, but a rate outside [0,1] is almost certainly a
                        // percent-vs-fraction misconfiguration (e.g. 10 meaning "10%") that would
                        // otherwise silently pay out the entire fee. Surface it instead of hiding it.
                        System.Diagnostics.Trace.TraceWarning(
                            "ReferralCommission: rate " + rate + " for referrer " + referrer +
                            " is outside [0,1] — likely a percent-vs-fraction misconfiguration; payout is capped at the fee.");
                    }

                    decimal commission = QryptoCard.Sec.ReferralMath.Commission(rate, fee.Value);
                    if (commission <= 0m) return;

                    // Fail-closed on the dedup index: this per-order unique index is the ENTIRE
                    // replay-safety mechanism (the sibling PGCrypto index is filtered to its own Type).
                    // If it is missing — e.g. the additive deploy script has not been applied yet —
                    // refuse to pay rather than risk a double credit on a redelivered/raced finalize.
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
                    // "duplicate_event" (this order already paid out) is a safe no-op, and so is any
                    // other non-success — the wallet balance is the source of truth either way.
                    if (res.Success)
                    {
                        try
                        {
                            WriteCommissionLedger(db, referrer, orderId, commission);
                        }
                        catch (Exception ex)
                        {
                            // The credit already committed (balance ledger is the source of truth);
                            // the commission-history row is best-effort. Log so the gap — balance
                            // credited but no history row — is detectable and repairable.
                            System.Diagnostics.Trace.TraceError(
                                "ReferralCommission: balance credited but commission-history write failed for order " +
                                orderId + ": " + ex.Message);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Never rethrow: a referral-commission failure must not roll back the card
                // finalization (the money-critical path). Log so the failure is observable — the
                // wallet credit, when it happened, is durably recorded in the balance ledger.
                System.Diagnostics.Trace.TraceError(
                    "ReferralCommission payout error for order " + orderId + ": " + ex);
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
