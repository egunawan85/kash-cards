using System;
using System.Data;
using System.Data.Entity.Infrastructure;
using System.Data.SqlClient;
using System.Linq;
using Newtonsoft.Json;
using QryptoCard.INT.Model;
using QryptoCard.INT.Model.PGCrypto;
using QryptoCard.INT.Script.Gateway.PGCrypto;

namespace QryptoCard.INT.Script.Service
{
    /// <summary>
    /// Central prepaid-balance helper. This is the ONLY sanctioned way to mutate
    /// tblM_User_Balance: every credit/debit is a conditional UPDATE that captures the
    /// before/after balance via an OUTPUT clause and writes a tblH_User_Balance ledger
    /// row in the same Serializable transaction. There is no read-modify-write on a
    /// balance anywhere — the append-only ledger is the system of record and the master
    /// row is a running cache updated atomically alongside it.
    ///
    /// It also owns idempotent provisioning of the wallet row and the per-user static
    /// deposit address, so OTP-verify never has to provision inline (and can never leave
    /// a half-provisioned, address-less account on a gateway hiccup).
    ///
    /// Each WCF assembly keeps its own copy of this helper (mirroring the per-assembly
    /// WasabiCardService / gateway copies); the Callback tier carries the subset it needs.
    /// </summary>
    public static class WalletService
    {
        public const string CurrencyUSDT = "USDT";
        // The single TRC20 network row id used at registration today (UserV1Service).
        public const string TronNetworkId = "F580A411-0E37-4287-B975-408172A2B4BF";

        // Ledger Type constants — data convention, no DDL (R14).
        public const string TypeCryptoDeposit = "Crypto Deposit";
        public const string TypeCardOpen = "Card Open";
        public const string TypeCardTopup = "Card Topup";
        public const string TypeCardOpenReversal = "Card Open Reversal";
        public const string TypeCardTopupReversal = "Card Topup Reversal";
        public const string TypeDepositRefund = "Deposit Refund";
        public const string TypeReferralCommission = "Referral Commission";

        /// <summary>Outcome of an atomic balance mutation.</summary>
        public class BalanceMutationResult
        {
            public bool Success { get; set; }
            /// <summary>Null on success; a short machine-readable reason on failure.</summary>
            public string FailureReason { get; set; }
            public decimal BalancePrevious { get; set; }
            public decimal BalanceNew { get; set; }
            public long LedgerId { get; set; }

            public static BalanceMutationResult Fail(string reason)
            {
                return new BalanceMutationResult { Success = false, FailureReason = reason };
            }
        }

        // Projection of the UPDATE ... OUTPUT result set. Property names must match the
        // OUTPUT column aliases for EF6 SqlQuery materialization.
        private class BalanceDelta
        {
            public decimal Prev { get; set; }
            public decimal Curr { get; set; }
            public string BalanceID { get; set; }
        }

        // ---- Public mutation API -------------------------------------------------

        /// <summary>
        /// Credit a wallet by <paramref name="netAmount"/> (the amount that actually lands
        /// on the balance). The ledger records the gross <paramref name="grossAmount"/> and
        /// <paramref name="commission"/> for reconciliation (R15: credit net, record gross).
        /// The wallet row must already exist (call <see cref="EnsureWallet"/> first); a
        /// missing/inactive row fails closed rather than silently creating one mid-credit.
        /// </summary>
        public static BalanceMutationResult Credit(
            string userId, decimal netAmount, decimal commission,
            double commissionInPercentage, string type, string transactionId, string status = null)
        {
            if (netAmount < 0m) return BalanceMutationResult.Fail("negative_credit");
            // Amount stores the NET balance delta — not the gross deposit — so every ledger
            // row satisfies the canonical tamper invariant BalancePrevious + Amount = Balance
            // (the deployed forensic check). Commission is recorded separately; the gross
            // deposit is recoverable as Amount + Commision.
            return Mutate(
                userId: userId,
                balanceDelta: netAmount,
                requireMinBalance: false,
                minBalance: 0m,
                ledgerAmount: netAmount,
                ledgerCommission: commission,
                ledgerCommissionPct: commissionInPercentage,
                type: type,
                transactionId: transactionId,
                status: status,
                noRowReason: "wallet_missing");
        }

