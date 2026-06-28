using System;
using System.Collections.Generic;

namespace QryptoCard.Dashboard.Models.Service
{
    // The full Referrals-tab payload returned by getReferralJoined: the per-referral breakdown plus
    // the chronological commission history.
    public class ReferralTabModel
    {
        public List<ReferralBreakdownModel> Referrals { get; set; }
        public List<CommissionHistoryModel> Commissions { get; set; }
    }

    // One commission event: when it was credited, the referral it came from, and the amount.
    public class CommissionHistoryModel
    {
        public Nullable<DateTime> DateCreated { get; set; }
        public string RefereeName { get; set; }
        public Nullable<double> Commission { get; set; }
    }

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
