using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq;

namespace QryptoCard.INT.Callback.Service
{
    /// <summary>
    /// Deposit-into-card streaming pump (Callback tier), driven by a scheduled tick:
    ///   1. FORWARD  — a covered intent (Status=Funding) has exactly its card's float draw forwarded
    ///                 to WasabiCard via the hardened transfer primitive, then advances to Confirming.
    ///   2. CONFIRM  — when the WasabiCard float reflects the forward (float >= the card's draw), the
    ///                 intent advances to Issuing so the INT-tier issuance tick can open/top-up the card.
    ///
    /// Issuance is SERIALIZED (at most one intent in Issuing at a time) so concurrent draws can never
    /// collectively exceed the float. Sizing is data-driven (new-card open = face + $1 create at 0%,
    /// per prod), never a blanket gross-up, so nothing is over-forwarded into the one-way float.
    /// Gated by CardFundingStreamingEnabled.
    /// </summary>
    public static class CardFundingForwardService
    {
        // Settings for float-draw sizing (mirror the migration seeds; env/DB/default precedence).
        public const string SetCreateCostUsd = "WasabiCardCreateCostUsd";       // flat $ drawn on a new-card open
        public const string SetOpenFeePct = "WasabiCardWcFeeRatePctOpen";        // per-card deposit fee % (open = 0)
        private const double DefCreateCostUsd = 1d;
        private const double DefOpenFeePct = 0d;

        private const int Batch = 20;

        public static string RunTick()
        {
            if (!CardFundingSettlementService.Enabled()) return "{\"skipped\":\"disabled\"}";

            int forwarded = 0, confirmed = 0;
            try
            {
                // 1) FORWARD: Funding -> Confirming (one forward per intent, ref = INTENT-<id>).
                // FORWARD-FIRST: the ledger's unique PartnerReferenceID makes the money movement
                // idempotent, so concurrent ticks / crash-retries can't double-send; we only advance
                // the intent AFTER a confirmed submit, so a crash mid-forward recovers cleanly (the
                // next tick re-reads the same ledger row's status) with no stuck Confirming state.
                foreach (var it in LoadByStatus(CardFundingIntentStatuses.Funding, Batch))
                {
                    decimal draw = CardDraw(it);
                    string partnerRef = "INTENT-" + it.IntentID;

                    string st = WasabiCardFundingService.ForwardForIntent(partnerRef, it.IntentID, draw);
                    if (st == "Submitted" || st == "Unknown")
                    {
                        // Submitted: awaiting float credit. Unknown: funds MAY have moved — never retry;
                        // the confirm step handles it. Advance to Confirming (loser of a race no-ops).
                        if (ClaimStatus(it.IntentID, CardFundingIntentStatuses.Funding, CardFundingIntentStatuses.Confirming, partnerRef))
                            forwarded++;
                    }
                    else if (st == "Failed")
                    {
                        // Definitive reject — money did NOT move. Fail the intent; the deposit stays as
                        // internal-wallet residual and the user can start a new intent.
                        ClaimStatus(it.IntentID, CardFundingIntentStatuses.Funding, CardFundingIntentStatuses.Failed, null);
                    }
                    // else pre-reserve skip (address/cap/resolve/disabled) created NO ledger row and left
                    // the intent Funding -> retry on a later tick when the condition clears.
                }

                // 2) CONFIRM: Confirming -> Issuing, SERIALIZED (only when nothing is already Issuing),
                //    and only when the float actually covers this card's draw.
                if (CountByStatus(CardFundingIntentStatuses.Issuing) == 0)
                {
                    decimal? floatUsd = WasabiCardFundingService.ReadFloatUsd();
                    if (floatUsd.HasValue)
                    {
                        foreach (var it in LoadByStatus(CardFundingIntentStatuses.Confirming, Batch))
                        {
                            if (floatUsd.Value < CardDraw(it)) continue;
                            if (ClaimStatus(it.IntentID, CardFundingIntentStatuses.Confirming, CardFundingIntentStatuses.Issuing, null))
                            {
                                if (!string.IsNullOrEmpty(it.ForwardRef))
                                    WasabiCardFundingService.MarkForwardConfirmed(it.ForwardRef);
                                confirmed++;
                                break; // one at a time
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Trace.TraceError("CardFundingForwardService.RunTick failed: " + ex.GetType().FullName);
            }

            return "{\"forwarded\":" + forwarded + ",\"confirmed\":" + confirmed + "}";
        }

        // ---- float-draw sizing ----------------------------------------------

        private static decimal CardDraw(IntentRow it)
        {
            bool isNew = string.Equals(it.Kind, "new", StringComparison.OrdinalIgnoreCase);
            decimal createCost = (decimal)ReadNum(SetCreateCostUsd, DefCreateCostUsd);
            double cardFeePct = ReadNum(SetOpenFeePct, DefOpenFeePct);
            return WasabiCardFundingMath.CardDrawUsd(isNew, it.Face, createCost, cardFeePct);
        }

        // ---- intent row access (raw SQL) ------------------------------------

        private class IntentRow
        {
            public string IntentID { get; set; }
            public string Kind { get; set; }
            public decimal Face { get; set; }
            public string ForwardRef { get; set; }
        }

        private static List<IntentRow> LoadByStatus(string status, int top)
        {
            using (var db = new DBEntities())
            {
                return db.Database.SqlQuery<IntentRow>(
                    "SELECT TOP (" + top + ") IntentID, Kind, Face, ForwardRef " +
                    "FROM dbo.tblT_Card_Funding_Intent WHERE Status = @st ORDER BY ID ASC",
                    P("@st", status)).ToList();
            }
        }

        private static bool ClaimStatus(string intentId, string from, string to, string forwardRef)
        {
            using (var db = new DBEntities())
            {
                int rows = db.Database.ExecuteSqlCommand(
                    "UPDATE dbo.tblT_Card_Funding_Intent " +
                    "SET Status = @to, UpdatedDate = @now" + (forwardRef != null ? ", ForwardRef = @ref " : " ") +
                    "WHERE IntentID = @id AND Status = @from",
                    forwardRef != null
                        ? new[] { P("@to", to), P("@now", DateTime.Now), P("@ref", forwardRef), P("@id", intentId), P("@from", from) }
                        : new[] { P("@to", to), P("@now", DateTime.Now), P("@id", intentId), P("@from", from) });
                return rows == 1;
            }
        }

        private static int CountByStatus(string status)
        {
            using (var db = new DBEntities())
            {
                var rows = db.Database.SqlQuery<int>(
                    "SELECT COUNT(*) FROM dbo.tblT_Card_Funding_Intent WHERE Status = @st",
                    P("@st", status)).ToList();
                return rows.Count > 0 ? rows[0] : 0;
            }
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

    /// <summary>Shared intent status literals (Callback tier mirror of the INT-tier constants).</summary>
    internal static class CardFundingIntentStatuses
    {
        public const string Pending = "Pending";
        public const string Funding = "Funding";
        public const string Confirming = "Confirming";
        public const string Issuing = "Issuing";
        public const string Completed = "Completed";
        public const string Expired = "Expired";
        public const string Cancelled = "Cancelled";
        public const string Failed = "Failed";
    }
}
