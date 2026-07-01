using System;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq;
using QryptoCard.Sec;

namespace QryptoCard.INT.Callback.Service
{
    /// <summary>
    /// Deposit-into-card settlement (Callback tier), INVOICE model: each intent has its own Runegate
    /// invoice (unique address + PartnerReferenceID = intentId). Runegate delivers ONE deposit webhook
    /// per settled on-chain payment (an invoice can be paid by MULTIPLE transactions — split payment),
    /// carrying that PartnerReferenceID and the payment's net amount.
    ///
    /// Settlement is amount-driven (NOT status-driven), exactly like the proven engine matcher:
    ///   - credit each genuine per-transaction net to the wallet ONCE (dedup on the payment's
    ///     TransactionID), and
    ///   - ACCUMULATE ReceivedTotal (+= net) atomically, advancing Pending→Funding when the accumulated
    ///     total covers ExpectedTotal.
    /// This handles split (each tx credited + accrued), exact, and overpay (surplus → available balance)
    /// correctly. Crediting only the covering event / overwriting ReceivedTotal (an earlier bug) LOST
    /// split-payment funds. NOTE: assumes the webhook's amount is PER-TRANSACTION net (same as the legacy
    /// static path); reconfirm on the live invoice spike.
    ///
    /// Gated by CardFundingStreamingEnabled (ships OFF). Best-effort, never throws into the credit path.
    /// </summary>
    public static class CardFundingSettlementService
    {
        // Keys come from the shared QryptoCard.Sec.CardFundingGate so the INT-tier copy can't drift.
        public const string SetEnabled = CardFundingGate.SettingEnabled;
        public const string EnvEnabled = CardFundingGate.EnvEnabled;

        public static bool Enabled()
        {
            string e = SecretsConfig.GetOptional(EnvEnabled, null);
            if (!string.IsNullOrWhiteSpace(e))
                return e.Trim() == "1" || string.Equals(e.Trim(), "true", StringComparison.OrdinalIgnoreCase);
            return ReadNum(SetEnabled, 0) >= 1;
        }

        /// <summary>
        /// If this deposit webhook is for one of our per-intent invoices (PartnerReferenceID matches an
        /// intent), settle it and return TRUE (handled) — the caller must NOT fall through to the legacy
        /// static-address path. Returns FALSE if it is not our invoice (caller handles it legacy-style).
        /// Credits the wallet ONCE on the covered/terminal event (dedup on the InvoiceID = txId).
        /// </summary>
        // Mirror the legacy PGCryptoCreditFloor: ignore dust below $1 (never credit / accrue on it), so a
        // malformed sub-$1 event can't credit pennies and wrongly nudge the accumulate toward covered.
        private const decimal CreditFloor = 1m;

        public static bool TrySettleInvoicePayment(DBEntities db, string partnerRef, string txId,
            decimal amount, string status, decimal commission, double commissionPct, string rawJson)
        {
            if (!Enabled()) return false;
            if (string.IsNullOrWhiteSpace(partnerRef)) return false;

            string userId; decimal expectedTotal;
            try
            {
                // PartnerReferenceID == IntentID (we set it that way at invoice creation).
                var rows = db.Database.SqlQuery<InvoiceIntentRow>(
                    "SELECT TOP 1 IntentID, UserID, ExpectedTotal FROM dbo.tblT_Card_Funding_Intent WHERE IntentID = @ref",
                    P("@ref", partnerRef)).ToList();
                if (rows.Count == 0) return false; // not one of our streaming invoices → legacy path
                userId = rows[0].UserID;
                expectedTotal = rows[0].ExpectedTotal;
            }
            catch (Exception ex)
            {
                // Couldn't determine — do NOT claim it (a transient DB error shouldn't hijack a legacy
                // deposit); the legacy path's unknown-address branch safely no-ops on the invoice address.
                Trace.TraceError("CardFundingSettlement.TrySettleInvoicePayment lookup failed for ref " + partnerRef + ": " + ex.GetType().FullName);
                return false;
            }

            // It IS our invoice. Credit each genuine PER-TRANSACTION net ONCE, then accumulate ReceivedTotal
            // and advance when it covers ExpectedTotal. Amount-driven (not status-driven) so split payments
            // are summed correctly; the volatile Status is never the authority. See the class doc.
            try
            {
                // Dust floor (mirror legacy): sub-$1 events are journaled, never credited/accrued.
                if (amount < CreditFloor)
                {
                    Trace.TraceError("CardFundingSettlement: sub-floor amount " + amount + " for intent " + partnerRef + " — journaled, not credited.");
                    return true;
                }

                WalletService.EnsureWallet(userId);
                // Dedup on the webhook's TransactionID (the Runegate payment-event id) → each on-chain
                // payment credits the wallet EXACTLY once, even on redelivery.
                var credit = WalletService.CreditDeposit(userId, amount, commission, commissionPct, txId, status, rawJson);
                if (credit.Success)
                {
                    // Genuine new credit → accumulate this net + advance-if-now-covered (atomic, idempotent).
                    AccumulateAndMaybeAdvance(partnerRef, amount, expectedTotal);
                }
                else if (credit.FailureReason != "duplicate_event")
                {
                    // Transient credit failure — do NOT accumulate/advance; the webhook journal drives replay.
                    Trace.TraceError("CardFundingSettlement: wallet credit failed (" + credit.FailureReason + ") for intent " + partnerRef + " — held for reconcile.");
                }
                // duplicate_event → this exact payment already credited+accrued on its first delivery: no-op.
            }
            catch (Exception ex)
            {
                // Never propagate into the credit path; the webhook journal enables replay/reconcile.
                Trace.TraceError("CardFundingSettlement.TrySettleInvoicePayment settle failed for intent " + partnerRef + ": " + ex.GetType().FullName);
            }
            return true; // it is our invoice — handled (or journaled for reconcile)
        }

        // Accumulate the credited net onto ReceivedTotal and advance Pending→Funding once it covers
        // ExpectedTotal — a SINGLE atomic UPDATE using the pre-update RHS, state-gated on Pending so a
        // late/dup event or an already-advanced intent is a no-op. Called ONLY on a genuine new credit.
        private static void AccumulateAndMaybeAdvance(string intentId, decimal net, decimal expectedTotal)
        {
            using (var db = new DBEntities())
            {
                db.Database.ExecuteSqlCommand(
                    "UPDATE dbo.tblT_Card_Funding_Intent " +
                    "SET ReceivedTotal = ReceivedTotal + @net, " +
                    "    Status = CASE WHEN ReceivedTotal + @net >= @expected THEN 'Funding' ELSE Status END, " +
                    "    UpdatedDate = @now " +
                    "WHERE IntentID = @id AND Status = 'Pending'",
                    P("@net", net), P("@expected", expectedTotal), P("@now", DateTime.Now), P("@id", intentId));
            }
        }

        private class InvoiceIntentRow
        {
            public string IntentID { get; set; }
            public string UserID { get; set; }
            public decimal ExpectedTotal { get; set; }
        }

        private static SqlParameter P(string n, object v) { return new SqlParameter(n, v ?? DBNull.Value); }

        private static double ReadNum(string name, double def)
        {
            using (var db = new DBEntities())
            {
                var s = db.tblM_Setting.FirstOrDefault(p => p.Name == name);
                return (s != null && s.Value.HasValue) ? s.Value.Value : def;
            }
        }
    }
}
