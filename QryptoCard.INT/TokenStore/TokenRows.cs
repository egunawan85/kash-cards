using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QryptoCard.INT.TokenStore
{
    // EF6 entities mapped to the two token tables. Shape mirrors QryptoCard.Sec.TokenRecord;
    // EfTokenStore converts between them. Schema is created by deploy/sql/create-token-tables.sql
    // (we disable the code-first initializer, so EF never tries to create/migrate).

    [Table("tblT_AuthToken")]
    public class AuthTokenRow
    {
        [Key] public long Id { get; set; }
        [Required, MaxLength(64)] public string TokenHash { get; set; }
        [Required, MaxLength(64)] public string SubjectId { get; set; }
        [Required, MaxLength(10)] public string SubjectType { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? ExpiresAt { get; set; }
        public DateTime? RevokedAt { get; set; }
    }

    [Table("tblT_RefreshToken")]
    public class RefreshTokenRow
    {
        [Key] public long Id { get; set; }
        [Required, MaxLength(64)] public string TokenHash { get; set; }
        [Required, MaxLength(64)] public string SubjectId { get; set; }
        [Required, MaxLength(10)] public string SubjectType { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? ExpiresAt { get; set; }
        public DateTime? RevokedAt { get; set; }
    }
}
