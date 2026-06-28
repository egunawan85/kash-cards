using System;

namespace QryptoCard.Dashboard.Models.Service
{
    // One row of the referrals-tab history (returned by getReferralJoined): a user this caller
    // referred, the total commission they've earned the caller, and whether they've converted.
    public class ReferralBreakdownModel
    {
        public string UserID { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public Nullable<DateTime> DateJoin { get; set; }
        public double Earned { get; set; }
        public bool Converted { get; set; }
    }
}
