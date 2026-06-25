using System;
using System.Linq;
using QryptoCard.INT.Script.Service;
using QryptoCard.Tests.Fixtures.LocalDb;
using Xunit;

namespace QryptoCard.Tests.Integration
{
    // Pure-logic guards that fail closed BEFORE any database access — no fixture, always run.
    public class WalletServiceGuardTests
    {
        [Fact]
        public void Credit_NegativeAmount_FailsClosed()
        {
            var r = WalletService.Credit("anyone", -1m, 0m, 0d, WalletService.TypeCryptoDeposit, "tx-neg-c");
            Assert.False(r.Success);
            Assert.Equal("negative_credit", r.FailureReason);
        }

        [Fact]
        public void Debit_NegativeAmount_FailsClosed()
        {
            var r = WalletService.Debit("anyone", -1m, WalletService.TypeCardOpen, "tx-neg-d");
            Assert.False(r.Success);
            Assert.Equal("negative_debit", r.FailureReason);
        }
    }

    // DB-backed coverage for the atomic credit/debit core and idempotent provisioning.
    // Each test uses a fresh UserID so the shared per-class fixture DB stays non-interfering.
    public class WalletServiceIntegrationTests : IClassFixture<LocalDbFixture>
    {
        readonly LocalDbFixture _db;
        public WalletServiceIntegrationTests(LocalDbFixture db) { _db = db; }

        static string FreshUser(string tag) =>
            "wsit-" + tag + "-" + Guid.NewGuid().ToString("N").Substring(0, 8);

        decimal BalanceOf(string uid)
        {
            using (var ctx = _db.NewContext())
                return ctx.tblM_User_Balance
                    .Where(p => p.UserID == uid && p.Currency == "USDT")
                    .Select(p => p.Balance).FirstOrDefault() ?? 0m;
        }

        [Fact]
        public void EnsureWallet_IsIdempotent_NoDuplicateRow()
        {
            var uid = FreshUser("ensure");
            Assert.NotNull(WalletService.EnsureWallet(uid));
            Assert.NotNull(WalletService.EnsureWallet(uid));
            using (var ctx = _db.NewContext())
                Assert.Equal(1, ctx.tblM_User_Balance.Count(p => p.UserID == uid && p.Currency == "USDT"));
        }

        [Fact]
        public void Credit_IncrementsBalance_AndLedgerSatisfiesTamperInvariant()
        {
            var uid = FreshUser("credit");
            WalletService.EnsureWallet(uid);

            var r = WalletService.Credit(uid, 100m, 0m, 0d, WalletService.TypeCryptoDeposit, "tx-credit-1");

            Assert.True(r.Success);
            Assert.Equal(0m, r.BalancePrevious);
            Assert.Equal(100m, r.BalanceNew);
            Assert.Equal(100m, BalanceOf(uid));

            using (var ctx = _db.NewContext())
            {
                var row = ctx.tblH_User_Balance.Single(p => p.TransactionID == "tx-credit-1");
                Assert.Equal(100m, row.Amount.Value);
                Assert.Equal(0m, row.BalancePrevious.Value);
                Assert.Equal(100m, row.Balance.Value);
                Assert.Equal(WalletService.TypeCryptoDeposit, row.Type);
                // Canonical forensic tamper-check (Plan 3 tmp/forensics.sql).
                Assert.Equal(row.Balance.Value, row.BalancePrevious.Value + row.Amount.Value);
            }
        }

        [Fact]
        public void Credit_WithCommission_StoresNetAsAmount_GrossRecoverable()
        {
            var uid = FreshUser("comm");
            WalletService.EnsureWallet(uid);

            // Gross 100, commission 3 -> net 97 credited.
            var r = WalletService.Credit(uid, 97m, 3m, 3d, WalletService.TypeCryptoDeposit, "tx-comm-1");

            Assert.True(r.Success);
            Assert.Equal(97m, BalanceOf(uid));

            using (var ctx = _db.NewContext())
            {
                var row = ctx.tblH_User_Balance.Single(p => p.TransactionID == "tx-comm-1");
                Assert.Equal(97m, row.Amount.Value);        // net delta, not gross
                Assert.Equal(3m, row.Commision.Value);      // commission recorded separately
                Assert.Equal(row.Balance.Value, row.BalancePrevious.Value + row.Amount.Value); // invariant holds
                Assert.Equal(100m, row.Amount.Value + row.Commision.Value);                     // gross recoverable
            }
        }

