using System;
using System.Data.Entity.Infrastructure;
using System.Linq;
using QryptoCard.INT;
using QryptoCard.INT.Script.Service;
using QryptoCard.Tests.Fixtures.LocalDb;
using Xunit;

namespace QryptoCard.Tests.Integration
{
    // Deposit-address invariants that protect the credit path: the address is created once and
    // never overwritten (so it can't be repointed), and an active address maps to exactly one
    // user (so an inbound deposit can't be credited to the wrong account).
    public class WalletAddressTests : IClassFixture<LocalDbFixture>
    {
        readonly LocalDbFixture _db;
        public WalletAddressTests(LocalDbFixture db) { _db = db; }

        static string Fresh(string t) => "addr-" + t + "-" + Guid.NewGuid().ToString("N").Substring(0, 8);

        [Fact]
        public void EnsureDepositAddress_IsIdempotent_NeverOverwrites()
        {
            var uid = Fresh("idem");
            var a = WalletService.EnsureDepositAddress(uid);
            var b = WalletService.EnsureDepositAddress(uid);

            Assert.NotNull(a);
            Assert.NotNull(b);
            Assert.Equal(a.Address, b.Address); // create-if-missing: same address, never repointed
            using (var ctx = _db.NewContext())
                Assert.Equal(1, ctx.tblM_User_Crypto_Deposit.Count(
                    p => p.UserID == uid && p.NetworkID == WalletService.TronNetworkId && p.isActive == 1));
        }

        [Fact]
        public void DepositAddress_DuplicateActiveAddress_RejectedByUniqueIndex()
        {
            string sharedAddr = "TDUP" + Guid.NewGuid().ToString("N").Substring(0, 20);
            var u1 = Fresh("u1");
            var u2 = Fresh("u2");

            using (var ctx = _db.NewContext())
            {
                ctx.tblM_User_Crypto_Deposit.Add(new tblM_User_Crypto_Deposit
                {
                    ID = Guid.NewGuid().ToString(),
                    UserID = u1,
                    NetworkID = WalletService.TronNetworkId,
                    Address = sharedAddr,
                    isActive = 1,
                    DateCreated = DateTime.Now
                });
                ctx.SaveChanges();
            }

            // A second active row with the SAME address must be rejected — otherwise a deposit to
            // that address could be credited to the wrong user.
            Assert.Throws<DbUpdateException>(() =>
            {
                using (var ctx = _db.NewContext())
                {
                    ctx.tblM_User_Crypto_Deposit.Add(new tblM_User_Crypto_Deposit
                    {
                        ID = Guid.NewGuid().ToString(),
                        UserID = u2,
                        NetworkID = WalletService.TronNetworkId,
                        Address = sharedAddr,
                        isActive = 1,
                        DateCreated = DateTime.Now
                    });
                    ctx.SaveChanges();
                }
            });
        }
    }
}
