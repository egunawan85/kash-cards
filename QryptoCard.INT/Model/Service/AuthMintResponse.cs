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
    }
}