        /// <summary>
        /// Debit a wallet by <paramref name="amount"/>, enforcing the hard floor at 0 (R11):
        /// the conditional UPDATE only applies when Balance &gt;= amount, so a debit that
        /// would go negative affects 0 rows and fails closed with "insufficient_balance".
        /// The ledger records a negative Amount.
        /// </summary>
        public static BalanceMutationResult Debit(
            string userId, decimal amount, string type, string transactionId, string status = null)
        {
            if (amount < 0m) return BalanceMutationResult.Fail("negative_debit");
            return Mutate(
                userId: userId,
                balanceDelta: -amount,
                requireMinBalance: true,
                minBalance: amount,
                ledgerAmount: -amount,
                ledgerCommission: 0m,
                ledgerCommissionPct: 0d,
                type: type,
                transactionId: transactionId,
                status: status,
                noRowReason: "insufficient_balance");
        }

        /// <summary>
        /// Debit for a card spend, atomically transitioning the order out of its pre-debit status in
        /// the SAME transaction. A crash can then never leave an order holding a committed debit in a
        /// status nothing reconciles. <paramref name="claimSql"/> must be a conditional UPDATE that
        /// affects exactly one row (else the whole debit rolls back as "claim_lost").
        /// </summary>
        public static BalanceMutationResult DebitForOrder(
            string userId, decimal amount, string type, string transactionId,
            string claimSql, SqlParameter[] claimParams)
        {
            if (amount < 0m) return BalanceMutationResult.Fail("negative_debit");
            return Mutate(
                userId: userId,
                balanceDelta: -amount,
                requireMinBalance: true,
                minBalance: amount,
                ledgerAmount: -amount,
                ledgerCommission: 0m,
                ledgerCommissionPct: 0d,
                type: type,
                transactionId: transactionId,
                status: null,
                noRowReason: "insufficient_balance",
                claimSql: claimSql,
                claimParams: claimParams);
        }

        /// <summary>
        /// Compensating reversal credit for a failed card spend, atomically transitioning the order
        /// to Failed in the SAME transaction (claim-gated, mirroring the Callback-tier CreditRefund).
        /// The reversal credit and the status flip commit or roll back together, so there is no window
        /// in which the order sits at PendingProvider with the refund already issued (which a webhook
        /// or reTopup reconciler could double-act on). The claim is also the idempotency gate: a second
        /// reversal finds 0 rows ("claim_lost") and is a no-op, so the user can never be double-credited.
        /// </summary>
        public static BalanceMutationResult ReverseForOrder(
            string userId, decimal amount, string type, string orderId,
            string claimSql, SqlParameter[] claimParams)
        {
            if (amount < 0m) return BalanceMutationResult.Fail("negative_credit");
            return Mutate(
                userId: userId,
                balanceDelta: amount,
                requireMinBalance: false,
                minBalance: 0m,
                ledgerAmount: amount,
                ledgerCommission: 0m,
                ledgerCommissionPct: 0d,
                type: type,
                transactionId: orderId,
                status: null,
                noRowReason: "wallet_missing",
                claimSql: claimSql,
                claimParams: claimParams);
        }

        /// <summary>
        /// Credit a confirmed inbound deposit, deduped per-event in the SAME transaction as the
        /// credit. The dedup row (tblH_Partner_Webhook_ID, keyed on TransactionID|Status,
        /// Type='PGCrypto') is inserted first; a duplicate-key means this exact event already
        /// credited, so the whole transaction rolls back and returns "duplicate_event" — no
        /// double credit. Because the dedup insert and the balance mutation commit or roll back
        /// together, a crash between them can never leave a deposit deduped-but-uncredited.
        /// Credits the net amount; records commission. (Kept in parity with the Callback-tier
        /// copy, which is where the webhook actually invokes it.)
        /// </summary>
        public static BalanceMutationResult CreditDeposit(
            string userId, decimal netAmount, decimal commission, double commissionInPercentage,
            string transactionId, string status, string dedupRequest)
        {
            if (netAmount < 0m) return BalanceMutationResult.Fail("negative_credit");
            // Dedup on the provider TransactionID ALONE — not TransactionID|Status. Credit is
            // gated on isPaid==1 before this is ever called, so only confirmed events reach here;
            // including the free-form Status would let the same confirmed deposit, redelivered
            // with a different status string, credit more than once.
            string dedupKey = transactionId;
            return Mutate(
                userId: userId,
                balanceDelta: netAmount,
                requireMinBalance: false,
                minBalance: 0m,
                ledgerAmount: netAmount,
                ledgerCommission: commission,
                ledgerCommissionPct: commissionInPercentage,
                type: TypeCryptoDeposit,
                transactionId: transactionId,
                status: status,
                noRowReason: "wallet_missing",
                dedupType: "PGCrypto",
                dedupKey: dedupKey,
                dedupRequest: dedupRequest);
        }

