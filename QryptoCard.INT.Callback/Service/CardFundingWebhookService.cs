using System;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Globalization;
using System.Linq;

namespace QryptoCard.INT.Callback.Service
{
    /// <summary>
    /// Deposit-into-card EVENT-DRIVEN confirmation (Phase C) — called from the WasabiCard webhook
    /// handler to advance streaming intents from real WasabiCard events instead of polling. Two events:
    ///
    ///   OnCardConfirmed  (card_transaction create/deposit success): the card was actually opened/
    ///     topped-up → complete the matching streaming intent. Matched via the order's UserReferenceID
    ///     (= the funding IntentID for streaming orders; a legacy wallet-buy order carries a per-page
    ///     GUID that matches no intent → safe no-op). Idempotent with the synchronous issuance path and
    ///     the poll reconcile (state-gated: only transitions an OPEN intent).
    ///
    ///   OnFloatLanded  (wallet_transaction: our forward credited the merchant float): matched to OUR
    ///     forward by the on-chain tx hash; records the ACTUAL landed amount, and advances the intent
    ///     Confirming->Issuing ONLY when the landed amount covers the card's draw. Because each intent's
    ///     own money is proven in the pool, this advance carries NO single-Issuing constraint (that's
    ///     the de-serialization the poll fallback can't safely do). *** DOCS-BASED / UNVERIFIED payload
    ///     shape *** — must be validated by one live forward before enabling; gated by the switch.
    ///
    /// All gated by CardFundingStreamingEnabled; never throws into the webhook path; never touches a
    /// non-streaming order. Signature verification + journal-first happen upstream (controller + Wasabi).
    /// </summary>
    public static class CardFundingWebhookService
    {
        // ---- C3: a card event confirms the card exists -> complete the intent ----
        public static void OnCardConfirmed(string merchantOrderNo, string cardNo)
        {
            if (!CardFundingSettlementService.Enabled()) return;
            if (string.IsNullOrWhiteSpace(merchantOrderNo)) return;
            try
            {
                using (var db = new DBEntities())
                {
                    // Join the intent to its order via UserReferenceID = IntentID (set by the streaming
                    // issuance tick). Only ONE of the two order tables can hold this id (distinct prefixes),
                    // so the UNION returns 0/1 rows. Complete from any open money-moving state — the card is
                    // proven made. State-gated WHERE => idempotent against redelivery / the sync path.
                    int rows = db.Database.ExecuteSqlCommand(
                        "UPDATE i SET i.Status = 'Completed', i.OrderID = o.ID, " +
                        "  i.CardNo = ISNULL(NULLIF(@card,''), i.CardNo), i.UpdatedDate = @now " +
                        "FROM dbo.tblT_Card_Funding_Intent i " +
                        "JOIN (SELECT ID, UserReferenceID FROM dbo.tblT_Card WHERE ID = @oid " +
                        "      UNION ALL SELECT ID, UserReferenceID FROM dbo.tblT_Card_Deposit WHERE ID = @oid) o " +
                        "  ON i.IntentID = o.UserReferenceID " +
                        "WHERE i.Status IN ('Funding','Confirming','Issuing')",
                        P("@card", (object)cardNo), P("@now", DateTime.Now), P("@oid", merchantOrderNo));
                    if (rows > 0)
                        Trace.TraceInformation("CardFundingWebhook: completed streaming intent from card event for order " + merchantOrderNo);
                }
            }
            catch (Exception ex)
            {
                Trace.TraceError("CardFundingWebhook.OnCardConfirmed failed for order " + merchantOrderNo + ": " + ex.GetType().FullName);
            }
        }

        // ---- C2: Runegate transfer completed -> stamp the on-chain hash onto our forward ----
        // The synchronous transfer response gives only a TRANSFER id (stored as ProviderRef); the
        // on-chain tx hash arrives here, on Runegate's transfer-completion webhook. Capture it onto the
        // forward row (keyed by the transfer id) so OnFloatLanded can then correlate WasabiCard's
        // wallet_transaction (which reports that same on-chain hash) to this forward — the third leg of
        // the three-way match. DOCS-BASED / UNVERIFIED payload shape (transfer id + hash carrier are
        // confirmed against a live forward before this is wired to the endpoint); gated + best-effort.
        public static void OnForwardChainHash(string transferId, string chainTxHash)
        {
            if (!CardFundingSettlementService.Enabled()) return;
            if (string.IsNullOrWhiteSpace(transferId) || string.IsNullOrWhiteSpace(chainTxHash)) return;
            try
            {
                using (var db = new DBEntities())
                {
                    // Stamp the hash only if not already set (idempotent against redelivery). Only our
                    // intent forwards; a transfer id that matches no forward is a safe no-op.
                    db.Database.ExecuteSqlCommand(
                        "UPDATE dbo.tblH_WasabiCard_Refill SET ChainTxHash = @hash, UpdatedDate = @now " +
                        "WHERE RefillType = 'intent' AND ProviderRef = @tid AND ChainTxHash IS NULL",
                        P("@hash", chainTxHash), P("@now", DateTime.Now), P("@tid", transferId));
                }
            }
            catch (Exception ex)
            {
                Trace.TraceError("CardFundingWebhook.OnForwardChainHash failed for transfer " + transferId + ": " + ex.GetType().FullName);
            }
        }

