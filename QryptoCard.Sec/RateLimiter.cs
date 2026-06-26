using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace QryptoCard.Sec
{
    // Sliding-window rate-limiter store.
    //
    // Single in-process backing store keyed by an opaque bucket string (the
    // RateLimitAttribute keys it "ip:<client-ip>:<route>"). Each bucket holds the timestamps
    // of recent claims; a claim is admitted if, after pruning timestamps older than
    // `now - windowSeconds`, the bucket size is strictly less than the limit. On admission,
    // `now` is appended and the claim returns true.
    //
    // Bucket lifetime: entries accumulate once created and are not GC'd on their own — at
    // single-instance scale (bounded IP/route cardinality over the window) the footprint
    // stays low single-digit MB even over days, so no sweeper is wired up. Each API-tier
    // app-pool has its own static store, which is correct for a per-IP soft DoS brake.
    // If a tier later scales horizontally, swap this for a Redis-backed store — the
    // interface shape is deliberately simple to make that swap mechanical.
    public static class RateLimiter
    {
        private static readonly ConcurrentDictionary<string, Bucket> _buckets =
            new ConcurrentDictionary<string, Bucket>(StringComparer.Ordinal);

        public static bool TryClaim(string bucketKey, int limit, int windowSeconds)
        {
            return TryClaim(bucketKey, limit, windowSeconds, DateTimeOffset.UtcNow);
        }

        // Testable overload — accepts the clock. No branching on a DEBUG build or hidden
        // statics; tests drive the clock explicitly.
        internal static bool TryClaim(string bucketKey, int limit, int windowSeconds, DateTimeOffset now)
        {
            if (string.IsNullOrEmpty(bucketKey)) throw new ArgumentNullException("bucketKey");
            if (limit < 1) throw new ArgumentOutOfRangeException("limit", "must be >= 1");
            if (windowSeconds < 1) throw new ArgumentOutOfRangeException("windowSeconds", "must be >= 1");

            Bucket bucket = _buckets.GetOrAdd(bucketKey, _ => new Bucket());

            lock (bucket.SyncRoot)
            {
                DateTimeOffset cutoff = now.AddSeconds(-windowSeconds);
                Prune(bucket.Timestamps, cutoff);

                if (bucket.Timestamps.Count >= limit) return false;

                bucket.Timestamps.Add(now);
                return true;
            }
        }

        // Test-only reset — clears all buckets so test runs don't leak state into each other.
        internal static void ResetForTests()
        {
            _buckets.Clear();
        }

        private static void Prune(List<DateTimeOffset> stamps, DateTimeOffset cutoff)
        {
            // Stamps are appended in (wall-clock) order — find the first index at or after the
            // cutoff and drop everything before it. Linear scan for code-review clarity at
            // expected list sizes (<= limit).
            int keepFrom = 0;
            while (keepFrom < stamps.Count && stamps[keepFrom] < cutoff) keepFrom++;
            if (keepFrom > 0) stamps.RemoveRange(0, keepFrom);
        }

        private sealed class Bucket
        {
            public readonly object SyncRoot = new object();
            public readonly List<DateTimeOffset> Timestamps = new List<DateTimeOffset>();
        }
    }
}
