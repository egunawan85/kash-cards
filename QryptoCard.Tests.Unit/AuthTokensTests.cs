using System;
using System.Linq;
using QryptoCard.Sec;
using Xunit;

namespace QryptoCard.Tests.Unit
{
    public class AuthTokensTests
    {
        [Fact]
        public void AccessAndRefresh_HaveCorrectPrefixes()
        {
            Assert.StartsWith("at_", AuthTokens.NewAccessToken());
            Assert.StartsWith("rt_", AuthTokens.NewRefreshToken());
        }

        [Fact]
        public void Tokens_AreHighEntropy_AndDistinct()
        {
            var tokens = Enumerable.Range(0, 200).Select(_ => AuthTokens.NewAccessToken()).ToList();
            Assert.Equal(tokens.Count, tokens.Distinct().Count()); // no collisions
            Assert.All(tokens, t => Assert.True(t.Length > 40));   // 256-bit base64url body
        }

        [Fact]
        public void Hash_IsDeterministic_AndNotTheToken()
        {
            var token = AuthTokens.NewAccessToken();
            Assert.Equal(AuthTokens.Hash(token), AuthTokens.Hash(token));
            Assert.NotEqual(token, AuthTokens.Hash(token));
            Assert.Equal(64, AuthTokens.Hash(token).Length); // hex SHA-256
        }

        [Fact]
        public void Matches_AcceptsCorrect_RejectsWrongOrEmpty()
        {
            var token = AuthTokens.NewAccessToken();
            var stored = AuthTokens.Hash(token);
            Assert.True(AuthTokens.Matches(token, stored));
            Assert.False(AuthTokens.Matches(AuthTokens.NewAccessToken(), stored)); // different token
            Assert.False(AuthTokens.Matches("", stored));
            Assert.False(AuthTokens.Matches(token, ""));
            Assert.False(AuthTokens.Matches(null, stored));
        }

        [Fact]
        public void IsActive_FailsClosed()
        {
            var now = new DateTime(2026, 1, 1, 12, 0, 0);
            Assert.True(AuthTokens.IsActive(now.AddMinutes(5), null, now));   // valid, not revoked
            Assert.False(AuthTokens.IsActive(now.AddMinutes(-1), null, now)); // expired
            Assert.False(AuthTokens.IsActive(now.AddMinutes(5), now.AddMinutes(-1), now)); // revoked
            Assert.False(AuthTokens.IsActive(null, null, now));               // no expiry -> inactive
        }

        [Fact]
        public void Lifetimes_MatchRunegateParity()
        {
            Assert.Equal(TimeSpan.FromMinutes(15), AuthTokens.AccessLifetime);
            Assert.Equal(TimeSpan.FromDays(7), AuthTokens.RefreshLifetime);
        }
    }
}
