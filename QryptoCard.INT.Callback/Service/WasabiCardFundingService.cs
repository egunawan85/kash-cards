using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using System.Linq;
using Newtonsoft.Json;
using QryptoCard.INT.Callback.Model.PGCrypto;
using QryptoCard.INT.Callback.Model.WasabiCard;
using QryptoCard.INT.Callback.Service.Gateway.PGCrypto;
using QryptoCard.INT.Callback.Service.Gateway.WasabiCard;

namespace QryptoCard.INT.Callback.Service
{
    /// <summary>
    /// WasabiCard auto-funding engine (the money-OUT mover). Two tiers:
    ///   Tier 1 (floor refill, on the scheduled monitor tick): when the WasabiCard USD float drops
    ///           below WasabiCardFloorUsd, transfer enough USDT via Runegate to top it back up to
    ///           WasabiCardTargetUsd.
    ///   Tier 2 (eager pass-through, on the deposit-credit hook): when a crypto deposit larger than
    ///           WasabiCardEagerThresholdUsd is credited, immediately forward (deposit - platform fee)
    ///           to WasabiCard, pre-positioning the spendable amount ahead of the likely card load.
    ///
    /// SAFETY PROPERTIES:
    ///  - Master kill-switch (WasabiCardAutoFundEnabled) ships OFF; NOTHING moves money until set.
    ///  - Every outbound transfer is recorded in tblH_WasabiCard_Refill FIRST, keyed by a unique
    ///    PartnerReferenceID, so a duplicate attempt (redelivered deposit, racing tick) is a no-op.
    ///  - In-flight accounting: floor-refill sizing and the in-flight guard count money already on
    ///    the way (Initiated/Submitted/Unknown) so we never double-fund the floor.
    ///  - Daily cap (WasabiCardDailyCapUsd): a hard circuit breaker — once 24h transfers reach it,
    ///    further transfers are refused and an alert is raised (WasabiCard funds are hard to claw back).
    ///  - Ambiguous transfer outcomes (timeout/5xx) are recorded 'Unknown' and left in-flight, NEVER
    ///    auto-retried — a retry could double-send money that already moved. Reconcile alerts for
    ///    manual review during the guarded rollout.
    /// All thresholds live in tblM_Setting so they tune without a redeploy.
    /// </summary>
    public static class WasabiCardFundingService
    {
        // ---- setting keys ----
        public const string SetEnabled = "WasabiCardAutoFundEnabled";       // 0/1 kill-switch (default 0 = OFF)
        public const string SetFloorUsd = "WasabiCardFloorUsd";             // refill trigger + low-balance alert (500)
        public const string SetTargetUsd = "WasabiCardTargetUsd";           // refill target (700)
        public const string SetEagerThresholdUsd = "WasabiCardEagerThresholdUsd"; // eager-forward trigger (500)
        public const string SetDailyCapUsd = "WasabiCardDailyCapUsd";       // 24h circuit breaker (30000)
        public const string SetMinTransferUsd = "WasabiCardMinTransferUsd"; // skip uneconomic tiny transfers (50)
        public const string SetWcFeeRatePct = "WasabiCardWcFeeRatePct";     // WasabiCard deposit fee % for gross-up (1.4)
        public const string SetReAlertHours = "WasabiCardReAlertHours";     // re-alert cadence while still breached (6)
        public const string SetInFlightStaleMinutes = "WasabiCardInFlightStaleMinutes"; // Submitted counts as in-flight only this long (60)
        public const string SetDepositAddress = "WasabiCardDepositAddress"; // Param1 = USDT-TRC20 deposit address
        public const string SetTronCoinId = "WasabiCardTronCoinId";         // Param1 override (else resolved live)
        public const string SetUsdtTokenId = "WasabiCardUsdtTokenId";       // Param1 override (else resolved live)
        public const string SetAlertState = "WasabiCardAlertState";         // Param1=last state, Param2=last alert ISO
        public const string SetAlertEmail = "WasabiCardAlertEmail";         // Param1 = ops alert recipient (optional)

        // The platform deposit fee % is the SAME global setting the card-pricing overlay uses, so the
        // amount forwarded equals the customer's spendable (deposit minus our margin). Default 3.
        public const string SetPlatformFeeRatePct = "CardDepositFeeRate";

