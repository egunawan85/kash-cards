using System;
using System.Runtime.Serialization;

namespace QryptoCard.INT.Model.Service
{
    // Returned from a successful mintAfterOtpVerify() or refresh() call. The
    // dashboard captures these into Session and replays AccessToken on every
    // upstream request via the Authorization: Bearer header.
    //
    // Both expiry timestamps are in server time. The dashboard's silent-refresh
    // helper uses a clock-skew buffer to decide when to refresh.
    [DataContract]
    public class AuthMintResponse
    {
        // Format: "at_<43 base64url chars>". See AuthTokens.
        [DataMember]
        public string AccessToken { get; set; }

        [DataMember]
        public DateTime AccessTokenExpires { get; set; }

        // Format: "rt_<43 base64url chars>".
        [DataMember]
        public string RefreshToken { get; set; }

        [DataMember]
        public DateTime RefreshTokenExpires { get; set; }

        // UserID or AdminID, depending on SubjectType. Lets the dashboard
        // bootstrap session state without an extra round-trip.
        [DataMember]
        public string Subject { get; set; }

        [DataMember]
        public string SubjectType { get; set; }

        // Serialized user/admin profile record the dashboard needs to bootstrap
        // its session in a single call — mirrors what the legacy /auth/login/verify
        // response returned (the tblM_User row for users, the vw_Admin row for
        // admins). JSON string (op.Data is itself a JSON-string-in-object, and this
        // nests one more level). Populated only by mintAfterOtpVerify; null on
        // refresh (the dashboard already has the profile from the original mint).
        //
        // Credential columns (Password, PIN) are redacted to null before
        // serialization — the legacy verify leaked them, this does not.
        [DataMember]
        public string Profile { get; set; }
    }
}
