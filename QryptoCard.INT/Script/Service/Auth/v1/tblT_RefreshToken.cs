using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QryptoCard.INT.Script.Service.Auth.v1
{
    // Refresh-token row with rotation metadata. Every refresh() call consumes a
    // row (ReplacedByID set, RevokedAt left null until explicit revoke), issues
    // a fresh row, and links them. RotationChainRoot points at the original
    // login-time refresh token so chain-revoke (on reuse detection or logout)
    // can sweep every descendant in one indexed UPDATE.
    [Table("tblT_RefreshToken")]
    public class tblT_RefreshToken
    {
        [Key]
        [StringLength(50)]
        public string RefreshTokenID { get; set; }

        [Required]
        [Column(TypeName = "char")]
        [StringLength(64)]
        public string TokenHash { get; set; }

        [Required]
        [StringLength(50)]
        public string Subject { get; set; }

        [Required]
        [StringLength(10)]
        public string SubjectType { get; set; }

        public DateTime DateIssued { get; set; }

        public DateTime DateExpired { get; set; }

        public DateTime? RevokedAt { get; set; }

        // Forward link: when this row is consumed by refresh(), ReplacedByID is
        // set to the new token's ID. A subsequent refresh() against this same
        // (now-replaced) token is the reuse-detection trigger.
        [StringLength(50)]
        public string ReplacedByID { get; set; }

        // Points at the first (login-time) refresh token in this rotation chain.
        // The root row has RotationChainRoot == RefreshTokenID (self-reference).
        // Chain-revoke is `WHERE RotationChainRoot = X`; this column is indexed.
        [Required]
        [StringLength(50)]
        public string RotationChainRoot { get; set; }
    }
}
