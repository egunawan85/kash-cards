using System;

namespace QryptoCard.Dashboard.Admin.Models.Service
{
    // Dashboard-local mirror of QryptoCard.INT.Model.Service.AuthMintResponse.
    // Local copy because the dashboard talks to the API tier over raw HTTP +
    // JsonConvert (no WCF Service Reference). Field names must match the upstream
    // DTO's property names so JsonConvert.DeserializeObject lands them correctly.
    // Profile is the JSON-serialized vw_Admin (with Password + PIN nulled) — the
    // dashboard deserializes it into AdminModel to bootstrap SessionLib.
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
