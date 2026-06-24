using System;
using System.Linq;
using QryptoCard.Sec;
using Xunit;

namespace QryptoCard.Tests.Unit
{
    public class OtpCodesTests
    {
        [Fact]
        public void Generate_IsSixNumericDigits()
        {
            for (int i = 0; i < 50; i++)
            {
                var code = OtpCodes.Generate(6);
                Assert.Equal(6, code.Length);
                Assert.True(code.All(char.IsDigit), "code had non-digits: " + code);
            }
        }

        [Fact]
        public void Generate_VariesAcrossCalls()
        {
            var codes = Enumerable.Range(0, 100).Select(_ => OtpCodes.Generate(6)).Distinct().Count();
            Assert.True(codes > 90, "expected mostly-distinct codes, got " + codes + "/100 distinct");
        }

        [Fact]
        public void Hash_IsDeterministic_AndNotThePlaintext()
        {
            var h1 = OtpCodes.Hash("123456");
            var h2 = OtpCodes.Hash("123456");
            Assert.Equal(h1, h2);
            Assert.NotEqual("123456", h1);
            Assert.True(h1.Length <= 50, "hash must fit the legacy nvarchar(50) column; was " + h1.Length);
        }

        [Fact]
        public void Verify_AcceptsCorrect_RejectsWrong()
        {
            var code = OtpCodes.Generate(6);
            var stored = OtpCodes.Hash(code);
            Assert.True(OtpCodes.Verify(code, stored));
            Assert.False(OtpCodes.Verify("000000", stored)); // the old hardcoded value is no longer special
            Assert.False(OtpCodes.Verify(code, OtpCodes.Hash("999999")));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public void Verify_NullOrEmpty_IsRejected(string provided)
        {
            Assert.False(OtpCodes.Verify(provided, OtpCodes.Hash("123456")));
            Assert.False(OtpCodes.Verify("123456", provided));
        }

        [Fact]
        public void IsExpired_FailsClosed()
        {
            var now = new DateTime(2026, 1, 1, 12, 0, 0);
            Assert.True(OtpCodes.IsExpired(now.AddMinutes(-1), now)); // past -> expired
            Assert.False(OtpCodes.IsExpired(now.AddMinutes(5), now)); // future -> valid
            Assert.True(OtpCodes.IsExpired(null, now));               // no expiry -> treated as expired
        }
    }
}
