using System;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using System.Linq;
using QryptoCard.INT.Model.Service;
using QryptoCard.Sec;

namespace QryptoCard.INT.Script.Service
{
    /// <summary>
    /// Deposit-into-card: the user-facing lifecycle of a "card funding intent" — the record that a
    /// user wants to fund a card (a NEW card, or a TOP-UP of an owned card) by depositing crypto to
    /// their fixed address. Managed by raw SQL over dbo.tblT_Card_Funding_Intent (no EF entity),
    /// mirroring WasabiCardFundingService.
    ///
    /// This INT-tier service owns creation / status / cancel. Settlement (matching a landed deposit,
    /// forwarding to WasabiCard, confirming, and issuing the card) is driven separately by the
    /// Callback tier + an issuance tick, communicating only through the intent's Status column.
    ///
    /// The whole path is gated by CardFundingStreamingEnabled (ships OFF).
    /// </summary>
    public static class CardFundingIntentService
    {
        // Lifecycle statuses (string constants). The OPEN set (Pending/Funding/Confirming/Issuing) is
        // what the filtered unique index UX_CFI_OneOpenPerUser enforces one-per-user on.
        // Shared source of truth in QryptoCard.Sec so the two tiers' status sets can't drift.
        public const string StPending = CardFundingStatuses.Pending;
        public const string StFunding = CardFundingStatuses.Funding;
        public const string StConfirming = CardFundingStatuses.Confirming;
        public const string StIssuing = CardFundingStatuses.Issuing;
        public const string StCompleted = CardFundingStatuses.Completed;
        public const string StExpired = CardFundingStatuses.Expired;
        public const string StCancelled = CardFundingStatuses.Cancelled;
        public const string StFailed = CardFundingStatuses.Failed;

        public const string KindNew = "new";
        public const string KindTopUp = "topup";

        // Settings / env (env wins, then DB, then default) — mirrors the funding service.
        // Keys come from the shared QryptoCard.Sec.CardFundingGate so the Callback-tier copy can't drift.
        public const string SetEnabled = CardFundingGate.SettingEnabled;
        public const string EnvEnabled = CardFundingGate.EnvEnabled;
        public const string SetMinDepositUsd = "CardMinDepositUsd";
        public const string SetExpiryMinutes = "CardFundingIntentExpiryMinutes";
        private const double DefMinDepositUsd = 0d;   // 0 => use the WasabiCard per-program quota
        private const double DefExpiryMinutes = 1440d;

        public class Result
        {
            public bool Ok;
            public string Error;      // customer-safe message when !Ok
            public bool Retryable;    // transient (provider/catalog) vs a hard rejection

            public string IntentID;
            public string DepositAddress;
            public string Network;    // "TRC20"
            public string Coin;       // "USDT"
            public decimal Face;
            public decimal Price;
            public decimal PercentageFee;
            public decimal FixedFee;
            public decimal ExpectedTotal;
            public string Status;

            public static Result Fail(string msg, bool retryable) { return new Result { Ok = false, Error = msg, Retryable = retryable }; }
        }

        public class StatusResult
        {
            public bool Found;
            public string IntentID;
            public string Kind;
            public string Status;
            public decimal ExpectedTotal;
            public decimal ReceivedTotal;
            public string CardNo;     // populated once issued
        }

        // ---- gate ------------------------------------------------------------

        public static bool Enabled()
        {
            string e = SecretsConfig.GetOptional(EnvEnabled, null);
            if (!string.IsNullOrWhiteSpace(e))
                return e.Trim() == "1" || string.Equals(e.Trim(), "true", StringComparison.OrdinalIgnoreCase);
            return ReadNum(SetEnabled, 0) >= 1;
        }

        // ---- create: new card -----------------------------------------------

