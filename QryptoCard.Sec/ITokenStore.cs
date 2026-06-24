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
        void RevokeRefresh(string tokenHash, DateTime at);
    }
}
