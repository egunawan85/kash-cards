using System;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq;
using QryptoCard.Sec;

namespace QryptoCard.INT.Callback.Service
{
    /// <summary>
    /// Deposit-into-card settlement (Callback tier), PAYMENT-REQUEST model: each intent has its own
    /// Runegate payment request (unique dynamic address + PartnerReferenceID = intentId). Runegate
    /// delivers ONE deposit webhook per settled on-chain payment (an intent can be paid by MULTIPLE
    /// transactions — split payment), carrying that PartnerReferenceID, the NET it credited us, and the
    /// gateway commission it kept.
    ///
    /// Settlement is amount-driven (NOT status-driven), exactly like the proven engine matcher, and works
    /// in GROSS on-chain terms (net + commission = what the customer actually SENT):
    ///   - credit each genuine per-transaction GROSS to the wallet ONCE (dedup on the payment's
    ///     TransactionID), and
    ///   - ACCUMULATE ReceivedTotal (+= gross) atomically, advancing Pending→Funding when the accumulated
    ///     gross covers ExpectedTotal.
    /// Working in gross means the customer pays the clean ExpectedTotal sticker and Runegate's ~0.5%
    /// deposit commission is ABSORBED by our margin (our real float nets less) rather than charged on top;
    /// it also keeps issuance's ExpectedTotal debit balanced against the wallet credit.
    ///
    /// LEDGER NOTE: for intent-path deposits the wallet credit is the GROSS (Amount stores gross, not
    /// net) — unlike the legacy static path which credits net. So a reconciliation query must NOT compute
    /// "what the customer sent" as Amount + Commision for these rows (that double-counts the commission);
    /// the gross IS Amount. Gross-crediting is sound ONLY because the credited amount is drained by
    /// issuance's ExpectedTotal debit as the intent leaves Pending — hence credit + accumulate happen only
    /// while Pending (a deposit to an already-advanced/terminal intent is journaled, not credited). Any
    /// future withdraw/cash-out path must reconcile gross-vs-net before it can pay out available balance.
    ///
    /// SPLIT-PAYMENT NOTE (payment-request specific, verified against the Runegate source): Runegate fires
    /// a deposit webhook for EVERY on-chain partial payment (not once on completion), but every partial of
    /// the same payment request carries the SAME TransactionID (the request's id) — the per-chunk amount
    /// differs but the dedup key does not. So on a genuinely SPLIT payment: the FIRST chunk is credited
    /// (gross) to available balance and the intent stays Pending (< ExpectedTotal); every LATER chunk is a
    /// TransactionID duplicate → dropped (duplicate_event) → NOT credited. The intent stays stuck at the
    /// first-chunk amount. No funds are lost (Runegate holds the full amount in our merchant balance; the
    /// D1 reconcile view flags ReceivedTotal &lt; ExpectedTotal), but this needs an OPERATOR:
    ///   RECONCILE A STUCK SPLIT INTENT BY CREDITING ONLY THE SHORTFALL (ExpectedTotal − ReceivedTotal),
    ///   NEVER RE-CREDITING THE FULL ExpectedTotal — the first chunk is ALREADY in available balance.
    /// (The webhook payload carries no per-chunk unique id, so per-partial dedup isn't possible app-side.)
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
        /// If this deposit webhook is for one of our per-intent payment requests (PartnerReferenceID
        /// matches an intent), settle it and return TRUE (handled) — the caller must NOT fall through to
        /// the legacy static-address path. Returns FALSE if it is not ours (caller handles it legacy-style).
        /// Credits the wallet ONCE per webhook TransactionID (dedup), and only while the intent is Pending.
        /// </summary>
        // Mirror the legacy PGCryptoCreditFloor: ignore dust below $1 (never credit / accrue on it), so a
        // malformed sub-$1 event can't credit pennies and wrongly nudge the accumulate toward covered.
        private const decimal CreditFloor = 1m;

