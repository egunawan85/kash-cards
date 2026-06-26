using System;
using QryptoCard.Sec;
using Xunit;

namespace QryptoCard.Tests.Unit
{
    // Pure state-machine tests for the password-lockout transition. The DB-side
    // QryptoCard.INT.Security.PasswordLockout.RecordFailure UPDATE must mirror this exactly.
    public class LockoutPolicyTests
    {
        private static readonly DateTime Now = new DateTime(2026, 6, 25, 12, 0, 0, DateTimeKind.Utc);

        [Theory]
        [InlineData(0, 1)]
        [InlineData(1, 2)]
        [InlineData(2, 3)]
        [InlineData(3, 4)]
        public void BelowThreshold_Increments_NoLock(int oldCount, int expectedCount)
        {
            var s = LockoutPolicy.ComputeNextState(oldCount, null, Now);
            Assert.Equal(expectedCount, s.FailureCount);
            Assert.Null(s.LockoutEnd); // still below threshold → not locked
        }

        [Fact]
        public void ReachingThreshold_SetsLockoutEnd()
        {
            // oldCount 4 → 5 == threshold → lock for the window
            var s = LockoutPolicy.ComputeNextState(4, null, Now);
            Assert.Equal(LockoutPolicy.Threshold, s.FailureCount);
            Assert.Equal(Now.AddMinutes(LockoutPolicy.LockoutMinutes), s.LockoutEnd);
        }

        [Fact]
        public void ExpiredLockout_ResetsToFreshWindow()
        {
            // Lock that already elapsed; the next failure must start over at 1, not immediately re-lock.
            DateTime expired = Now.AddMinutes(-1);
            var s = LockoutPolicy.ComputeNextState(LockoutPolicy.Threshold, expired, Now);
            Assert.Equal(1, s.FailureCount);
            Assert.Null(s.LockoutEnd);
        }

        [Fact]
        public void ActiveLockout_StillIncrements_KeepsExistingEnd()
        {
            // A failure while still locked (end in the future, below-threshold path) keeps the existing
            // end and just counts up — IsLockedOut short-circuits this in practice, but the math holds.
            DateTime future = Now.AddMinutes(10);
            var s = LockoutPolicy.ComputeNextState(1, future, Now);
            Assert.Equal(2, s.FailureCount);
            Assert.Equal(future, s.LockoutEnd);
        }

        [Fact]
        public void NullEnd_BehavesAsNormalIncrement()
        {
            var s = LockoutPolicy.ComputeNextState(0, null, Now);
            Assert.Equal(1, s.FailureCount);
            Assert.Null(s.LockoutEnd);
        }
    }
}
