using System;

namespace QryptoCard.Sec
{
    /// <summary>
    /// Persistence for token rows. The EF6 implementation lives in the INT tier (against the two
    /// token tables); <see cref="TokenService"/> holds the lifecycle logic and is storage-agnostic
    /// so it can be unit-tested against an in-memory fake.
    /// </summary>
    public interface ITokenStore
    {
        void SaveAccess(TokenRecord record);
        void SaveRefresh(TokenRecord record);
        TokenRecord FindAccess(string tokenHash);
        TokenRecord FindRefresh(string tokenHash);
        void RevokeAccess(string tokenHash, DateTime at);
        /// <summary>Atomically revoke an active refresh row; returns true only if THIS call flipped
        /// it (single-winner under concurrency, so refresh rotation can't double-spend).</summary>
        bool TryRevokeRefresh(string tokenHash, DateTime at);
    }
}
