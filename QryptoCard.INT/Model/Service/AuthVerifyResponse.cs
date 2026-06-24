using System;
using System.Runtime.Serialization;

namespace QryptoCard.INT.Model.Service
{
    // Returned from IAuthV1Service.verify. The Bearer attribute on the API side
    // checks Valid, then stashes Subject + SubjectType (+ Email) onto the request
    // for controller pickup.
    //
    // Invalid cases (missing row, expired, revoked, hash mismatch) all return
    // Valid=false with the other fields cleared. No information leak — the
    // caller cannot distinguish "token never existed" from "token revoked
    // 5 minutes ago" from "token expired 10 minutes ago".
    [DataContract]
    public class AuthVerifyResponse
    {
        [DataMember]
        public bool Valid { get; set; }

        [DataMember]
        public string Subject { get; set; }

        [DataMember]
        public string SubjectType { get; set; }

        [DataMember]
        public DateTime ExpiresAt { get; set; }

        // Subject's email for controller-side use, resolved by verify() via an
        // indexed lookup against tblM_User / tblM_Admin. May be null if the
        // subject row was deleted between mint and verify (extreme edge — the
        // token already expires within the access TTL).
        [DataMember]
        public string Email { get; set; }
    }
}
