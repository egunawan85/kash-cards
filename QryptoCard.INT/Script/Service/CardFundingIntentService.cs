using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using QryptoCard.INT.Model.PGCrypto;
using QryptoCard.INT.Model.Service;
using QryptoCard.INT.Script.Gateway.PGCrypto;
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

        // Runegate invoice config (merchant-specific string ids; operator-provided via Param1/env).
        // Required to mint a per-intent USDT-TRC20 invoice. The customer MUST be dynamic-address
        // (isStaticAddress=0) so each invoice gets its own address and concurrent invoices are allowed.
        public const string SetPaymentId = "RunegatePaymentId";
        public const string EnvPaymentId = "RUNEGATE_PAYMENT_ID";
        public const string SetCustomerId = "RunegateCustomerId";
        public const string EnvCustomerId = "RUNEGATE_CUSTOMER_ID";
        public const string SetProductId = "RunegateProductId";
        public const string EnvProductId = "RUNEGATE_PRODUCT_ID";

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
            // Snapshot echoed so the app can RE-SHOW the funding screen (address + QR + breakdown)
            // when the user re-opens an in-progress intent from the card list — the create response is
            // not persisted client-side. Read-only projection of the intent's own row.
            public string DepositAddress;
            public decimal Face;
            public decimal Price;
            public decimal PercentageFee;
            public decimal FixedFee;
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

                // Mint a per-intent Runegate invoice (unique address + amount, tagged PartnerReferenceID
                // = intentId) INSTEAD of the shared static address. Attribution of the landed deposit is
                // then unambiguous per intent, and multiple concurrent intents are safe.
                string intentId = Guid.NewGuid().ToString("N");
                var inv = CreateInvoiceForIntent(intentId, expectedTotal);
                if (inv == null)
                    return Result.Fail("Deposit request is temporarily unavailable. Please try again shortly.", true);

                return InsertIntent(db, intentId, userId, KindNew, cardTypeId, holder.HolderId, null,
                    inv.Address, inv.InvoiceID, face, price, feePct, pctFee, fixedFee, expectedTotal);
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

                string intentId = Guid.NewGuid().ToString("N");
                var inv = CreateInvoiceForIntent(intentId, expectedTotal);
                if (inv == null)
                    return Result.Fail("Deposit request is temporarily unavailable. Please try again shortly.", true);

                return InsertIntent(db, intentId, userId, KindTopUp, null, null, cardNo,
                    inv.Address, inv.InvoiceID, face, 0m, feePct, pctFee, fixedFee, expectedTotal);
            }
        }

        // Plain insert — multiple concurrent intents per user are now allowed (each isolated by its own
        // invoice/address; a landed deposit is attributed by the invoice's PartnerReferenceID, not by
        // "the user's single open intent"). IntentID is unique (UX_CFI_IntentID); a GUID collision is
        // astronomically unlikely and surfaces as a retryable duplicate-key error.
        private static Result InsertIntent(DBEntities db, string intentId, string userId, string kind, long? cardTypeId,
            long? holderId, string cardNo, string invoiceAddress, string invoiceId, decimal face, decimal price, double feePct,
            decimal pctFee, decimal fixedFee, decimal expectedTotal)
        {
            DateTime now = DateTime.Now;
            DateTime expiry = now.AddMinutes(ReadNum(SetExpiryMinutes, DefExpiryMinutes));

            const string sql =
                "INSERT INTO dbo.tblT_Card_Funding_Intent " +
                "(IntentID, UserID, Kind, CardTypeId, HolderID, CardNo, DepositAddress, InvoiceID, InvoiceAddress, " +
                " Face, Price, FeeInPercentage, PercentageFee, FixedFee, ExpectedTotal, ReceivedTotal, " +
                " Status, CreatedDate, ExpiryDate) " +
                "VALUES (@intentId, @userId, @kind, @cardTypeId, @holderId, @cardNo, @addr, @invId, @addr, " +
                " @face, @price, @feePct, @pctFee, @fixedFee, @expectedTotal, 0, " +
                " 'Pending', @now, @expiry)";

            try
            {
                db.Database.ExecuteSqlCommand(sql,
                    P("@intentId", intentId), P("@userId", userId), P("@kind", kind),
                    P("@cardTypeId", (object)cardTypeId), P("@holderId", (object)holderId),
                    P("@cardNo", (object)cardNo), P("@addr", invoiceAddress), P("@invId", (object)invoiceId),
                    P("@face", face), P("@price", price), P("@feePct", (decimal)feePct),
                    P("@pctFee", pctFee), P("@fixedFee", fixedFee), P("@expectedTotal", expectedTotal),
                    P("@now", now), P("@expiry", expiry));
            }
            catch (Exception ex) when (IsDuplicateKey(ex))
            {
                return Result.Fail("Could not start card funding. Please try again.", true);
            }

            return new Result
            {
                Ok = true,
                IntentID = intentId,
                DepositAddress = invoiceAddress,
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

        // Mint a Runegate invoice for this intent: unique deposit address + amount = ExpectedTotal (one
        // product line, Price overridden), tagged PartnerReferenceID = intentId (idempotent per merchant)
        // and DateExpired matching the intent expiry. Returns the invoice (Address + InvoiceID) or null if
        // config is missing / the gateway fails / the returned coin isn't USDT (defensive — never show a
        // non-USDT address). The merchant customer MUST be dynamic-address (isStaticAddress=0).
        private static InvoiceModel CreateInvoiceForIntent(string intentId, decimal expectedTotal)
        {
            string paymentId = ReadParam1(EnvPaymentId, SetPaymentId);
            string customerId = ReadParam1(EnvCustomerId, SetCustomerId);
            string productId = ReadParam1(EnvProductId, SetProductId);
            if (string.IsNullOrWhiteSpace(paymentId) || string.IsNullOrWhiteSpace(customerId) || string.IsNullOrWhiteSpace(productId))
            {
                Trace.TraceError("CardFundingIntent: Runegate invoice config (RunegatePaymentId/CustomerId/ProductId) not set — cannot create invoice.");
                return null;
            }

            var req = new InvoiceModel
            {
                PaymentID = paymentId,
                CustomerID = customerId,
                PartnerReferenceID = intentId,
                DateExpired = DateTime.Now.AddMinutes(ReadNum(SetExpiryMinutes, DefExpiryMinutes)),
                Notes = "Card funding " + intentId,
                Products = new List<ProductModel>
                {
                    new ProductModel { ProductID = productId, Price = expectedTotal, Quantity = 1 }
                }
            };

            InvoiceModel resp;
            try { resp = PGCryptoService.createInvoice(req); }
            catch (Exception ex) { Trace.TraceError("CardFundingIntent: createInvoice threw: " + ex.GetType().FullName); resp = null; }
            if (resp == null || string.IsNullOrWhiteSpace(resp.Address)) return null;
            // Defensive: only proceed with a USDT invoice (the merchant PaymentID should resolve to
            // USDT-TRC20; never hand the customer a non-USDT address).
            if (!string.IsNullOrEmpty(resp.Symbol) && !string.Equals(resp.Symbol, "USDT", StringComparison.OrdinalIgnoreCase))
            {
                Trace.TraceError("CardFundingIntent: invoice returned non-USDT symbol '" + resp.Symbol + "' — rejecting.");
                return null;
            }
            return resp;
        }

        private static string ReadParam1(string envName, string dbName)
        {
            string e = SecretsConfig.GetOptional(envName, null);
            if (!string.IsNullOrWhiteSpace(e)) return e;
            using (var db = new DBEntities())
            {
                var s = db.tblM_Setting.FirstOrDefault(p => p.Name == dbName);
                return s != null ? s.Param1 : null;
            }
        }

        // ---- status ----------------------------------------------------------

        public static StatusResult GetStatus(string userId, string intentId)
        {
            using (var db = new DBEntities())
            {
                var rows = db.Database.SqlQuery<IntentRow>(
                    "SELECT TOP 1 IntentID, Kind, Status, ExpectedTotal, ReceivedTotal, CardNo, " +
                    "DepositAddress, Face, Price, PercentageFee, FixedFee " +
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
                    DepositAddress = r.DepositAddress,
                    Face = r.Face,
                    Price = r.Price,
                    PercentageFee = r.PercentageFee,
                    FixedFee = r.FixedFee,
                };
            }
        }

        // ---- list a user's OPEN intents (for the card list "In progress" section) -----------
        // STRICTLY user-scoped (WHERE UserID = @u). Returns only in-flight intents (Pending/Funding/
        // Confirming/Issuing) so the app can render one tracker tile each; terminal intents are already
        // reflected as real cards / available balance and don't belong in an "in progress" list.
        public static System.Collections.Generic.List<StatusResult> ListOpen(string userId)
        {
            var outp = new System.Collections.Generic.List<StatusResult>();
            using (var db = new DBEntities())
            {
                var rows = db.Database.SqlQuery<IntentRow>(
                    "SELECT TOP 50 IntentID, Kind, Status, ExpectedTotal, ReceivedTotal, CardNo, " +
                    "DepositAddress, Face, Price, PercentageFee, FixedFee " +
                    "FROM dbo.tblT_Card_Funding_Intent " +
                    "WHERE UserID = @u AND Status IN ('Pending','Funding','Confirming','Issuing') " +
                    "ORDER BY ID DESC",
                    P("@u", userId)).ToList();
                foreach (var r in rows)
                    outp.Add(new StatusResult
                    {
                        Found = true,
                        IntentID = r.IntentID,
                        Kind = r.Kind,
                        Status = r.Status,
                        ExpectedTotal = r.ExpectedTotal,
                        ReceivedTotal = r.ReceivedTotal,
                        CardNo = r.CardNo,
                        DepositAddress = r.DepositAddress,
                        Face = r.Face,
                        Price = r.Price,
                        PercentageFee = r.PercentageFee,
                        FixedFee = r.FixedFee,
                    });
            }
            return outp;
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
            public string DepositAddress { get; set; }
            public decimal Face { get; set; }
            public decimal Price { get; set; }
            public decimal PercentageFee { get; set; }
            public decimal FixedFee { get; set; }
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