        public static bool TrySettleInvoicePayment(DBEntities db, string partnerRef, string txId,
            decimal amount, string status, decimal commission, double commissionPct, string rawJson)
        {
            if (!Enabled()) return false;
            if (string.IsNullOrWhiteSpace(partnerRef)) return false;

            string userId; decimal expectedTotal; string intentStatus;
            try
            {
                // PartnerReferenceID == IntentID (we set it that way at payment-request creation).
                var rows = db.Database.SqlQuery<InvoiceIntentRow>(
                    "SELECT TOP 1 IntentID, UserID, ExpectedTotal, Status FROM dbo.tblT_Card_Funding_Intent WHERE IntentID = @ref",
                    P("@ref", partnerRef)).ToList();
                if (rows.Count == 0) return false; // not one of our streaming intents → legacy path
                userId = rows[0].UserID;
                expectedTotal = rows[0].ExpectedTotal;
                intentStatus = rows[0].Status;
            }
            catch (Exception ex)
            {
                // Couldn't determine — do NOT claim it (a transient DB error shouldn't hijack a legacy
                // deposit); the legacy path's unknown-address branch safely no-ops on the dynamic address.
                Trace.TraceError("CardFundingSettlement.TrySettleInvoicePayment lookup failed for ref " + partnerRef + ": " + ex.GetType().FullName);
                return false;
            }

            // It IS our intent (so we must handle it — return true — and NOT fall through to the legacy
            // static-address path). Only credit + accumulate GROSS while the intent is still PENDING: the
            // gross credit is sound only because it is drained by issuance's ExpectedTotal debit, which
            // happens exactly once as the intent advances OUT of Pending. A deposit arriving after the
            // intent has already advanced/terminated (Funding/Issuing/Completed/Expired/Cancelled/Failed)
            // is unexpected — crediting gross then would leave ~0.5% unbacked in spendable balance with no
            // offsetting debit. So journal it for operator reconciliation instead (the funds are safe in
            // our Runegate merchant balance; the D1 reconcile view surfaces the intent). See the class doc.
            if (!string.Equals(intentStatus, "Pending", StringComparison.OrdinalIgnoreCase))
            {
                Trace.TraceError("CardFundingSettlement: deposit for intent " + partnerRef + " in non-Pending state '" +
                    intentStatus + "' — journaled for reconcile, not credited (avoids unbacked gross credit).");
                return true;
            }

            // It IS our intent. Credit each genuine PER-TRANSACTION GROSS ONCE, then accumulate ReceivedTotal
            // and advance when it covers ExpectedTotal. Amount-driven (not status-driven) so split payments
            // are summed correctly; the volatile Status is never the authority. Working in GROSS (net +
            // commission = what the customer sent) means the customer pays the clean ExpectedTotal and the
            // gateway commission is absorbed by our margin, and keeps the wallet credit aligned with
            // issuance's ExpectedTotal debit. See the class doc.
            decimal gross = CardFundingMath.GrossOnChain(amount, commission);
            try
            {
                // Dust floor (mirror legacy): sub-$1 (gross) events are journaled, never credited/accrued.
                if (gross < CreditFloor)
                {
                    Trace.TraceError("CardFundingSettlement: sub-floor gross " + gross + " for intent " + partnerRef + " — journaled, not credited.");
                    return true;
                }

                WalletService.EnsureWallet(userId);
                // Dedup on the webhook's TransactionID (the Runegate payment-event id) → each on-chain
                // payment credits the wallet EXACTLY once, even on redelivery. Credit the GROSS (commission
                // recorded separately for reconciliation); the ~0.5% is absorbed at the real float.
                var credit = WalletService.CreditDeposit(userId, gross, commission, commissionPct, txId, status, rawJson);
                if (credit.Success)
                {
                    // Genuine new credit → accumulate this gross + advance-if-now-covered (atomic, idempotent).
                    AccumulateAndMaybeAdvance(partnerRef, gross, expectedTotal);
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

        // Accumulate the credited GROSS onto ReceivedTotal and advance Pending→Funding once it covers
        // ExpectedTotal — a SINGLE atomic UPDATE using the pre-update RHS, state-gated on Pending so a
        // late/dup event or an already-advanced intent is a no-op. Called ONLY on a genuine new credit.
        // ReceivedTotal is in GROSS on-chain terms (what the customer sent), so it is directly comparable
        // to the ExpectedTotal sticker; the gateway commission is absorbed at the float, not here.
        private static void AccumulateAndMaybeAdvance(string intentId, decimal gross, decimal expectedTotal)
        {
            using (var db = new DBEntities())
            {
                db.Database.ExecuteSqlCommand(
                    "UPDATE dbo.tblT_Card_Funding_Intent " +
                    "SET ReceivedTotal = ReceivedTotal + @gross, " +
                    "    Status = CASE WHEN ReceivedTotal + @gross >= @expected THEN 'Funding' ELSE Status END, " +
                    "    UpdatedDate = @now " +
                    "WHERE IntentID = @id AND Status = 'Pending'",
                    P("@gross", gross), P("@expected", expectedTotal), P("@now", DateTime.Now), P("@id", intentId));
            }
        }

        private class InvoiceIntentRow
        {
            public string IntentID { get; set; }
            public string UserID { get; set; }
            public decimal ExpectedTotal { get; set; }
            public string Status { get; set; }
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
