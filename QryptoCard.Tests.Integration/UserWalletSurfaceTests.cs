using System.Linq;
using Newtonsoft.Json.Linq;
using QryptoCard.INT.Script.Service;
using QryptoCard.INT.Script.Service.App.v1;
using QryptoCard.Tests.Fixtures.LocalDb;
using Xunit;

namespace QryptoCard.Tests.Integration
{
    // The user-facing wallet read surfaces: deposit address and the paginated ledger. Asserts the
    // happy path plus the IDOR property (a caller only ever sees their own rows) and that internal
    // identifiers are not leaked.
    public class UserWalletSurfaceTests : IClassFixture<LocalDbFixture>
    {
        readonly LocalDbFixture _db;
        public UserWalletSurfaceTests(LocalDbFixture db) { _db = db; }

        [Fact]
        public void GetDepositAddress_ProvisionsAndReturnsOwnAddress()
        {
            var sut = new UserV1Service();
            var op = sut.getDepositAddress(LocalDbFixture.Ids.EmailA);

            Assert.Equal("success", op.Status);
            var json = JObject.Parse(op.Data.ToString());
            Assert.False(string.IsNullOrEmpty((string)json["Address"]));
            Assert.Equal("USDT", (string)json["Coin"]);
        }

        [Fact]
        public void GetLedger_ReturnsOnlyOwnRows_PaginatedWithoutInternalIds()
        {
            // Seed two ledger rows for the authenticated user and one for a different user.
            WalletService.EnsureWallet(LocalDbFixture.Ids.UserA);
            WalletService.Credit(LocalDbFixture.Ids.UserA, 100m, 0m, 0d, WalletService.TypeCryptoDeposit, "surf-a-1");
            WalletService.Credit(LocalDbFixture.Ids.UserA, 50m, 0m, 0d, WalletService.TypeCryptoDeposit, "surf-a-2");
            WalletService.EnsureWallet("surf-other-user");
            WalletService.Credit("surf-other-user", 999m, 0m, 0d, WalletService.TypeCryptoDeposit, "surf-other-1");

            var sut = new UserV1Service();
            var op = sut.getLedger(LocalDbFixture.Ids.EmailA, 1, 20);

            Assert.Equal("success", op.Status);
            var data = op.Data.ToString();
            var json = JObject.Parse(data);
            Assert.Equal(2, (int)json["Total"]);                       // only the caller's rows
            Assert.Equal(2, ((JArray)json["Items"]).Count);
            Assert.DoesNotContain("surf-other", data);                 // no other user's data leaks
            Assert.DoesNotContain("BalanceID", data);                 // internal id not exposed
        }
    }
}
