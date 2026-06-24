using System;

namespace QryptoCard.Sec
{
    /// <summary>Token subject tiers. A token minted for one tier can never satisfy the other.</summary>
    public static class SubjectType
    {
        public const string User = "user";
        public const string Admin = "admin";

        public static bool IsValid(string t) { return t == User || t == Admin; }
    }

    /// <summary>
    /// A persisted access- or refresh-token row. The raw token is never stored — only
    /// <see cref="TokenHash"/> (SHA-256). Authority lives entirely in this row.
    /// </summary>
    public class TokenRecord
    {
        public string TokenHash { get; set; }
        public string SubjectId { get; set; }
        public string SubjectType { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? ExpiresAt { get; set; }
        public DateTime? RevokedAt { get; set; }
    }
}
