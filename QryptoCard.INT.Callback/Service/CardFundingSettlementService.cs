using System;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq;
using QryptoCard.Sec;

namespace QryptoCard.INT.Callback.Service
{
    /// <summary>
    /// Deposit-into-card settlement (Callback tier): when a crypto deposit is credited, apply it to
    /// the depositing user's OPEN funding intent and, once the intent is covered, advance it to
    /// Funding so the streaming forwarder can move the card's funds to WasabiCard.
    ///
    /// Only the match/advance step runs here (the webhook must return fast). The multi-minute
    /// forward -> confirm -> issue steps are driven by ticks (this tier forwards/confirms; the INT
    /// tier issues), communicating purely through the intent's Status column.
    ///
    /// Gated by CardFundingStreamingEnabled (ships OFF). Best-effort and never throws into the
    /// credit path — a settlement hiccup must not fail the wallet credit.
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
        /// Apply a genuine new deposit credit to the user's open Pending intent. Bumps ReceivedTotal
        /// and, when it reaches ExpectedTotal, flips Pending -> Funding in the SAME atomic UPDATE.
        /// Must be called ONLY on a genuine new credit (never a duplicate_event replay), so each net
        /// amount is applied exactly once. A deposit with no open Pending intent affects no rows and
        /// harmlessly remains as internal-wallet residual for the next purchase.
        /// </summary>
        public static void OnDepositCredited(string userId, decimal net, string depositTxId)
        {
            if (!Enabled()) return;
            if (string.IsNullOrWhiteSpace(userId) || net <= 0m) return;

            try
            {
                using (var db = new DBEntities())
                {
                    db.Database.ExecuteSqlCommand(
                        "UPDATE dbo.tblT_Card_Funding_Intent " +
                        "SET ReceivedTotal = ReceivedTotal + @net, " +
                        "    Status = CASE WHEN ReceivedTotal + @net >= ExpectedTotal THEN 'Funding' ELSE Status END, " +
                        "    UpdatedDate = @now " +
                        "WHERE UserID = @u AND Status = 'Pending'",
                        P("@net", net), P("@now", DateTime.Now), P("@u", userId));
                }
            }
            catch (Exception ex)
            {
                // Never propagate into the credit path; the deposit stays as wallet balance and the
                // next tick / retry can still settle it.
                Trace.TraceError("CardFundingSettlement.OnDepositCredited failed for user " + userId +
                    " tx " + depositTxId + ": " + ex.GetType().FullName);
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
}