        // ---- defaults (used when a setting row is absent) ----
        private const double DefFloor = 500, DefTarget = 700, DefEager = 500, DefDailyCap = 30000,
            DefMinTransfer = 50, DefWcFee = 1.4, DefReAlertHours = 6, DefPlatformFee = 3, DefStaleMinutes = 60;

        private const string StInitiated = "Initiated", StSubmitted = "Submitted",
            StConfirmed = "Confirmed", StFailed = "Failed", StUnknown = "Unknown";

        private static SqlParameter P(string n, object v) { return new SqlParameter(n, v ?? DBNull.Value); }

        // ================= settings reads =================
        private static double ReadNum(string name, double def)
        {
            using (var db = new DBEntities())
            {
                var s = db.tblM_Setting.FirstOrDefault(p => p.Name == name);
                return (s != null && s.Value.HasValue) ? s.Value.Value : def;
            }
        }
        private static string ReadParam1(string name)
        {
            using (var db = new DBEntities())
            {
                var s = db.tblM_Setting.FirstOrDefault(p => p.Name == name);
                return s != null ? s.Param1 : null;
            }
        }
        public static bool Enabled() { return ReadNum(SetEnabled, 0) >= 1; }

        // ================= public entrypoints =================

        /// <summary>
        /// Scheduled monitor tick (invoked over the loopback monitor endpoint). Reads the float,
        /// performs a floor refill if enabled and needed, then evaluates the low-balance / coverage
        /// alert. Returns a compact JSON summary. Never throws to the caller.
        /// </summary>
        public static string RunMonitorTick()
        {
            var summary = new Dictionary<string, object>();
            try
            {
                decimal? floatUsd = ReadFloatUsd();
                decimal inFlight = InFlightUsd();
                double floor = ReadNum(SetFloorUsd, DefFloor);
                double target = ReadNum(SetTargetUsd, DefTarget);

                summary["floatUsd"] = floatUsd.HasValue ? (object)floatUsd.Value : null;
                summary["inFlightUsd"] = inFlight;
                summary["enabled"] = Enabled();

                // Floor refill (Tier 1), only when enabled, the float read succeeded, and the
                // effective balance (pot + money on the way) is below the floor.
                if (Enabled() && floatUsd.HasValue)
                {
                    decimal effective = floatUsd.Value + inFlight;
                    if (effective < (decimal)floor && !HasInFlightFloor())
                    {
                        decimal net = (decimal)target - effective;
                        var res = AttemptTransfer(StFloorType, "kash-floor-" + DateTime.Now.ToString("yyyyMMddHHmmss"),
                            null, net);
                        summary["floorRefill"] = res;
                    }
                }

                // Liability + coverage (WC3, informational) and the low-balance alert (WC1).
                decimal liability = TotalLiabilityUsd();
                summary["liabilityUsd"] = liability;
                summary["coverageRatio"] = liability > 0 && floatUsd.HasValue
                    ? Math.Round(floatUsd.Value / liability, 4) : (object)null;

                EvaluateAlert(floatUsd, inFlight, (decimal)floor, liability, summary);
            }
            catch (Exception ex)
            {
                summary["error"] = ex.Message;
                System.Diagnostics.Trace.TraceError("WasabiCard monitor tick failed: " + ex);
            }
            return JsonConvert.SerializeObject(summary);
        }

        private const string StFloorType = "floor", StEagerType = "eager";

        /// <summary>
        /// Tier-2 eager pass-through. Called from the deposit-credit path AFTER a deposit has been
        /// credited to the user's wallet. Forwards (deposit - platform fee) to WasabiCard, grossed up
        /// for WasabiCard's deposit fee so the spendable amount lands. Best-effort and fully guarded;
        /// never throws into the credit path (the credit is already committed).
        /// </summary>
        public static void OnDepositCredited(decimal depositAmount, string depositTxId)
        {
            try
            {
                if (!Enabled()) return;
                if (string.IsNullOrEmpty(depositTxId)) return;
                double eager = ReadNum(SetEagerThresholdUsd, DefEager);
                if (depositAmount <= (decimal)eager) return; // small deposits feed the wallet; floor covers them

                double platformFeePct = ReadNum(SetPlatformFeeRatePct, DefPlatformFee);
                decimal net = WasabiCardFundingMath.SpendableNet(depositAmount, platformFeePct); // spendable / card-face portion
                AttemptTransfer(StEagerType, "kash-eager-" + depositTxId, depositTxId, net);
            }
            catch (Exception ex)
            {
                // The deposit credit is already durable; an eager-forward failure must not surface.
                System.Diagnostics.Trace.TraceError("WasabiCard eager forward failed for tx " + depositTxId + ": " + ex);
            }
        }