        public static Result CreateNewCard(string userId, string email, long cardTypeId, decimal face)
        {
            if (!Enabled()) return Result.Fail("Card funding is not available.", false);
            if (string.IsNullOrWhiteSpace(userId)) return Result.Fail("Not signed in.", false);
            if (face <= 0m || face != Math.Truncate(face)) return Result.Fail("Deposit amount must be a whole number.", false);

            var data = CardCatalogService.GetById(cardTypeId);
            if (data == null) return Result.Fail("Card is not available.", true);

            // Min/max: WasabiCard per-program quota, raised by the optional CardMinDepositUsd override.
            double programMin = ToDouble(data.DepositAmountMinQuotaForActiveCard);
            double programMax = ToDouble(data.DepositAmountMaxQuotaForActiveCard);
            double minDeposit = Math.Max(programMin, ReadNum(SetMinDepositUsd, DefMinDepositUsd));
            if ((double)face < minDeposit) return Result.Fail("Minimum deposit amount is " + minDeposit.ToString("0.##") + " USD.", false);
            if (programMax > 0 && (double)face > programMax) return Result.Fail("Maximum deposit amount is " + programMax.ToString("0.##") + " USD.", false);

            using (var db = new DBEntities())
            {
                // KYC fail-fast: create/resolve the holder BEFORE the customer sends any crypto.
                var holder = CardholderProvisioningService.EnsureHolder(db, userId, email, data);
                if (!holder.Ok) return Result.Fail(holder.Error, holder.Retryable);

                decimal price = (decimal)CardCatalogService.PriceOf(data);
                double feePct = CardCatalogService.GetDepositFeeRate();
                decimal fixedFee = (decimal)CardCatalogService.GetFixedDepositFee();
                decimal pctFee = CardFundingMath.PercentageFee(feePct, face);
                decimal expectedTotal = CardFundingMath.ExpectedTotal(price, face, feePct, fixedFee);

                var addr = WalletService.EnsureDepositAddress(userId);
                if (addr == null || string.IsNullOrWhiteSpace(addr.Address))
                    return Result.Fail("Deposit address is temporarily unavailable. Please try again shortly.", true);

                return InsertIntent(db, userId, KindNew, cardTypeId, holder.HolderId, null, addr.Address,
                    face, price, feePct, pctFee, fixedFee, expectedTotal);
            }
        }

        // ---- create: top-up an owned card -----------------------------------

        public static Result CreateTopUp(string userId, string cardNo, decimal face)
        {
            if (!Enabled()) return Result.Fail("Card funding is not available.", false);
            if (string.IsNullOrWhiteSpace(userId)) return Result.Fail("Not signed in.", false);
            if (string.IsNullOrWhiteSpace(cardNo)) return Result.Fail("Card is required.", false);
            if (face <= 0m || face != Math.Truncate(face)) return Result.Fail("Top-up amount must be a whole number.", false);

            using (var db = new DBEntities())
            {
                // Ownership: the card must belong to this user and be issued (has a CardNo, active).
                var card = db.tblT_Card.FirstOrDefault(c => c.CardNo == cardNo && c.UserID == userId && c.isActive == 1);
                if (card == null) return Result.Fail("Card not found.", false);

                // Enforce the card type's TOP-UP (recharge) quota so an over-limit top-up can't push
                // surplus into WasabiCard's one-way float (it would be rejected at issuance, refunded to
                // the user, and left stranded in the float). Mirrors the min/max guard in CreateNewCard.
                var ct = card.CardTypeId.HasValue ? CardCatalogService.GetById(card.CardTypeId.Value) : null;
                if (ct == null) return Result.Fail("Card is not available for top-up right now. Please try again shortly.", true);
                double rMin = Math.Max(ToDouble(ct.RechargeMinQuota), ReadNum(SetMinDepositUsd, DefMinDepositUsd));
                double rMax = ToDouble(ct.RechargeMaxQuota);
                if (rMin > 0 && (double)face < rMin) return Result.Fail("Minimum top-up amount is " + rMin.ToString("0.##") + " USD.", false);
                if (rMax > 0 && (double)face > rMax) return Result.Fail("Maximum top-up amount is " + rMax.ToString("0.##") + " USD.", false);

                double feePct = CardCatalogService.GetDepositFeeRate();
                decimal fixedFee = (decimal)CardCatalogService.GetFixedDepositFee();
                decimal pctFee = CardFundingMath.PercentageFee(feePct, face);
                decimal expectedTotal = CardFundingMath.ExpectedTotal(0m, face, feePct, fixedFee); // no card price on a top-up

                var addr = WalletService.EnsureDepositAddress(userId);
                if (addr == null || string.IsNullOrWhiteSpace(addr.Address))
                    return Result.Fail("Deposit address is temporarily unavailable. Please try again shortly.", true);

                return InsertIntent(db, userId, KindTopUp, null, null, cardNo, addr.Address,
                    face, 0m, feePct, pctFee, fixedFee, expectedTotal);
            }
        }

