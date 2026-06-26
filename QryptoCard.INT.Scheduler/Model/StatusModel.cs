using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace QryptoCard.INT.Scheduler.Model
{
    public class StatusModel
    {
        public const string Created = "created";
        public const string Expired = "expired";
        public const string Paid = "paid";
        public const string Completed = "completed";
        public const string Cancelled = "cancelled";
        public const string InProgress = "in progress";
        public const string OverPaid = "over paid";
        // Synced with the INT/Callback status models: a spend parked here had an ambiguous provider
        // result (debit committed, card outcome unknown). The sweep resolves these.
        public const string PendingProvider = "pending provider";
        public const string OpenCard = "open card";
        public const string Success = "success";
        public const string Failed = "failed";
    }
}