        // ================= core transfer primitive =================

        /// <summary>
        /// Idempotent, capped, fail-safe outbound transfer. Inserts the ledger row first (unique
        /// PartnerReferenceID — duplicate => no-op), enforces the daily cap, resolves coin/token and
        /// destination, grosses up for the WasabiCard fee, submits via Runegate, and records the
        /// trichotomy outcome. Returns a small status object for the tick summary / logs.
        /// </summary>
        private static object AttemptTransfer(string type, string partnerRef, string depositTxId, decimal netUsd)
        {
            // Defense-in-depth: both callers already gate on Enabled(); re-check here so the
            // kill-switch holds for any future caller too.
            if (!Enabled()) return new { partnerRef, skipped = "disabled" };

            netUsd = Math.Round(netUsd, 4);
            double minTransfer = ReadNum(SetMinTransferUsd, DefMinTransfer);
            if (netUsd < (decimal)minTransfer)
                return new { partnerRef, skipped = "below_min_transfer", netUsd };

            // (Daily cap is enforced ATOMICALLY in Reserve() below, together with the floor
            // single-in-flight rule and idempotency — a check here would be a TOCTOU race.)

            // Resolve TRON CoinID + USDT-TRC20 TokenID (settings override, else live master-data).
            string coinId, tokenId;
            if (!ResolveTronUsdt(out coinId, out tokenId))
            {
                Alert("WasabiCard auto-fund: cannot resolve TRON/USDT ids",
                    "Skipped a " + type + " transfer of $" + netUsd + " — could not resolve the TRON CoinID / USDT-TRC20 TokenID from Runegate master data.");
                return new { partnerRef, skipped = "resolve_failed", netUsd };
            }

            string address = (ReadParam1(SetDepositAddress) ?? "").Trim();
            if (!LooksLikeTronAddress(address))
            {
                Alert("WasabiCard auto-fund: deposit address missing/invalid",
                    "Skipped a " + type + " transfer of $" + netUsd + " — WasabiCardDepositAddress is not a valid TRON address.");
                return new { partnerRef, skipped = "bad_address", netUsd };
            }

            double wcFeePct = ReadNum(SetWcFeeRatePct, DefWcFee);
            decimal sendUsdt = WasabiCardFundingMath.GrossUpSend(netUsd, wcFeePct);
            double dailyCap = ReadNum(SetDailyCapUsd, DefDailyCap);

            // Atomically reserve the slot BEFORE sending: one SERIALIZABLE transaction enforces
            // idempotency (unique PartnerReferenceID), the 24h cap, and the single-in-flight-floor
            // rule together — closing the check-then-act races (external red-team F1/F2). Only a
            // 'Reserved' result proceeds to an actual transfer.
            switch (Reserve(partnerRef, type, depositTxId, netUsd, sendUsdt, (decimal)dailyCap))
            {
                case ReserveResult.Duplicate:
                    return new { partnerRef, skipped = "duplicate" };
                case ReserveResult.FloorInFlight:
                    return new { partnerRef, skipped = "floor_in_flight" };
                case ReserveResult.CapExceeded:
                    Alert("WasabiCard auto-fund daily cap reached",
                        "A " + type + " transfer of net $" + netUsd + " was REFUSED: it would exceed the 24h cap $" +
                        dailyCap + ". Auto-funding is paused until the window rolls off; review for anomalies.");
                    return new { partnerRef, skipped = "daily_cap", netUsd };
            }

            TransferOutcome outcome = PGCryptoService.createTransfer(new TransferRequestModel
            {
                CoinID = coinId,
                isToken = 1,
                TokenID = tokenId,
                Amount = sendUsdt,
                Address = address,
                isFeeIncluded = 0, // Runegate network fee charged ON TOP (Amount lands in full)
                PartnerReferenceID = partnerRef
            });