        // Race-safe insert enforcing ONE open intent per user (conditional insert under SERIALIZABLE,
        // backed by the filtered unique index UX_CFI_OneOpenPerUser).
        private static Result InsertIntent(DBEntities db, string userId, string kind, long? cardTypeId,
            long? holderId, string cardNo, string address, decimal face, decimal price, double feePct,
            decimal pctFee, decimal fixedFee, decimal expectedTotal)
        {
            string intentId = Guid.NewGuid().ToString("N");
            DateTime now = DateTime.Now;
            DateTime expiry = now.AddMinutes(ReadNum(SetExpiryMinutes, DefExpiryMinutes));

            const string sql =
                "INSERT INTO dbo.tblT_Card_Funding_Intent " +
                "(IntentID, UserID, Kind, CardTypeId, HolderID, CardNo, DepositAddress, " +
                " Face, Price, FeeInPercentage, PercentageFee, FixedFee, ExpectedTotal, ReceivedTotal, " +
                " Status, CreatedDate, ExpiryDate) " +
                "SELECT @intentId, @userId, @kind, @cardTypeId, @holderId, @cardNo, @addr, " +
                " @face, @price, @feePct, @pctFee, @fixedFee, @expectedTotal, 0, " +
                " 'Pending', @now, @expiry " +
                "WHERE NOT EXISTS (SELECT 1 FROM dbo.tblT_Card_Funding_Intent WITH (UPDLOCK, HOLDLOCK) " +
                "                  WHERE UserID = @userId AND Status IN ('Pending','Funding','Confirming','Issuing'))";

            using (var tx = db.Database.BeginTransaction(IsolationLevel.Serializable))
            {
                try
                {
                    int rows = db.Database.ExecuteSqlCommand(sql,
                        P("@intentId", intentId), P("@userId", userId), P("@kind", kind),
                        P("@cardTypeId", (object)cardTypeId), P("@holderId", (object)holderId),
                        P("@cardNo", (object)cardNo), P("@addr", address),
                        P("@face", face), P("@price", price), P("@feePct", (decimal)feePct),
                        P("@pctFee", pctFee), P("@fixedFee", fixedFee), P("@expectedTotal", expectedTotal),
                        P("@now", now), P("@expiry", expiry));
                    tx.Commit();

                    if (rows != 1)
                        return Result.Fail("You already have a card being funded. Finish or cancel it first.", false);
                }
                catch (Exception ex) when (IsDuplicateKey(ex))
                {
                    tx.Rollback();
                    return Result.Fail("You already have a card being funded. Finish or cancel it first.", false);
                }
                catch
                {
                    tx.Rollback();
                    throw;
                }
            }

            return new Result
            {
                Ok = true,
                IntentID = intentId,
                DepositAddress = address,
                Network = "TRC20",
                Coin = "USDT",
                Face = face,
                Price = price,
                PercentageFee = pctFee,
                FixedFee = fixedFee,
                ExpectedTotal = expectedTotal,
                Status = StPending,
            };
        }

        // ---- status ----------------------------------------------------------

        public static StatusResult GetStatus(string userId, string intentId)
        {
            using (var db = new DBEntities())
            {
                var rows = db.Database.SqlQuery<IntentRow>(
                    "SELECT TOP 1 IntentID, Kind, Status, ExpectedTotal, ReceivedTotal, CardNo " +
                    "FROM dbo.tblT_Card_Funding_Intent WHERE IntentID = @id AND UserID = @u",
                    P("@id", intentId), P("@u", userId)).ToList();
                if (rows.Count == 0) return new StatusResult { Found = false };
                var r = rows[0];
                return new StatusResult
                {
                    Found = true,
                    IntentID = r.IntentID,
                    Kind = r.Kind,
                    Status = r.Status,
                    ExpectedTotal = r.ExpectedTotal,
                    ReceivedTotal = r.ReceivedTotal,
                    CardNo = r.CardNo,
                };
            }
        }

        // ---- cancel (only while still awaiting funds) ------------------------

        public static bool Cancel(string userId, string intentId)
        {
            using (var db = new DBEntities())
            {
                int rows = db.Database.ExecuteSqlCommand(
                    "UPDATE dbo.tblT_Card_Funding_Intent SET Status = 'Cancelled', UpdatedDate = @now " +
                    "WHERE IntentID = @id AND UserID = @u AND Status = 'Pending'",
                    P("@now", DateTime.Now), P("@id", intentId), P("@u", userId));
                return rows == 1;
            }
        }

        // ---- helpers ---------------------------------------------------------

        private class IntentRow
        {
            public string IntentID { get; set; }
            public string Kind { get; set; }
            public string Status { get; set; }
            public decimal ExpectedTotal { get; set; }
            public decimal ReceivedTotal { get; set; }
            public string CardNo { get; set; }
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

        private static double ToDouble(string s)
        {
            double v;
            return double.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out v) ? v : 0d;
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
    }
}
