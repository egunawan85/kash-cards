using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using QryptoCard.INT.Callback.Model.PGCrypto;
using QryptoCard.INT.Callback.Model.WasabiCard;
using QryptoCard.INT.Callback.Service.Gateway.WasabiCard;
using QryptoCard.Sec;

namespace QryptoCard.INT.Callback.Service
{
    /// <summary>
    /// Reconciliation sweep for card spends stranded at 'pending provider' — i.e. the balance was
    /// debited but the WasabiCard call returned an ambiguous/timeout result, so the card outcome is
    /// unknown. For each stranded order older than a grace window, it asks WasabiCard what actually
    /// happened (by our order id) and resolves:
    ///   - provider confirms SUCCESS  -> finalize via the shared CardFinalizationService;
    ///   - provider confirms FAILURE  -> refund (restore balance) via the claim-gated, idempotent
    ///                                   ReverseForOrder — safe against a racing late webhook;
    ///   - NO RECORD / unreachable    -> never auto-act; alert + leave for the next sweep / manual review.
    ///
    /// Lives in the Callback tier because all its dependencies (WasabiCard gateway, WalletService,
    /// the cross-check evaluator, the finalizer) already exist here. A scheduled trigger (separate
    /// change) invokes ReconcilePendingProvider over a loopback endpoint.
    /// </summary>
    public static class ReconciliationService
    {
        // Orders younger than this are skipped, so a genuinely in-flight call / late webhook can land
        // before the sweep touches the order.
        private static readonly TimeSpan GraceWindow = TimeSpan.FromMinutes(15);

        public enum ReconcileOutcome { Success, Failure, Unavailable }

        public static int ReconcilePendingProvider()
        {
            int handled = 0;
            DateTime cutoff = DateTime.Now - GraceWindow;

            List<tblT_Card> opens;
            using (var db = new DBEntities())
                opens = db.tblT_Card.Where(p => p.Status == PGStatusModel.PendingProvider
                    && p.DateModified != null && p.DateModified < cutoff).ToList();
            foreach (var o in opens)
            {
                try
                {
                    var rec = WasabiCardService.getCreateOperation(o.ID);
                    ResolveOpen(o, Classify(rec), rec);
                    handled++;
                }
                catch (Exception ex)
                {
                    // One bad order must not abort the batch (the refund/finalize are each atomic).
                    System.Diagnostics.Trace.TraceError("Reconcile: open " + o.ID + " threw — skipped this pass: " + ex.GetType().FullName);
                }
            }

            List<tblT_Card_Deposit> tops;
            using (var db = new DBEntities())
                tops = db.tblT_Card_Deposit.Where(p => p.Status == PGStatusModel.PendingProvider
                    && p.DateTransaction != null && p.DateTransaction < cutoff).ToList();
            foreach (var t in tops)
            {
                try
                {
                    var rec = WasabiCardService.getDepositOperation(t.ID);
                    ResolveTopUp(t, Classify(rec));
                    handled++;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Trace.TraceError("Reconcile: topup " + t.ID + " threw — skipped this pass: " + ex.GetType().FullName);
                }
            }

            return handled;
        }

        // Classify a provider record by reusing the existing status evaluator: Confirmed means the
        // provider confirms a FAILURE state; Mismatch means it says the op SUCCEEDED; Unavailable means
        // no record / pending / unknown / unreachable (fail-closed -> never auto-act).
        public static ReconcileOutcome Classify(WCCardTransactionResponseModel.Record rec)
        {
            if (rec == null) return ReconcileOutcome.Unavailable;
            switch (WebhookCrossCheckEvaluator.EvaluateDepositRefund(rec.status))
            {
                case CrossCheckOutcome.Confirmed: return ReconcileOutcome.Failure;
                case CrossCheckOutcome.Mismatch: return ReconcileOutcome.Success;
                default: return ReconcileOutcome.Unavailable;
            }
        }

        // Act on a classified card-OPEN. Public for direct unit testing (the live gateway returns null
        // under test, so the job-level path only exercises Unavailable).
        public static void ResolveOpen(tblT_Card o, ReconcileOutcome outcome, WCCardTransactionResponseModel.Record rec)
        {
            if (outcome == ReconcileOutcome.Success)
            {
                var fo = CardFinalizationService.FinalizeOpenSuccess(o.ID, rec != null ? rec.cardNo : null);
                Audit(o.ID, "open", "success", fo.ToString());
            }
            else if (outcome == ReconcileOutcome.Failure)
            {
                string claim = "UPDATE dbo.tblT_Card SET Status = '" + PGStatusModel.Failed +
                    "' WHERE ID = @id AND Status IN ('" + PGStatusModel.PendingProvider + "', '" + PGStatusModel.InProgress + "')";
                var rev = WalletService.ReverseForOrder(o.UserID, Convert.ToDecimal(o.Total),
                    WalletService.TypeCardOpenReversal, o.ID, claim, new[] { new SqlParameter("@id", o.ID) });
                if (!rev.Success && rev.FailureReason != "claim_lost")
                    System.Diagnostics.Trace.TraceError("Reconcile: open " + o.ID + " refund could not apply (" + rev.FailureReason + ") — money-affecting.");
                Audit(o.ID, "open", "failure", rev.Success ? "refunded" : rev.FailureReason);
            }
            else
            {
                System.Diagnostics.Trace.TraceWarning("Reconcile: open " + o.ID + " unavailable — left pending-provider for manual review.");
                Audit(o.ID, "open", "unavailable", "left");
            }
        }

        // Act on a classified TOP-UP.
        public static void ResolveTopUp(tblT_Card_Deposit t, ReconcileOutcome outcome)
        {
            if (outcome == ReconcileOutcome.Success)
            {
                var fo = CardFinalizationService.FinalizeTopUpSuccess(t.ID);
                Audit(t.ID, "topup", "success", fo.ToString());
            }
            else if (outcome == ReconcileOutcome.Failure)
            {
                string claim = "UPDATE dbo.tblT_Card_Deposit SET Status = '" + PGStatusModel.Failed +
                    "' WHERE ID = @id AND Status IN ('" + PGStatusModel.PendingProvider + "', '" + PGStatusModel.InProgress + "')";
                var rev = WalletService.ReverseForOrder(t.UserID, Convert.ToDecimal(t.Total),
                    WalletService.TypeCardTopupReversal, t.ID, claim, new[] { new SqlParameter("@id", t.ID) });
                if (!rev.Success && rev.FailureReason != "claim_lost")
                    System.Diagnostics.Trace.TraceError("Reconcile: topup " + t.ID + " refund could not apply (" + rev.FailureReason + ") — money-affecting.");
                Audit(t.ID, "topup", "failure", rev.Success ? "refunded" : rev.FailureReason);
            }
            else
            {
                System.Diagnostics.Trace.TraceWarning("Reconcile: topup " + t.ID + " unavailable — left pending-provider for manual review.");
                Audit(t.ID, "topup", "unavailable", "left");
            }
        }

        // Best-effort decision audit (forensics); reuses the partner-webhook journal table.
        private static void Audit(string orderId, string kind, string outcome, string action)
        {
            try
            {
                using (var db = new DBEntities())
                {
                    db.tblH_Partner_Webhook.Add(new tblH_Partner_Webhook
                    {
                        Type = "Reconciliation",
                        Response = kind + " " + orderId + " outcome=" + outcome + " action=" + action,
                        ResponseDate = DateTime.Now
                    });
                    db.SaveChanges();
                }
            }
            catch { /* audit is best-effort; never let it abort the sweep */ }
        }
    }
}
