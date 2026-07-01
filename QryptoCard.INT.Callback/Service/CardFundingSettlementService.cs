using System;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq;
using QryptoCard.Sec;

namespace QryptoCard.INT.Callback.Service
{
    /// <summary>
    /// Deposit-into-card settlement (Callback tier), INVOICE model: each intent has its own Runegate
    /// invoice (unique address + PartnerReferenceID = intentId). The Runegate deposit webhook carries
    /// that PartnerReferenceID + a Status (PartiallyPaid / OverPaid / Completed), and Runegate tracks
    /// the invoice's cumulative paid state — so settlement REACTS to status rather than summing deposits:
    ///   - PartiallyPaid → record received-so-far (display), stay Pending, no wallet credit yet.
    ///   - Completed / OverPaid → credit the wallet ONCE (deduped on the InvoiceID) + advance Pending→Funding.
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
        public static bool TrySettleInvoicePayment(DBEntities db, string partnerRef, string txId,
            decimal amount, string status, decimal commission, double commissionPct, string rawJson)
        {
            if (!Enabled()) return false;
            if (string.IsNullOrWhiteSpace(partnerRef)) return false;

            string userId;
            try
            {
                // PartnerReferenceID == IntentID (we set it that way at invoice creation).
                var rows = db.Database.SqlQuery<InvoiceIntentRow>(
                    "SELECT TOP 1 IntentID, UserID FROM dbo.tblT_Card_Funding_Intent WHERE IntentID = @ref",
                    P("@ref", partnerRef)).ToList();
                if (rows.Count == 0) return false; // not one of our streaming invoices → legacy path
                userId = rows[0].UserID;
            }
            catch (Exception ex)
            {
                // Couldn't determine — do NOT claim it (a transient DB error shouldn't hijack a legacy
                // deposit); the legacy path's unknown-address branch safely no-ops on the invoice address.
                Trace.TraceError("CardFundingSettlement.TrySettleInvoicePayment lookup failed for ref " + partnerRef + ": " + ex.GetType().FullName);
                return false;
            }

            // ADVANCE ONLY WHEN THE WALLET IS ACTUALLY CREDITED. Issuance later debits the wallet by the
            // intent's ExpectedTotal, so advancing Pending->Funding without a matching credit would drive
            // the balance negative / fail issuance. So the advance is gated on a confirmed credit:
            //   - not covered (PartiallyPaid) -> record ReceivedTotal, stay Pending, no credit.
            //   - covered but a malformed amount (<= 0) -> do NOT advance; surface for reconcile.
            //   - covered + credited (fresh success OR already-credited duplicate) -> advance to Funding.
            //   - covered but the credit failed transiently -> stay Pending; the webhook journal drives
            //     reconcile/replay (we never falsely advance an un-credited intent).
            bool covered = InvoicePaymentStatus.IsCovered(status);
            try
            {
                if (!covered)
                {
                    Advance(partnerRef, amount, false); // partial: update ReceivedTotal only
                    return true;
                }
                if (amount <= 0m)
                {
                    Trace.TraceError("CardFundingSettlement: covered status '" + status + "' with non-positive amount " + amount + " for intent " + partnerRef + " — not advancing, held for reconcile.");
                    return true;
                }
                // Credit the user's wallet ONCE — dedup is on txId (= InvoiceID), so a re-delivered
                // Completed can't double-credit, and the earlier PartiallyPaid events (which don't credit)
                // don't consume the dedup slot.
                WalletService.EnsureWallet(userId);
                var credit = WalletService.CreditDeposit(userId, amount, commission, commissionPct, txId, status, rawJson);
                bool credited = credit.Success || credit.FailureReason == "duplicate_event";
                if (!credited)
                {
                    Trace.TraceError("CardFundingSettlement: wallet credit failed (" + credit.FailureReason + ") for intent " + partnerRef + " — staying Pending, held for reconcile.");
                    Advance(partnerRef, amount, false); // record received, do not advance
                    return true;
                }
                Advance(partnerRef, amount, true); // credited -> advance Pending->Funding
            }
            catch (Exception ex)
            {
                // Never propagate into the credit path; the webhook journal enables replay/reconcile.
                Trace.TraceError("CardFundingSettlement.TrySettleInvoicePayment settle failed for intent " + partnerRef + ": " + ex.GetType().FullName);
            }
            return true; // it is our invoice — handled (or journaled for reconcile)
        }

        // Advance the intent: always record ReceivedTotal (display), and move Pending->Funding only when
        // credited==true. State-gated on Pending so a re-delivered/late event (or an already-advanced
        // intent) is a no-op — idempotent.
        private static void Advance(string intentId, decimal receivedTotal, bool credited)
        {
            using (var db = new DBEntities())
            {
                db.Database.ExecuteSqlCommand(
                    "UPDATE dbo.tblT_Card_Funding_Intent " +
                    "SET ReceivedTotal = @recv, " +
                    "    Status = CASE WHEN @credited = 1 THEN 'Funding' ELSE Status END, " +
                    "    UpdatedDate = @now " +
                    "WHERE IntentID = @id AND Status = 'Pending'",
                    P("@recv", receivedTotal), P("@credited", credited ? 1 : 0), P("@now", DateTime.Now), P("@id", intentId));
            }
        }

        private class InvoiceIntentRow
        {
            public string IntentID { get; set; }
            public string UserID { get; set; }
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