        /// <summary>
        /// Credit a referrer's wallet with the commission earned on a referee's finalized card
        /// buy/top-up, deduped per referee order in the SAME transaction as the credit (dedup row
        /// tblH_Partner_Webhook_ID, Type='ReferralCommission', TXID=&lt;referee order id&gt;). A re-run
        /// (redelivered webhook, or the reconciliation sweep racing the webhook) hits the constraint
        /// and rolls back as "duplicate_event" — no double payout. The Type is namespaced so a referee
        /// order id can never collide with a PGCrypto deposit TXID. Caller computes
        /// amount = referrer-rate * referee-fee (capped at the fee) and ensures the referrer's wallet
        /// exists. Kept in parity with the Callback-tier copy, which is where the finalize invokes it.
        /// </summary>
        public static BalanceMutationResult CreditReferralCommission(
            string referrerUserId, decimal amount, string refereeOrderId, string dedupRequest)
        {
            if (amount <= 0m) return BalanceMutationResult.Fail("non_positive_commission");
            return Mutate(
                userId: referrerUserId,
                balanceDelta: amount,
                requireMinBalance: false,
                minBalance: 0m,
                ledgerAmount: amount,
                ledgerCommission: 0m,
                ledgerCommissionPct: 0d,
                type: TypeReferralCommission,
                transactionId: refereeOrderId,
                status: null,
                noRowReason: "wallet_missing",
                dedupType: "ReferralCommission",
                dedupKey: refereeOrderId,
                dedupRequest: dedupRequest);
        }

        // ---- Atomic core ---------------------------------------------------------

