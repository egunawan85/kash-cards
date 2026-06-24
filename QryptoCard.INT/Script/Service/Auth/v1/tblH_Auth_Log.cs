using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QryptoCard.INT.Script.Service.Auth.v1
{
    // Append-only audit ledger for auth-relevant events. Distinct from
    // tblH_User_Login / tblH_Admin_Login (those are OTP session-tracking with a
    // very different shape).
    //
    // EventType values currently emitted:
    //   "refresh_token_reuse"            — refresh() saw an already-rotated refresh
    //                                      token; the whole chain was revoked.
    //   "refresh_token_concurrent_use"   — two concurrent refresh() calls raced;
    //                                      the loser triggers a chain-revoke.
    //   "logout"                         — explicit revoke() (idempotent logout).
    //   "subject_revoke"                 — revokeAllForSubject (ban / pwd-change).
    //   "revoke_token_auth_failure"      — wrong service token on revokeAllForSubject.
    //
    // SourceIP is populated when available (WCF channel surfaces it; null on
    // in-process test invocations). Details is a JSON blob for event-specific
    // payload.
    [Table("tblH_Auth_Log")]
    public class tblH_Auth_Log
    {
        [Key]
        [StringLength(50)]
        public string LogID { get; set; }

        [Required]
        [StringLength(50)]
        public string EventType { get; set; }

        [StringLength(50)]
        public string Subject { get; set; }

        [StringLength(10)]
        public string SubjectType { get; set; }

        [StringLength(50)]
        public string RefreshTokenID { get; set; }

        [StringLength(50)]
        public string RotationChainRoot { get; set; }

        [StringLength(45)]
        public string SourceIP { get; set; }

        public string Details { get; set; }

        public DateTime DateLogged { get; set; }
    }
}
