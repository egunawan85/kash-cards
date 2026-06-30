using QryptoCard.Sec;
using QryptoCard.Tests.Fixtures;
using Xunit;

namespace QryptoCard.Tests.Unit
{
    public class WasabiSignatureVerifierTests
    {
        private static readonly byte[] Body =
            CryptoFixtures.Utf8("{\"event\":\"card.funded\",\"cardNo\":\"4111\",\"amount\":\"50.00\"}");

        [Fact]
        public void ValidSignature_Passes()
        {
            var pair = CryptoFixtures.NewRsaPair();
            var sig = CryptoFixtures.WasabiSign(pair.PrivateKey, Body);
            Assert.True(WasabiSignatureVerifier.Verify(sig, Body, pair.PublicKeySpkiBase64));
        }

        [Fact]
        public void Wasabi1024BitKey_Passes()
        {
            // WasabiCard's platform signing key is 1024-bit; the verifier must accept it (a 2048 floor
            // rejected their real key and 401'd every webhook).
            var pair = CryptoFixtures.NewRsaPair(1024);
            var sig = CryptoFixtures.WasabiSign(pair.PrivateKey, Body);
            Assert.True(WasabiSignatureVerifier.Verify(sig, Body, pair.PublicKeySpkiBase64));
        }

        [Fact]
        public void KeyBelowFloor_Fails()
        {
            // A truly-weak (512-bit) substituted key is still rejected by the 1024-bit floor.
            var pair = CryptoFixtures.NewRsaPair(512);
            var sig = CryptoFixtures.WasabiSign(pair.PrivateKey, Body);
            Assert.False(WasabiSignatureVerifier.Verify(sig, Body, pair.PublicKeySpkiBase64));
        }

        [Fact]
        public void TamperedBody_Fails()
        {
            var pair = CryptoFixtures.NewRsaPair();
            var sig = CryptoFixtures.WasabiSign(pair.PrivateKey, Body);
            var tampered = CryptoFixtures.Utf8("{\"event\":\"card.funded\",\"cardNo\":\"4111\",\"amount\":\"5000.00\"}");
            Assert.False(WasabiSignatureVerifier.Verify(sig, tampered, pair.PublicKeySpkiBase64));
        }

        [Fact]
        public void WrongKey_Fails()
        {
            var signer = CryptoFixtures.NewRsaPair();
            var attacker = CryptoFixtures.NewRsaPair();
            var sig = CryptoFixtures.WasabiSign(signer.PrivateKey, Body);
            // Verified against a different platform key -> must fail.
            Assert.False(WasabiSignatureVerifier.Verify(sig, Body, attacker.PublicKeySpkiBase64));
        }

        [Theory]
        [InlineData("not-base64-$$$")]
        [InlineData("")]
        [InlineData(null)]
        public void MalformedSignature_Fails(string sig)
        {
            var pair = CryptoFixtures.NewRsaPair();
            Assert.False(WasabiSignatureVerifier.Verify(sig, Body, pair.PublicKeySpkiBase64));
        }

        [Fact]
        public void MalformedPublicKey_Fails()
        {
            var pair = CryptoFixtures.NewRsaPair();
            var sig = CryptoFixtures.WasabiSign(pair.PrivateKey, Body);
            Assert.False(WasabiSignatureVerifier.Verify(sig, Body, "not-a-real-spki-key"));
        }
    }
}