        private static BalanceMutationResult Mutate(
            string userId, decimal balanceDelta, bool requireMinBalance, decimal minBalance,
            decimal ledgerAmount, decimal ledgerCommission, double ledgerCommissionPct,
            string type, string transactionId, string status, string noRowReason,
            string dedupType = null, string dedupKey = null, string dedupRequest = null,
            string claimSql = null, SqlParameter[] claimParams = null)
        {
            // ISNULL guards a legacy NULL balance; the OUTPUT captures before/after in the
            // same statement so there is no read-then-update window. The debit variant adds
            // the Balance >= @minBalance predicate so an overdraft simply matches 0 rows.
            string updateSql =
                "UPDATE dbo.tblM_User_Balance " +
                "SET Balance = ISNULL(Balance, 0) + @delta, DateUpdated = @now " +
                "OUTPUT ISNULL(deleted.Balance, 0) AS Prev, inserted.Balance AS Curr, inserted.BalanceID AS BalanceID " +
                "WHERE UserID = @uid AND Currency = @cur AND isActive = 1" +
                (requireMinBalance ? " AND ISNULL(Balance, 0) >= @minBalance" : "");

            string insertSql =
                "INSERT INTO dbo.tblH_User_Balance " +
                "(UserID, BalanceID, TransactionID, Type, BalancePrevious, Amount, Commision, " +
                " CommisionInPercentage, Balance, BalanceHold, CreatedDate, Status) " +
                "OUTPUT inserted.ID " +
                "VALUES (@uid, @balId, @txid, @type, @prev, @amt, @comm, @commpct, @curr, 0, @now, @status)";

            DateTime now = DateTime.Now;

            using (var ctx = new DBEntities())
            using (var tx = ctx.Database.BeginTransaction(IsolationLevel.Serializable))
            {
                try
                {
                    // Optional claim in the SAME transaction (e.g. an order Created->PendingProvider
                    // transition): a conditional UPDATE that must affect exactly one row, so the order
                    // can never be left in a pre-debit status while holding a committed debit. 0 rows
                    // affected -> roll back (already transitioned / not in the expected state).
                    // (Ordered before the dedup block to match the Callback-tier copy.)
                    if (claimSql != null)
                    {
                        int claimed = ctx.Database.ExecuteSqlCommand(claimSql, claimParams);
                        if (claimed != 1)
                        {
                            tx.Rollback();
                            return BalanceMutationResult.Fail("claim_lost");
                        }
                    }

                    // Per-event dedup, in the SAME transaction as the credit: insert first so a
                    // duplicate-key rolls the whole thing back (no double credit), and a crash
                    // after this point cannot leave the event deduped-but-uncredited.
                    if (dedupKey != null)
                    {
                        try
                        {
                            ctx.Database.ExecuteSqlCommand(
                                "INSERT INTO dbo.tblH_Partner_Webhook_ID (Type, TXID, Request, RequestDate) " +
                                "VALUES (@dtype, @dkey, @dreq, @now)",
                                P("@dtype", dedupType), P("@dkey", dedupKey),
                                P("@dreq", dedupRequest), P("@now", now));
                        }
                        catch (Exception dupEx) when (IsDuplicateKey(dupEx))
                        {
                            tx.Rollback();
                            return BalanceMutationResult.Fail("duplicate_event");
                        }
                    }

                    var updateRows = ctx.Database.SqlQuery<BalanceDelta>(updateSql,
                        P("@uid", userId),
                        P("@cur", CurrencyUSDT),
                        P("@delta", balanceDelta),
                        P("@minBalance", minBalance),
                        P("@now", now)).ToList();

                    if (updateRows.Count != 1)
                    {
                        tx.Rollback();
                        return BalanceMutationResult.Fail(noRowReason);
                    }

                    BalanceDelta d = updateRows[0];

                    long ledgerId = ctx.Database.SqlQuery<long>(insertSql,
                        P("@uid", userId),
                        P("@balId", d.BalanceID),
                        P("@txid", transactionId),
                        P("@type", type),
                        P("@prev", d.Prev),
                        P("@amt", ledgerAmount),
                        P("@comm", ledgerCommission),
                        P("@commpct", ledgerCommissionPct),
                        P("@curr", d.Curr),
                        P("@now", now),
                        P("@status", status)).Single();

                    tx.Commit();

                    return new BalanceMutationResult
                    {
                        Success = true,
                        BalancePrevious = d.Prev,
                        BalanceNew = d.Curr,
                        LedgerId = ledgerId
                    };
                }
                catch
                {
                    tx.Rollback();
                    throw;
                }
            }
        }

        // ---- Provisioning (idempotent, create-if-missing only) -------------------

        /// <summary>
        /// Ensure the user's USDT wallet row exists; returns it. Idempotent and race-safe:
        /// relies on the unique index (UserID, Currency) so a concurrent loser re-reads the
        /// winner's row rather than inserting a duplicate. Never overwrites an existing row.
        /// </summary>
        public static tblM_User_Balance EnsureWallet(string userId)
        {
            using (var ctx = new DBEntities())
            {
                var bal = ctx.tblM_User_Balance
                    .FirstOrDefault(p => p.UserID == userId && p.Currency == CurrencyUSDT);
                if (bal != null) return bal;

                bal = new tblM_User_Balance
                {
                    UserID = userId,
                    Currency = CurrencyUSDT,
                    BalanceID = Guid.NewGuid().ToString(),
                    Balance = 0m,
                    isActive = 1,
                    DateCreated = DateTime.Now,
                    CreatedBy = "system"
                };
                ctx.tblM_User_Balance.Add(bal);
                try
                {
                    ctx.SaveChanges();
                    return bal;
                }
                catch (DbUpdateException ex) when (IsDuplicateKey(ex))
                {
                    // Lost the create race — the winning row is now committed; re-read it.
                    using (var re = new DBEntities())
                        return re.tblM_User_Balance
                            .FirstOrDefault(p => p.UserID == userId && p.Currency == CurrencyUSDT);
                }
            }
        }

