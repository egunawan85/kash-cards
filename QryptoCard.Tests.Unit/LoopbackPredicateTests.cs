using QryptoCard.Sec;
using Xunit;

namespace QryptoCard.Tests.Unit
{
    public class LoopbackPredicateTests
    {
        [Theory]
        [InlineData("127.0.0.1")]
        [InlineData("127.0.0.5")]
        [InlineData("::1")]
        [InlineData("::ffff:127.0.0.1")] // IPv4-mapped loopback — must still be treated as loopback
        public void LoopbackForms_AreLoopback(string peer)
        {
            Assert.True(LoopbackPredicate.IsLoopback(peer));
        }

        [Theory]
        [InlineData("203.0.113.7")]
        [InlineData("8.8.8.8")]
        [InlineData("::ffff:1.2.3.4")] // IPv4-mapped public — not loopback
        [InlineData("not-an-ip")]
        [InlineData("")]
        [InlineData(null)]
        public void NonLoopbackOrGarbage_IsNotLoopback(string peer)
        {
            Assert.False(LoopbackPredicate.IsLoopback(peer));
        }
    }
}