            if (outcome.Submitted)
            {
                UpdateStatus(partnerRef, StSubmitted, ProviderRef(outcome), "submitted");
                return new { partnerRef, status = StSubmitted, netUsd, sendUsdt };
            }
            if (outcome.DefinitiveReject)
            {
                // Provider rejected; money did NOT move -> safe to mark Failed (frees the floor guard).
                UpdateStatus(partnerRef, StFailed, ProviderRef(outcome), "definitive_reject:" + outcome.EnvelopeStatus);
                Alert("WasabiCard auto-fund: transfer rejected",
                    "A " + type + " transfer of $" + netUsd + " (send " + sendUsdt + " USDT) was rejected by Runegate (status " + outcome.EnvelopeStatus + ").");
                return new { partnerRef, status = StFailed, reason = outcome.EnvelopeStatus };
            }
            // Ambiguous: outcome unknown -> leave in-flight, NEVER auto-retry. Alert for manual review.
            UpdateStatus(partnerRef, StUnknown, ProviderRef(outcome), "ambiguous:" + outcome.EnvelopeStatus);
            Alert("WasabiCard auto-fund: transfer outcome UNKNOWN",
                "A " + type + " transfer of $" + netUsd + " (send " + sendUsdt + " USDT, ref " + partnerRef +
                ") returned an ambiguous result (" + outcome.EnvelopeStatus + "). The funds MAY have moved. Do not retry; verify on Runegate and reconcile the ledger row manually.");
            return new { partnerRef, status = StUnknown, reason = outcome.EnvelopeStatus };
        }

        private static string ProviderRef(TransferOutcome o)
        {
            if (o == null || o.Result == null) return null;
            return o.Result.TransferID ?? o.Result.ID ?? o.Result.TxHash;
        }

        // ================= WasabiCard float + liability reads =================

        /// <summary>The merchant USD wallet available balance, or null if the read failed/garbled.</summary>
        public static decimal? ReadFloatUsd()
        {
            WCAccountInfoResponseModel info = WasabiCardService.getAccountInfo();
            if (info == null || !info.success || info.code != 200 || info.data == null) return null;
            // Fail CLOSED if there is no USD entry — never treat another currency's balance as the
            // USD float (that would size a refill / alert off a number we don't actually understand).
            var usd = info.data.FirstOrDefault(d => string.Equals(d.currency, "USD", StringComparison.OrdinalIgnoreCase));
            if (usd == null) return null;
            decimal bal;
            if (!decimal.TryParse(usd.availableBalance, NumberStyles.Any, CultureInfo.InvariantCulture, out bal))
                return null;
            return bal;
        }

        /// <summary>Total outstanding user wallet liability: SUM of active USDT balances.</summary>
        public static decimal TotalLiabilityUsd()
        {
            using (var db = new DBEntities())
            {
                var rows = db.Database.SqlQuery<decimal?>(
                    "SELECT SUM(ISNULL(Balance,0)) FROM dbo.tblM_User_Balance WHERE Currency = @cur AND isActive = 1",
                    P("@cur", WalletService.CurrencyUSDT)).ToList();
                return rows.Count > 0 && rows[0].HasValue ? rows[0].Value : 0m;
            }
        }

        // ================= refill ledger queries =================
        // "In-flight" = money on the way that the float read does NOT yet reflect. A Submitted
        // transfer lands within minutes, after which the float read includes it — so counting it
        // as in-flight past the staleness window would double-count it against the float and (for
        // the floor) block all future refills forever. Submitted therefore counts only while recent;
        // the uncertain states (Initiated = crashed mid-call, Unknown = ambiguous outcome) count
        // indefinitely until manually resolved (fail-safe: never auto-bypass a maybe-sent transfer).
        private static string InFlightWhere()
        {
            return "((Status = '" + StSubmitted + "' AND CreatedDate > @stale) " +
                   "OR Status IN ('" + StInitiated + "','" + StUnknown + "'))";
        }
        private static SqlParameter StaleParam()
        {
            return P("@stale", DateTime.Now.AddMinutes(-ReadNum(SetInFlightStaleMinutes, DefStaleMinutes)));
        }

        private static decimal InFlightUsd()
        {
            using (var db = new DBEntities())
            {
                var rows = db.Database.SqlQuery<decimal?>(
                    "SELECT SUM(ISNULL(NetUsd,0)) FROM dbo.tblH_WasabiCard_Refill WHERE " + InFlightWhere(),
                    StaleParam()).ToList();
                return rows.Count > 0 && rows[0].HasValue ? rows[0].Value : 0m;
            }
        }
        private static bool HasInFlightFloor()
        {
            using (var db = new DBEntities())
            {
                var rows = db.Database.SqlQuery<int>(
                    "SELECT COUNT(*) FROM dbo.tblH_WasabiCard_Refill WHERE RefillType = 'floor' AND " + InFlightWhere(),
                    StaleParam()).ToList();
                return rows.Count > 0 && rows[0] > 0;
            }
        }
        private enum ReserveResult { Reserved, Duplicate, CapExceeded, FloorInFlight }