        /// <summary>
        /// Ensure the user's active TRC20 static deposit address exists; returns it.
        /// Create-if-missing only (the address is immutable post-creation, T6.5) and
        /// race-safe via the unique index (UserID, NetworkID). In prod this calls the
        /// Runegate gateway; in dev it mints a synthetic T-address (mirrors registration).
        /// Returns null if the gateway is unavailable so the caller can degrade gracefully
        /// rather than throw — provisioning is decoupled from any user-facing success path.
        /// </summary>
        public static tblM_User_Crypto_Deposit EnsureDepositAddress(string userId)
        {
            using (var ctx = new DBEntities())
            {
                var existing = ctx.tblM_User_Crypto_Deposit.FirstOrDefault(
                    p => p.UserID == userId && p.NetworkID == TronNetworkId && p.isActive == 1);
                if (existing != null) return existing;

                tblM_User_Crypto_Deposit addr = new tblM_User_Crypto_Deposit
                {
                    ID = Guid.NewGuid().ToString(),
                    UserID = userId,
                    NetworkID = TronNetworkId,
                    DateCreated = DateTime.Now,
                    CreatedBy = "system",
                    isActive = 1
                };

                if (KeyModel.QRYPTO_ENVIRONMENT == "prod")
                {
                    var coins = PGCryptoService.getCoin();
                    if (coins == null) return null; // gateway unavailable — try again next access
                    var coinId = coins.Where(p => p.Network == "TRC20")
                                      .Select(p => p.CoinID).FirstOrDefault();
                    var sta = PGCryptoService.addressStaticCreation(
                        new AddressStaticModel { CoinID = coinId });
                    if (sta == null || string.IsNullOrEmpty(sta.Address)) return null;

                    addr.PGCryptoID = sta.AddressID;
                    addr.Address = sta.Address;
                    addr.Param1 = JsonConvert.SerializeObject(sta);
                }
                else
                {
                    addr.Address = "T" + Common.RandomString(12);
                }

                ctx.tblM_User_Crypto_Deposit.Add(addr);
                try
                {
                    ctx.SaveChanges();
                    return addr;
                }
                catch (DbUpdateException ex) when (IsDuplicateKey(ex))
                {
                    using (var re = new DBEntities())
                        return re.tblM_User_Crypto_Deposit.FirstOrDefault(
                            p => p.UserID == userId && p.NetworkID == TronNetworkId && p.isActive == 1);
                }
            }
        }

        // ---- Read accessors (replace the scattered ad-hoc queries) ---------------

        /// <summary>Active USDT balance for a user, or null if no wallet row exists yet.</summary>
        public static tblM_User_Balance GetBalance(string userId)
        {
            using (var ctx = new DBEntities())
                return ctx.tblM_User_Balance
                    .FirstOrDefault(p => p.UserID == userId && p.Currency == CurrencyUSDT);
        }

        /// <summary>Active TRC20 deposit address for a user, or null if not provisioned.</summary>
        public static tblM_User_Crypto_Deposit GetDepositAddress(string userId)
        {
            using (var ctx = new DBEntities())
                return ctx.tblM_User_Crypto_Deposit.FirstOrDefault(
                    p => p.UserID == userId && p.NetworkID == TronNetworkId && p.isActive == 1);
        }

        // ---- Helpers -------------------------------------------------------------

        private static SqlParameter P(string name, object value)
        {
            return new SqlParameter(name, value ?? DBNull.Value);
        }

        /// <summary>
        /// True if a DbUpdateException was caused by a SQL Server unique/primary-key
        /// violation (2627 duplicate key, 2601 duplicate index row) — the DB-layer dedup
        /// signal the house pattern swallows as a no-op.
        /// </summary>
        public static bool IsDuplicateKey(Exception ex)
        {
            for (Exception e = ex; e != null; e = e.InnerException)
            {
                SqlException sql = e as SqlException;
                if (sql != null)
                {
                    foreach (SqlError err in sql.Errors)
                        if (err.Number == 2627 || err.Number == 2601) return true;
                }
            }
            return false;
        }
    }
}
