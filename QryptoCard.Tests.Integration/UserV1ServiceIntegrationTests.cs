using System;
using System.Linq;
using QryptoCard.INT;
using QryptoCard.INT.Script.Service.App.v1;
using QryptoCard.Tests.Fixtures.LocalDb;
using Xunit;

namespace QryptoCard.Tests.Integration
{
    // End-to-end harness proof: seed via EF against a real throwaway LocalDB, drive a real INT
    // WCF service method (instantiated directly), and assert the database effect. This is the
    // template the auth-token port builds on — IClassFixture<LocalDbFixture> for the seeded DB +
    // direct `new <Service>()` against it.
    public class UserV1ServiceIntegrationTests : IClassFixture<LocalDbFixture>
    {
        readonly LocalDbFixture _db;
        public UserV1ServiceIntegrationTests(LocalDbFixture db) { _db = db; }

        [Fact]
        public void RegisterVerify_ValidPendingCode_VerifiesUserAndCreatesBalance()
        {
            // Seed a pending registration OTP for the baseline user. The service stores the OTP
            // hash (OtpCodes.Hash); the plaintext is what the caller presents.
            string otpId = Guid.NewGuid().ToString();
            const string plainCode = "123456";
            using (var ctx = _db.NewContext())
            {
                // Start from the unverified state RegisterVerify is meant to transition out of, so
                // the assertion proves the method flipped the flag (not the seed).
                var u = ctx.tblM_User.Single(p => p.UserID == LocalDbFixture.Ids.UserA);
                u.isVerified = 0;
                u.DateVerified = null;

                ctx.tblH_User_Register.Add(new tblH_User_Register
                {
                    ID = otpId,
                    UserID = LocalDbFixture.Ids.UserA,
                    Code = QryptoCard.Sec.OtpCodes.Hash(plainCode),
                    DateCreated = DateTime.UtcNow,
                    DateExpired = DateTime.UtcNow.AddMinutes(15),
                    isVerify = 0
                });
                ctx.SaveChanges();
            }

            // Drive the real WCF service. Its field-initialised `new DBEntities()` reads
            // name=DBEntities from App.config — pointed at the same TestQryptoCard LocalDB.
            var sut = new UserV1Service();
            var op = sut.RegisterVerify(new tblH_User_Register { ID = otpId, Code = plainCode });

            Assert.Equal("success", op.Status);

            // DB effects: the OTP row is marked verified, the user is now verified, and the
            // registration side-effect (USDT balance row) was created.
            using (var ctx = _db.NewContext())
            {
                var otpRow = ctx.tblH_User_Register.Single(p => p.ID == otpId);
                Assert.Equal(1, otpRow.isVerify);

                var user = ctx.tblM_User.Single(p => p.UserID == LocalDbFixture.Ids.UserA);
                Assert.Equal(1, user.isVerified);
                Assert.NotNull(user.DateVerified);

                var balance = ctx.tblM_User_Balance
                    .Where(p => p.UserID == LocalDbFixture.Ids.UserA)
                    .OrderByDescending(p => p.DateCreated)
                    .FirstOrDefault();
                Assert.NotNull(balance);
                Assert.Equal("USDT", balance.Currency);
            }
        }
    }
}
