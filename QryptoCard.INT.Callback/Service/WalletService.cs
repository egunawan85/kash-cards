using System;
using System.Data;
using System.Data.Entity.Infrastructure;
using System.Data.SqlClient;
using System.Linq;

namespace QryptoCard.INT.Callback.Service
{
    /// <summary>
    /// Callback-tier copy of the prepaid-balance mutation helper. Self-contained per
    /// assembly, like the WasabiCardService gateway copy. The canonical/fuller version
    /// (with deposit-address provisioning, which depends on tier-specific gateways) lives
    /// in QryptoCard.INT\Script\Service\WalletService.cs; this copy carries only what the
    /// callback paths need: the atomic Credit/Debit core, EnsureWallet, and read access.
    ///
    /// Keep the mutation core byte-for-byte aligned with the INT copy — both are the ONLY
    /// sanctioned way to move a prepaid balance: a conditional UPDATE that captures
    /// before/after via OUTPUT and writes the tblH_User_Balance ledger row in one
    /// Serializable transaction, with no read-modify-write anywhere.
    /// </summary>
    public static class WalletService
    {
        public const string CurrencyUSDT = "USDT";

        public const string TypeCryptoDeposit = "Crypto Deposit";
        public const string TypeCardOpen = "Card Open";
        public const string TypeCardTopup = "Card Topup";
        public const string TypeCardOpenReversal = "Card Open Reversal";
        public const string TypeCardTopupReversal = "Card Topup Reversal";
        public const string TypeDepositRefund = "Deposit Refund";

        public class BalanceMutationResult
        {
            public bool Success { get; set; }
            public string FailureReason { get; set; }
            public decimal BalancePrevious { get; set; }
            public decimal BalanceNew { get; set; }
            public long LedgerId { get; set; }

            public static BalanceMutationResult Fail(string reason)
            {
                return new BalanceMutationResult { Success = false, FailureReason = reason };
            }
        }

        private class BalanceDelta
        {
            public decimal Prev { get; set; }
            public decimal Curr { get; set; }
            public string BalanceID { get; set; }
        }

        public static BalanceMutationResult Credit(
            string userId, decimal netAmount, decimal commission,
            double commissionInPercentage, string type, string transactionId, string status = null)
        {
            if (netAmount < 0m) return BalanceMutationResult.Fail("negative_credit");
            // Amount stores the NET balance delta (not gross) so every ledger row satisfies
            // BalancePrevious + Amount = Balance; gross is recoverable as Amount + Commision.
            return Mutate(userId, netAmount, false, 0m, netAmount, commission,
                commissionInPercentage, type, transactionId, status, "wallet_missing");
        }

        public static BalanceMutationResult Debit(
            string userId, decimal amount, string type, string transactionId, string status = null)
        {
            if (amount < 0m) return BalanceMutationResult.Fail("negative_debit");
            return Mutate(userId, -amount, true, amount, -amount, 0m, 0d,
                type, transactionId, status, "insufficient_balance");
        }

        /// <summary>
        /// Credit a confirmed inbound deposit, deduped per-event in the SAME transaction as
        /// the credit. The dedup row (tblH_Partner_Webhook_ID, keyed on TransactionID|Status,
        /// Type='PGCrypto') is inserted first; a duplicate-key means this exact event already
        /// credited, so the whole transaction rolls back and returns "duplicate_event" — no
        /// double credit. Because the dedup insert and the balance mutation commit or roll
        /// back together, a crash between them can never leave a deposit deduped-but-uncredited
        /// (the F-0031 loss-of-funds lesson). Credits the net amount; records commission.
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
            return Mutate(userId, netAmount, false, 0m, netAmount, commission,
                commissionInPercentage, TypeCryptoDeposit, transactionId, status, "wallet_missing",
                dedupType: "PGCrypto", dedupKey: dedupKey, dedupRequest: dedupRequest);
        }

        /// <summary>
        /// Refund a failed card deposit back to the wallet, atomically with the order's
        /// InProgress/PendingProvider -> Failed transition. The status claim and the credit commit or
        /// roll back together, so a crash between them can never leave a deposit Failed-but-unrefunded
        /// (the loss-of-funds gap the red-team caught). The claim is also the idempotency gate: a
        /// replay finds 0 rows to claim and is a no-op.
        /// </summary>
        public static BalanceMutationResult CreditRefund(
            string userId, decimal netAmount, decimal commission, double commissionInPercentage,
            string depositOrderId)
        {
            if (netAmount < 0m) return BalanceMutationResult.Fail("negative_credit");
            string claimSql =
                "UPDATE dbo.tblT_Card_Deposit SET Status = 'failed' " +
                "WHERE ID = @cid AND Status IN ('in progress', 'pending provider')";
            return Mutate(userId, netAmount, false, 0m, netAmount, commission,
                commissionInPercentage, TypeDepositRefund, depositOrderId, null, "wallet_missing",
                claimSql: claimSql, claimParams: new[] { new SqlParameter("@cid", depositOrderId) });
        }

        private static BalanceMutationResult Mutate(
            string userId, decimal balanceDelta, bool requireMinBalance, decimal minBalance,
            decimal ledgerAmount, decimal ledgerCommission, double ledgerCommissionPct,
            string type, string transactionId, string status, string noRowReason,
            string dedupType = null, string dedupKey = null, string dedupRequest = null,
            string claimSql = null, SqlParameter[] claimParams = null)
        {
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
                    // Optional claim, in the SAME transaction as the credit: a conditional UPDATE that
                    // must affect exactly one row (e.g. an order InProgress->Failed transition). It is
                    // both the idempotency gate and an atomicity guarantee — a crash after the claim but
                    // before the credit rolls BOTH back, so the credit is never lost, and a replay finds
                    // 0 rows and is a no-op.
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

        /// <summary>
        /// Ensure the user's USDT wallet row exists; returns it. Idempotent and race-safe
        /// via the unique index (UserID, Currency).
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
                    using (var re = new DBEntities())
                        return re.tblM_User_Balance
                            .FirstOrDefault(p => p.UserID == userId && p.Currency == CurrencyUSDT);
                }
            }
        }

        public static tblM_User_Balance GetBalance(string userId)
        {
            using (var ctx = new DBEntities())
                return ctx.tblM_User_Balance
                    .FirstOrDefault(p => p.UserID == userId && p.Currency == CurrencyUSDT);
        }

        private static SqlParameter P(string name, object value)
        {
            return new SqlParameter(name, value ?? DBNull.Value);
        }

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
