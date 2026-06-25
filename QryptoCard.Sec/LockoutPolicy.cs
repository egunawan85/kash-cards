using System;

namespace QryptoCard.Sec
{
    // Pure password-lockout state machine: the single source of the threshold/window constants and the
    // failure-transition logic. The DB-side counterpart (QryptoCard.INT.Security.PasswordLockout) issues
    // an atomic UPDATE … CASE whose result MUST stay in lockstep with ComputeNextState here; the pure
    // form lives in Sec so it is unit-testable without a database or the EF6/web dependency graph.
    public static class LockoutPolicy
    {
        public const int Threshold = 5;       // consecutive failures that trip a lock
        public const int LockoutMinutes = 15; // lock duration once tripped

        // Given the stored (FailureCount, LockoutEnd) and the current time, return the next state a
        // failed attempt produces. NULL FailureCount is passed as 0 by the caller (matching the SQL's
        // ISNULL(FailureCount, 0)).
        //   - An expired lockout (LockoutEnd set but in the past) starts a FRESH window: count = 1,
        //     end = null. This is the anti-perpetual-DoS corrective — without it, one wrong attempt right
        //     after expiry would immediately re-lock (lock/expire/one-guess/re-lock forever).
        //   - Otherwise the count increments, and the lock trips (end = now + LockoutMinutes) exactly
        //     when the incremented count reaches the threshold; below threshold the end is unchanged.
        public static LockoutState ComputeNextState(int oldCount, DateTime? oldEnd, DateTime now)
        {
            if (oldEnd.HasValue && oldEnd.Value <= now)
                return new LockoutState(1, null);

            int newCount = oldCount + 1;
            DateTime? newEnd = newCount >= Threshold ? (DateTime?)now.AddMinutes(LockoutMinutes) : oldEnd;
            return new LockoutState(newCount, newEnd);
        }

        // Plain struct rather than a ValueTuple so this carries no System.ValueTuple package dependency
        // on the legacy net462/net472 consumers.
        public struct LockoutState
        {
            public readonly int FailureCount;
            public readonly DateTime? LockoutEnd;
            public LockoutState(int failureCount, DateTime? lockoutEnd)
            {
                FailureCount = failureCount;
                LockoutEnd = lockoutEnd;
            }
        }
    }
}
