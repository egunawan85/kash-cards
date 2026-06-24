using System;

namespace QryptoCard.Dashboard.Models.Service
{
    // Dashboard-local mirror of QryptoCard.INT.Model.Service.AuthMintResponse.
    // The dashboard talks to the API tier over raw HTTP + JsonConvert (no WCF
    // Service Reference), so field names must match the API DTO's [DataMember]
    // names for JsonConvert.DeserializeObject to land them correctly.
    //
    // Returned from a successful mint-after-otp (and refresh) call. The dashboard
    // captures the tokens into Session and replays AccessToken on every upstream
    // request via the Authorization: Bearer header. Profile is the JSON-serialized
    // user record (Password + PIN nulled by the API) the dashboard deserializes
    // into UserModel to bootstrap SessionLib — populated only on mint, null on
    // refresh.
    public class AuthMintResponse
    {
        public string AccessToken { get; set; }
        public DateTime AccessTokenExpires { get; set; }
        public string RefreshToken { get; set; }
        public DateTime RefreshTokenExpires { get; set; }
        public string Subject { get; set; }
        public string SubjectType { get; set; }
        public string Profile { get; set; }
    }
}
