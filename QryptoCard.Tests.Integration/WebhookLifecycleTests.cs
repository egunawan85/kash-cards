using System.Text;
using Newtonsoft.Json.Linq;
using QryptoCard.Sec;
using QryptoCard.Tests.Fixtures;
using Xunit;

namespace QryptoCard.Tests.Integration
{
    /// <summary>
    /// Cross-component "trust the webhook" path, minus the DB: a provider signs a payload, our
    /// verifier accepts it, and the raw body deserializes to the fields the callback handler
    /// relies on. Forged/stale/foreign-key variants are rejected before any parse.
    /// </summary>
    public class WebhookLifecycleTests
    {
        private const string RunegateSecret = "runegate-callback-signing-key-0123456789";

        [Fact]
        public void RunegateDeposit_VerifiesThenParses()
        {
            const long ts = 1700000000;
            var body = CryptoFixtures.Utf8(
                "{\"type\":\"deposit.succeeded\",\"depositId\":\"dep_42\",\"amount\":\"100.00\",\"currency\":\"USDT\"}");
            var header = CryptoFixtures.RunegateHeader(RunegateSecret, ts, body);

            Assert.True(RunegateWebhookVerifier.Verify(header, body, RunegateSecret, ts, 300));

            var json = JObject.Parse(Encoding.UTF8.GetString(body));
            Assert.Equal("dep_42", (string)json["depositId"]);
            Assert.Equal("100.00", (string)json["amount"]);
        }

        [Fact]
        public void ReplayedRunegateWebhook_IsRejected()
        {
            const long signedAt = 1700000000;
            var body = CryptoFixtures.Utf8("{\"type\":\"deposit.succeeded\",\"depositId\":\"dep_42\"}");
            var header = CryptoFixtures.RunegateHeader(RunegateSecret, signedAt, body);

            // Same signature presented 10 minutes later -> outside the 5-minute freshness window.
            Assert.False(RunegateWebhookVerifier.Verify(header, body, RunegateSecret, signedAt + 600, 300));
        }

        [Fact]
        public void WasabiWebhook_VerifiesThenParses()
        {
            var pair = CryptoFixtures.NewRsaPair();
            var body = CryptoFixtures.Utf8(
                "{\"event\":\"card.funded\",\"cardNo\":\"4111111111111111\",\"amount\":\"50.00\"}");
            var sig = CryptoFixtures.WasabiSign(pair.PrivateKey, body);

            Assert.True(WasabiSignatureVerifier.Verify(sig, body, pair.PublicKeySpkiBase64));

            var json = JObject.Parse(Encoding.UTF8.GetString(body));
            Assert.Equal("card.funded", (string)json["event"]);
        }

        [Fact]
        public void ForgedWasabiWebhook_IsRejectedBeforeParse()
        {
            var legit = CryptoFixtures.NewRsaPair();
            var attacker = CryptoFixtures.NewRsaPair();
            var body = CryptoFixtures.Utf8("{\"event\":\"card.funded\",\"amount\":\"99999.00\"}");
            var forged = CryptoFixtures.WasabiSign(attacker.PrivateKey, body);

            // Signed by the wrong key -> the handler must not trust (or process) it.
            Assert.False(WasabiSignatureVerifier.Verify(forged, body, legit.PublicKeySpkiBase64));
        }
    }
}