        /// <summary>
        /// Atomically reserve a transfer slot. A single conditional INSERT, run in a SERIALIZABLE
        /// transaction so the cap-SUM and floor-in-flight reads range-lock the rows they read, inserts
        /// the Initiated row ONLY IF (a) it would not breach the 24h cap and (b) for a floor refill, no
        /// floor transfer is already in flight. This makes the cap + floor guards atomic with the
        /// insert — closing the check-then-act races (external red-team F1/F2) — while the unique index
        /// on PartnerReferenceID remains the idempotency gate (a duplicate key => no-op). Returns which
        /// gate (if any) blocked, so the caller can alert appropriately.
        /// </summary>
        private static ReserveResult Reserve(string partnerRef, string type, string depositTxId,
            decimal netUsd, decimal sendUsdt, decimal dailyCap)
        {
            string sql =
                "INSERT INTO dbo.tblH_WasabiCard_Refill " +
                "(RefillType, PartnerReferenceID, DepositTxId, NetUsd, SentUsdt, Status, CreatedDate, UpdatedDate) " +
                "SELECT @type, @ref, @dep, @net, @send, '" + StInitiated + "', @now, @now " +
                "WHERE (SELECT ISNULL(SUM(NetUsd),0) FROM dbo.tblH_WasabiCard_Refill " +
                "       WHERE Status <> '" + StFailed + "' AND CreatedDate > @cutoff) + @net <= @cap " +
                "  AND (@type <> '" + StFloorType + "' OR NOT EXISTS (SELECT 1 FROM dbo.tblH_WasabiCard_Refill " +
                "       WHERE RefillType = '" + StFloorType + "' AND " + InFlightWhere() + "))";

            using (var ctx = new DBEntities())
            using (var tx = ctx.Database.BeginTransaction(IsolationLevel.Serializable))
            {
                try
                {
                    int rows = ctx.Database.ExecuteSqlCommand(sql,
                        P("@type", type), P("@ref", partnerRef), P("@dep", depositTxId),
                        P("@net", netUsd), P("@send", sendUsdt), P("@now", DateTime.Now),
                        P("@cutoff", DateTime.Now.AddHours(-24)), P("@cap", dailyCap), StaleParam());
                    tx.Commit();
                    if (rows == 1) return ReserveResult.Reserved;
                    // 0 rows: the cap WHERE or (for floor) the in-flight WHERE blocked it. Disambiguate
                    // for the alert with a cheap read (post-commit).
                    if (type == StFloorType && HasInFlightFloor()) return ReserveResult.FloorInFlight;
                    return ReserveResult.CapExceeded;
                }
                catch (Exception ex) when (IsDuplicateKey(ex))
                {
                    tx.Rollback();
                    return ReserveResult.Duplicate;
                }
                catch
                {
                    tx.Rollback();
                    throw;
                }
            }
        }
        private static void UpdateStatus(string partnerRef, string status, string providerRef, string note)
        {
            using (var db = new DBEntities())
            {
                db.Database.ExecuteSqlCommand(
                    "UPDATE dbo.tblH_WasabiCard_Refill SET Status = @st, ProviderRef = @pref, Note = @note, UpdatedDate = @now " +
                    "WHERE PartnerReferenceID = @ref",
                    P("@st", status), P("@pref", providerRef), P("@note", note),
                    P("@now", DateTime.Now), P("@ref", partnerRef));
            }
        }

        private static bool IsDuplicateKey(Exception ex)
        {
            for (Exception e = ex; e != null; e = e.InnerException)
            {
                var se = e as SqlException;
                if (se != null)
                    foreach (SqlError err in se.Errors)
                        if (err.Number == 2627 || err.Number == 2601) return true;
            }
            return false;
        }

