using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace QryptoCard.INT.Callback.Model.PGCrypto
{
    public class PGStatusModel
    {
        public const string Created = "created";
        public const string Requested = "requested";
        public const string WaitingPayment = "awaiting for payment";
        public const string Paid = "paid";
        public const string InProgress = "in progress";
        // Spend parked here when the provider result was ambiguous (timeout/null). The card webhook
        // must still be able to finalize or refund it, so finalizer queries accept it too.
        public const string PendingProvider = "pending provider";
        public const string OpenCard = "open card";
        public const string Completed = "completed";
        public const string Success = "success";
        public const string CancelByAdmin = "cancel by admin";
        public const string CancelByCust = "cancel by customer";
        public const string Failed = "failed";
    }
}