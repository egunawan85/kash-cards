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
    }
}