        [Fact]
        public void Debit_SufficientBalance_DecrementsAndLogsNegativeAmount()
        {
            var uid = FreshUser("debit");
            WalletService.EnsureWallet(uid);
            WalletService.Credit(uid, 100m, 0m, 0d, WalletService.TypeCryptoDeposit, "tx-d-credit");

            var r = WalletService.Debit(uid, 30m, WalletService.TypeCardOpen, "tx-d-debit");

            Assert.True(r.Success);
            Assert.Equal(100m, r.BalancePrevious);
            Assert.Equal(70m, r.BalanceNew);
            Assert.Equal(70m, BalanceOf(uid));

            using (var ctx = _db.NewContext())
            {
                var row = ctx.tblH_User_Balance.Single(p => p.TransactionID == "tx-d-debit");
                Assert.Equal(-30m, row.Amount.Value);
                Assert.Equal(row.Balance.Value, row.BalancePrevious.Value + row.Amount.Value);
            }
        }

        [Fact]
        public void Debit_InsufficientBalance_FailsClosed_NoMutation()
        {
            var uid = FreshUser("insuff");
            WalletService.EnsureWallet(uid);
            WalletService.Credit(uid, 20m, 0m, 0d, WalletService.TypeCryptoDeposit, "tx-i-credit");

            var r = WalletService.Debit(uid, 50m, WalletService.TypeCardOpen, "tx-i-debit");

            Assert.False(r.Success);
            Assert.Equal("insufficient_balance", r.FailureReason);
            Assert.Equal(20m, BalanceOf(uid)); // unchanged

            using (var ctx = _db.NewContext())
                Assert.False(ctx.tblH_User_Balance.Any(p => p.TransactionID == "tx-i-debit")); // no ledger row
        }

        [Fact]
        public void Debit_ToExactZero_Succeeds()
        {
            var uid = FreshUser("zero");
            WalletService.EnsureWallet(uid);
            WalletService.Credit(uid, 50m, 0m, 0d, WalletService.TypeCryptoDeposit, "tx-z-credit");

            var r = WalletService.Debit(uid, 50m, WalletService.TypeCardOpen, "tx-z-debit");

            Assert.True(r.Success);
            Assert.Equal(0m, r.BalanceNew);
            Assert.Equal(0m, BalanceOf(uid));
        }

        [Fact]
        public void Credit_MissingWallet_FailsClosed()
        {
            var uid = FreshUser("nowallet"); // deliberately no EnsureWallet
            var r = WalletService.Credit(uid, 10m, 0m, 0d, WalletService.TypeCryptoDeposit, "tx-nw");
            Assert.False(r.Success);
            Assert.Equal("wallet_missing", r.FailureReason);
        }

        [Fact]
        public void Ledger_Reconciles_SumOfAmounts_EqualsBalance()
        {
            var uid = FreshUser("recon");
            WalletService.EnsureWallet(uid);
            WalletService.Credit(uid, 100m, 0m, 0d, WalletService.TypeCryptoDeposit, "tx-r1");
            WalletService.Credit(uid, 50m, 0m, 0d, WalletService.TypeCryptoDeposit, "tx-r2");
            WalletService.Debit(uid, 30m, WalletService.TypeCardOpen, "tx-r3");
            WalletService.Debit(uid, 20m, WalletService.TypeCardTopup, "tx-r4");

            using (var ctx = _db.NewContext())
            {
                var rows = ctx.tblH_User_Balance.Where(p => p.UserID == uid).ToList();
                decimal sum = rows.Sum(p => p.Amount ?? 0m);
                Assert.Equal(100m, sum);              // 100 + 50 - 30 - 20
                Assert.Equal(sum, BalanceOf(uid));    // running cache == ledger sum
                foreach (var row in rows)
                    Assert.Equal(row.Balance.Value, row.BalancePrevious.Value + row.Amount.Value);
            }
        }
    }
}
