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
            string userId, decimal netAmount, decimal grossAmount, decimal commission,
            double commissionInPercentage, string type, string transactionId, string status = null)
        {
            if (netAmount < 0m) return BalanceMutationResult.Fail("negative_credit");
            return Mutate(userId, netAmount, false, 0m, grossAmount, commission,
                commissionInPercentage, type, transactionId, status, "wallet_missing");
        }

        public static BalanceMutationResult Debit(
            string userId, decimal amount, string type, string transactionId, string status = null)
        {
            if (amount < 0m) return BalanceMutationResult.Fail("negative_debit");
            return Mutate(userId, -amount, true, amount, -amount, 0m, 0d,
                type, transactionId, status, "insufficient_balance");
        }

        private static BalanceMutationResult Mutate(
            string userId, decimal balanceDelta, bool requireMinBalance, decimal minBalance,
            decimal ledgerAmount, decimal ledgerCommission, double ledgerCommissionPct,
            string type, string transactionId, string status, string noRowReason)
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

        public static bool IsDuplicateKey(DbUpdateException ex)
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