        // ---- C2: our forward landed in the float -> confirm + advance to Issuing ----
        // DOCS-BASED / UNVERIFIED payload shape. Do NOT enable without validating the wallet_transaction
        // payload (esp. that txId == the hash Runegate returns) via one live forward.
        public static void OnFloatLanded(string txId, string fromAddress, string receivedAmountStr)
        {
            if (!CardFundingSettlementService.Enabled()) return;
            if (string.IsNullOrWhiteSpace(txId)) return;
            decimal landed;
            if (!decimal.TryParse(receivedAmountStr, NumberStyles.Any, CultureInfo.InvariantCulture, out landed)) return;

            try
            {
                using (var db = new DBEntities())
                {
                    // Match to OUR forward by the on-chain tx hash. Primary key is ChainTxHash (stamped by
                    // the transfer-completion webhook, OnForwardChainHash), which is exactly what WasabiCard
                    // reports here; ProviderRef is a fallback for gateways whose synchronous transfer id IS
                    // the chain hash. Only intent-type forwards.
                    var rows = db.Database.SqlQuery<ForwardRow>(
                        "SELECT TOP 1 PartnerReferenceID, DepositTxId AS IntentID, NetUsd " +
                        "FROM dbo.tblH_WasabiCard_Refill WHERE RefillType = 'intent' AND (ChainTxHash = @tx OR ProviderRef = @tx) " +
                        // Deterministic: prefer the authoritative ChainTxHash match over the ProviderRef
                        // fallback, then newest, so a (astronomically unlikely) hash/ref collision can't pick
                        // the wrong forward and advance the wrong intent.
                        "ORDER BY CASE WHEN ChainTxHash = @tx THEN 0 ELSE 1 END, ID DESC",
                        P("@tx", txId)).ToList();
                    if (rows.Count == 0)
                    {
                        // Fallback (fromAddress + amount + time) is left as a TODO until the payload is
                        // verified; a no-match is surfaced for reconciliation, never silently dropped.
                        Trace.TraceWarning("CardFundingWebhook: float landing tx " + txId + " matched no intent forward (fallback match pending live-spike verification).");
                        return;
                    }
                    var fw = rows[0];

                    // Record the ACTUAL landed amount + mark the forward Confirmed (idempotent).
                    db.Database.ExecuteSqlCommand(
                        "UPDATE dbo.tblH_WasabiCard_Refill SET Status = 'Confirmed', ActualReceivedUsd = @amt, " +
                        "  LandedTxId = @tx, LandedDate = @now, ConfirmedDate = ISNULL(ConfirmedDate, @now), UpdatedDate = @now " +
                        "WHERE PartnerReferenceID = @ref",
                        P("@amt", landed), P("@tx", txId), P("@now", DateTime.Now), P("@ref", fw.PartnerReferenceID));

                    if (WasabiCardFundingMath.LandedCoversDraw(landed, fw.NetUsd))
                    {
                        // De-serialized: this intent's OWN money is proven in the pool, so concurrent
                        // issuance can't over-draw — advance with NO single-Issuing constraint (unlike the
                        // conservative poll fallback). State-gated => idempotent.
                        db.Database.ExecuteSqlCommand(
                            "UPDATE dbo.tblT_Card_Funding_Intent SET Status = 'Issuing', UpdatedDate = @now " +
                            "WHERE IntentID = @id AND Status = 'Confirming'",
                            P("@now", DateTime.Now), P("@id", fw.IntentID));
                    }
                    else
                    {
                        // Underpaid float landing (bad gross-up, fee change, or a wrong manual send): hold
                        // in Confirming for operator reconciliation — NEVER issue against a short landing.
                        Trace.TraceError("CardFundingWebhook: UNDERPAID float landing for intent " + fw.IntentID +
                            " landed=" + landed + " needed=" + fw.NetUsd + " (tx " + txId + ") — held for reconcile.");
                    }
                }
            }
            catch (Exception ex)
            {
                Trace.TraceError("CardFundingWebhook.OnFloatLanded failed tx " + txId + ": " + ex.GetType().FullName);
            }
        }

        private class ForwardRow
        {
            public string PartnerReferenceID { get; set; }
            public string IntentID { get; set; }
            public decimal NetUsd { get; set; }
        }

        private static SqlParameter P(string n, object v) { return new SqlParameter(n, v ?? DBNull.Value); }
    }
}