        // ================= coin/token + address helpers =================
        private static bool ResolveTronUsdt(out string coinId, out string tokenId)
        {
            coinId = (ReadParam1(SetTronCoinId) ?? "").Trim();
            tokenId = (ReadParam1(SetUsdtTokenId) ?? "").Trim();
            if (coinId.Length > 0 && tokenId.Length > 0) return true;

            if (coinId.Length == 0)
            {
                var coins = PGCryptoService.getCoin();
                var tron = coins?.FirstOrDefault(c => string.Equals(c.Network, "TRC20", StringComparison.OrdinalIgnoreCase));
                if (tron == null || string.IsNullOrEmpty(tron.CoinID)) return false;
                coinId = tron.CoinID;
            }
            if (tokenId.Length == 0)
            {
                var tokens = PGCryptoService.getToken("TRC20");
                var usdt = tokens?.FirstOrDefault(t => string.Equals(t.Symbol, "USDT", StringComparison.OrdinalIgnoreCase));
                if (usdt == null || string.IsNullOrEmpty(usdt.TokenID)) return false;
                tokenId = usdt.TokenID;
            }
            return coinId.Length > 0 && tokenId.Length > 0;
        }

        private static bool LooksLikeTronAddress(string a)
        {
            return !string.IsNullOrEmpty(a) && a.Length == 34 && a[0] == 'T';
        }

        // ================= alerting (WC1/WC3) =================
        private static void EvaluateAlert(decimal? floatUsd, decimal inFlight, decimal floor, decimal liability, Dictionary<string, object> summary)
        {
            string state;
            string subject;
            string body;

            if (!floatUsd.HasValue)
            {
                state = "read_fail";
                subject = "WasabiCard monitor: balance read FAILED";
                body = "getAccountInfo did not return a usable USD balance this tick. Card spends still work off the existing float, but monitoring is blind until this clears.";
            }
            else if (floatUsd.Value < floor)
            {
                state = "low";
                subject = "WasabiCard float LOW: $" + floatUsd.Value + " (floor $" + floor + ")";
                body = "Available float $" + floatUsd.Value + " is below the floor $" + floor + ". In-flight refills: $" + inFlight +
                       ". Total user liability (active USDT): $" + liability +
                       ". Coverage ratio: " + (liability > 0 ? Math.Round(floatUsd.Value / liability, 4).ToString() : "n/a") +
                       ". If auto-funding is OFF, top up manually.";
            }
            else
            {
                state = "ok";
                subject = null; body = null;
            }

            summary["alertState"] = state;
            if (state == "ok") { WriteAlertState("ok"); return; }

            // Throttle: alert on transition into a non-ok state, or re-alert every N hours while it persists.
            string prevState; DateTime? lastAt;
            ReadAlertState(out prevState, out lastAt);
            double reAlertHours = ReadNum(SetReAlertHours, DefReAlertHours);
            bool transitioned = !string.Equals(prevState, state, StringComparison.Ordinal);
            bool stale = !lastAt.HasValue || (DateTime.Now - lastAt.Value).TotalHours >= reAlertHours;
            if (transitioned || stale)
            {
                Alert(subject, body);
                WriteAlertState(state);
                summary["alertSent"] = true;
            }
            else
            {
                summary["alertSent"] = false;
            }
        }

        private static void ReadAlertState(out string state, out DateTime? lastAt)
        {
            state = null; lastAt = null;
            using (var db = new DBEntities())
            {
                var s = db.tblM_Setting.FirstOrDefault(p => p.Name == SetAlertState);
                if (s == null) return;
                state = s.Param1;
                DateTime t;
                if (!string.IsNullOrEmpty(s.Param2) &&
                    DateTime.TryParse(s.Param2, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out t))
                    lastAt = t;
            }
        }
        private static void WriteAlertState(string state)
        {
            using (var db = new DBEntities())
            {
                string nowIso = DateTime.Now.ToString("o", CultureInfo.InvariantCulture);
                int rows = db.Database.ExecuteSqlCommand(
                    "UPDATE dbo.tblM_Setting SET Param1 = @st, Param2 = @now WHERE Name = @name",
                    P("@st", state), P("@now", nowIso), P("@name", SetAlertState));
                if (rows == 0)
                    db.Database.ExecuteSqlCommand(
                        "INSERT INTO dbo.tblM_Setting (Name, Value, DateCreated, Param1, Param2) VALUES (@name, NULL, @dc, @st, @now)",
                        P("@name", SetAlertState), P("@dc", DateTime.Now), P("@st", state), P("@now", nowIso));
            }
        }

        private static void Alert(string subject, string body)
        {
            try
            {
                string to = ReadParam1(SetAlertEmail); // optional DB-tunable recipient; null -> env/EMAIL_FROM
                NotificationService.sendOpsAlert(subject, body, to);
            }
            catch (Exception ex) { System.Diagnostics.Trace.TraceError("Ops alert send failed: " + ex); }
        }
    }
}
