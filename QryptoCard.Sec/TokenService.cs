using System;

namespace QryptoCard.Sec
{
    /// <summary>The tokens handed back to a client at mint/refresh. The raw values appear here once.</summary>
    public class IssuedTokens
    {
        public string AccessToken;
        public string RefreshToken;
        public DateTime AccessExpiresAt;
    }

    /// <summary>
    /// Mint / verify / refresh / revoke opaque bearer tokens. All crypto goes through
    /// <see cref="AuthTokens"/>; persistence through <see cref="ITokenStore"/>. SubjectType is
    /// stamped at mint and enforced at verify, so a user token can never satisfy an admin route.
    /// </summary>
    public class TokenService
    {
        private readonly ITokenStore _store;

        public TokenService(ITokenStore store)
        {
            if (store == null) throw new ArgumentNullException("store");
            _store = store;
        }

        public IssuedTokens Issue(string subjectId, string subjectType, DateTime now)
        {
            if (string.IsNullOrEmpty(subjectId)) throw new ArgumentException("subjectId required");
            if (!SubjectType.IsValid(subjectType)) throw new ArgumentException("subjectType must be user or admin");

            string access = AuthTokens.NewAccessToken();
            string refresh = AuthTokens.NewRefreshToken();
            DateTime accessExp = now.Add(AuthTokens.AccessLifetime);

            _store.SaveAccess(new TokenRecord
            {
                TokenHash = AuthTokens.Hash(access), SubjectId = subjectId, SubjectType = subjectType,
                CreatedAt = now, ExpiresAt = accessExp
            });
            _store.SaveRefresh(new TokenRecord
            {
                TokenHash = AuthTokens.Hash(refresh), SubjectId = subjectId, SubjectType = subjectType,
                CreatedAt = now, ExpiresAt = now.Add(AuthTokens.RefreshLifetime)
            });

            return new IssuedTokens { AccessToken = access, RefreshToken = refresh, AccessExpiresAt = accessExp };
        }

        /// <summary>Subject id if the access token is valid AND of the required tier, else null.</summary>
        public string VerifyAccess(string accessToken, string requiredType, DateTime now)
        {
            if (string.IsNullOrEmpty(accessToken) || !accessToken.StartsWith(AuthTokens.AccessPrefix)) return null;
            TokenRecord rec = _store.FindAccess(AuthTokens.Hash(accessToken));
            if (rec == null) return null;
            if (!AuthTokens.IsActive(rec.ExpiresAt, rec.RevokedAt, now)) return null;
            if (rec.SubjectType != requiredType) return null;   // cross-tier token rejected
            return rec.SubjectId;
        }

        /// <summary>Rotate: validate the refresh token, single-use-revoke it, and issue a fresh pair.</summary>
        public IssuedTokens Refresh(string refreshToken, DateTime now)
        {
            if (string.IsNullOrEmpty(refreshToken) || !refreshToken.StartsWith(AuthTokens.RefreshPrefix)) return null;
            string hash = AuthTokens.Hash(refreshToken);
            TokenRecord rec = _store.FindRefresh(hash);
            if (rec == null || !AuthTokens.IsActive(rec.ExpiresAt, rec.RevokedAt, now)) return null;
            _store.RevokeRefresh(hash, now);                    // rotation: a refresh token is single-use
            return Issue(rec.SubjectId, rec.SubjectType, now);
        }

        public void RevokeAccess(string accessToken, DateTime now)
        {
            if (string.IsNullOrEmpty(accessToken)) return;
            _store.RevokeAccess(AuthTokens.Hash(accessToken), now);
        }
    }
}
