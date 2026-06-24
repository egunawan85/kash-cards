using System;
using System.Collections.Generic;
using QryptoCard.Sec;
using Xunit;

namespace QryptoCard.Tests.Unit
{
    public class TokenServiceTests
    {
        // In-memory ITokenStore so the lifecycle logic is testable without a database.
        private sealed class FakeStore : ITokenStore
        {
            public readonly Dictionary<string, TokenRecord> Access = new Dictionary<string, TokenRecord>();
            public readonly Dictionary<string, TokenRecord> Refresh = new Dictionary<string, TokenRecord>();
            public void SaveAccess(TokenRecord r) { Access[r.TokenHash] = r; }
            public void SaveRefresh(TokenRecord r) { Refresh[r.TokenHash] = r; }
            public TokenRecord FindAccess(string h) { TokenRecord r; return Access.TryGetValue(h, out r) ? r : null; }
            public TokenRecord FindRefresh(string h) { TokenRecord r; return Refresh.TryGetValue(h, out r) ? r : null; }
            public void RevokeAccess(string h, DateTime at) { if (Access.ContainsKey(h)) Access[h].RevokedAt = at; }
            public void RevokeRefresh(string h, DateTime at) { if (Refresh.ContainsKey(h)) Refresh[h].RevokedAt = at; }
        }

        private static readonly DateTime Now = new DateTime(2026, 1, 1, 12, 0, 0);

        [Fact]
        public void Issue_MintsBothTokens_StoredOnlyAsHashes()
        {
            var store = new FakeStore();
            var svc = new TokenService(store);
            var t = svc.Issue("user-1", SubjectType.User, Now);

            Assert.StartsWith("at_", t.AccessToken);
            Assert.StartsWith("rt_", t.RefreshToken);
            // stored under the HASH, and the raw token is not a key
            Assert.True(store.Access.ContainsKey(AuthTokens.Hash(t.AccessToken)));
            Assert.False(store.Access.ContainsKey(t.AccessToken));
            Assert.Equal(Now.Add(AuthTokens.AccessLifetime), t.AccessExpiresAt);
        }

        [Fact]
        public void VerifyAccess_AcceptsValid_OfCorrectTier()
        {
            var svc = new TokenService(new FakeStore());
            var t = svc.Issue("user-1", SubjectType.User, Now);
            Assert.Equal("user-1", svc.VerifyAccess(t.AccessToken, SubjectType.User, Now));
        }

        [Fact]
        public void VerifyAccess_RejectsCrossTier()
        {
            var svc = new TokenService(new FakeStore());
            var t = svc.Issue("user-1", SubjectType.User, Now);
            // a user token must NOT satisfy an admin route
            Assert.Null(svc.VerifyAccess(t.AccessToken, SubjectType.Admin, Now));
        }

        [Fact]
        public void VerifyAccess_RejectsExpiredRevokedUnknownAndWrongPrefix()
        {
            var store = new FakeStore();
            var svc = new TokenService(store);
            var t = svc.Issue("user-1", SubjectType.User, Now);

            Assert.Null(svc.VerifyAccess(t.AccessToken, SubjectType.User, Now.Add(AuthTokens.AccessLifetime).AddSeconds(1))); // expired
            Assert.Null(svc.VerifyAccess(AuthTokens.NewAccessToken(), SubjectType.User, Now)); // unknown
            Assert.Null(svc.VerifyAccess("rt_" + t.AccessToken.Substring(3), SubjectType.User, Now)); // wrong prefix
            Assert.Null(svc.VerifyAccess(null, SubjectType.User, Now));

            svc.RevokeAccess(t.AccessToken, Now);
            Assert.Null(svc.VerifyAccess(t.AccessToken, SubjectType.User, Now)); // revoked
        }

        [Fact]
        public void Refresh_RotatesAndIsSingleUse()
        {
            var svc = new TokenService(new FakeStore());
            var first = svc.Issue("admin-9", SubjectType.Admin, Now);

            var rotated = svc.Refresh(first.RefreshToken, Now);
            Assert.NotNull(rotated);
            Assert.NotEqual(first.AccessToken, rotated.AccessToken);
            Assert.NotEqual(first.RefreshToken, rotated.RefreshToken);
            // the new access token preserves the subject + tier
            Assert.Equal("admin-9", svc.VerifyAccess(rotated.AccessToken, SubjectType.Admin, Now));
            // the OLD refresh token is now dead (single-use rotation)
            Assert.Null(svc.Refresh(first.RefreshToken, Now));
        }

        [Fact]
        public void Refresh_RejectsAccessTokenOrUnknown()
        {
            var svc = new TokenService(new FakeStore());
            var t = svc.Issue("user-1", SubjectType.User, Now);
            Assert.Null(svc.Refresh(t.AccessToken, Now));        // an access token is not a refresh token
            Assert.Null(svc.Refresh(AuthTokens.NewRefreshToken(), Now)); // unknown
        }
    }
}
