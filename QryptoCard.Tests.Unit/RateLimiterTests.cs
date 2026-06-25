using System;
using QryptoCard.Sec;
using Xunit;

namespace QryptoCard.Tests.Unit
{
    // Sliding-window RateLimiter. Drives the injected-clock overload so window behaviour is
    // deterministic. ResetForTests clears the shared static store between cases.
    public class RateLimiterTests
    {
        private static readonly DateTimeOffset T0 =
            new DateTimeOffset(2026, 6, 25, 12, 0, 0, TimeSpan.Zero);

        public RateLimiterTests()
        {
            RateLimiter.ResetForTests();
        }

        [Fact]
        public void UnderLimit_Admits()
        {
            Assert.True(RateLimiter.TryClaim("k", 3, 60, T0));
            Assert.True(RateLimiter.TryClaim("k", 3, 60, T0.AddSeconds(1)));
            Assert.True(RateLimiter.TryClaim("k", 3, 60, T0.AddSeconds(2)));
        }

        [Fact]
        public void AtLimit_Rejects()
        {
            Assert.True(RateLimiter.TryClaim("k", 3, 60, T0));
            Assert.True(RateLimiter.TryClaim("k", 3, 60, T0));
            Assert.True(RateLimiter.TryClaim("k", 3, 60, T0));
            Assert.False(RateLimiter.TryClaim("k", 3, 60, T0));
        }

        [Fact]
        public void WindowScroll_FreesAllSlots()
        {
            for (int i = 0; i < 3; i++) Assert.True(RateLimiter.TryClaim("k", 3, 60, T0));
            Assert.False(RateLimiter.TryClaim("k", 3, 60, T0.AddSeconds(30)));
            // All three stamps age out past the 60s window.
            Assert.True(RateLimiter.TryClaim("k", 3, 60, T0.AddSeconds(61)));
        }

        [Fact]
        public void WindowScroll_FreesOnlyExpiredSlots()
        {
            Assert.True(RateLimiter.TryClaim("k", 2, 60, T0));            // stamp @ t0
            Assert.True(RateLimiter.TryClaim("k", 2, 60, T0.AddSeconds(40))); // stamp @ t0+40
            Assert.False(RateLimiter.TryClaim("k", 2, 60, T0.AddSeconds(50))); // both still live
            // Only the t0 stamp expires by t0+61; one slot frees, the t0+40 stamp remains.
            Assert.True(RateLimiter.TryClaim("k", 2, 60, T0.AddSeconds(61)));
            Assert.False(RateLimiter.TryClaim("k", 2, 60, T0.AddSeconds(61)));
        }

        [Fact]
        public void DistinctKeys_AreIsolated()
        {
            Assert.True(RateLimiter.TryClaim("a", 1, 60, T0));
            Assert.False(RateLimiter.TryClaim("a", 1, 60, T0));
            // Different key has its own bucket.
            Assert.True(RateLimiter.TryClaim("b", 1, 60, T0));
        }

        [Fact]
        public void InvalidArguments_Throw()
        {
            Assert.Throws<ArgumentNullException>(() => RateLimiter.TryClaim(null, 1, 60, T0));
            Assert.Throws<ArgumentNullException>(() => RateLimiter.TryClaim("", 1, 60, T0));
            Assert.Throws<ArgumentOutOfRangeException>(() => RateLimiter.TryClaim("k", 0, 60, T0));
            Assert.Throws<ArgumentOutOfRangeException>(() => RateLimiter.TryClaim("k", 1, 0, T0));
        }
    }
}
