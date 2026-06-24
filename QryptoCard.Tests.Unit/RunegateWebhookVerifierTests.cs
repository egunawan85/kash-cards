using QryptoCard.Sec;
using QryptoCard.Tests.Fixtures;
using Xunit;

namespace QryptoCard.Tests.Unit
{
    public class RunegateWebhookVerifierTests
    {
        // >= 32 bytes (256-bit minimum the verifier enforces).
        private const string Secret = "runegate-callback-signing-key-0123456789";
        private const string WrongSecret = "another-callback-signing-key-9876543210ab";
        private const long Ts = 1700000000;
        private static readonly byte[] Body = CryptoFixtures.Utf8("{\"id\":\"dep_1\",\"amount\":\"100.00\"}");

        [Fact]
        public void ValidSignature_Passes()
        {
            var header = CryptoFixtures.RunegateHeader(Secret, Ts, Body);
            Assert.True(RunegateWebhookVerifier.Verify(header, Body, Secret, Ts, 300));
        }

        [Fact]
        public void TamperedBody_Fails()
        {
            var header = CryptoFixtures.RunegateHeader(Secret, Ts, Body);
            var tampered = CryptoFixtures.Utf8("{\"id\":\"dep_1\",\"amount\":\"9999.00\"}");
            Assert.False(RunegateWebhookVerifier.Verify(header, tampered, Secret, Ts, 300));
        }

        [Fact]
        public void StaleTimestamp_Fails()
        {
            var header = CryptoFixtures.RunegateHeader(Secret, Ts, Body);
            // 'now' is 1000s past the signed ts, outside the 300s window.
            Assert.False(RunegateWebhookVerifier.Verify(header, Body, Secret, Ts + 1000, 300));
        }

        [Fact]
        public void WrongSecret_Fails()
        {
            var header = CryptoFixtures.RunegateHeader(Secret, Ts, Body);
            Assert.False(RunegateWebhookVerifier.Verify(header, Body, WrongSecret, Ts, 300));
        }

        [Fact]
        public void WeakSecret_Fails()
        {
            // A signature minted with a too-short secret must be rejected (key-strength floor).
            const string weak = "short";
            var header = CryptoFixtures.RunegateHeader(weak, Ts, Body);
            Assert.False(RunegateWebhookVerifier.Verify(header, Body, weak, Ts, 300));
        }

        [Theory]
        [InlineData("garbage")]
        [InlineData("t=1700000000")]              // missing v1
        [InlineData("v1=deadbeef")]               // missing t
        [InlineData("t=1700000000,v1=nothex!!")]  // non-hex digest
        [InlineData("")]
        public void MalformedHeader_Fails(string header)
        {
            Assert.False(RunegateWebhookVerifier.Verify(header, Body, Secret, Ts, 300));
        }
    }
}
