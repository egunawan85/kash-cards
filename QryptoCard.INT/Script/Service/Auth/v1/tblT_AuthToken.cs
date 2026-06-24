using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QryptoCard.INT.Script.Service.Auth.v1
{
    // Access-token row. SHA-256 hash of the opaque random token; plaintext token
    // never persists. Verifier hits this table once per authenticated request via
    // an indexed exact-match on TokenHash. Schema lives at
    // deploy/sql/create-token-tables.sql; mirrored into the test fixture's
    // init.sql. Owned by AuthDbContext (separate code-first context — NOT the
    // legacy DBEntities .edmx).
    [Table("tblT_AuthToken")]
    public class tblT_AuthToken
    {
        [Key]
        [StringLength(50)]
        public string TokenID { get; set; }

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

        [StringLength(50)]
        public string ParentRefreshTokenID { get; set; }
    }
}
