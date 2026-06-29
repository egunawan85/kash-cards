using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace QryptoCard.INT.Model.Service
{
    public class StatusModel
    {
        public const string Created = "created";
        public const string Expired = "expired";
        public const string Paid = "paid";
        public const string Completed = "completed";
        public const string Cancelled = "cancelled";
        public const string Rejected = "rejected";
        public const string Approved = "approved";
        public const string InQueue = "in queue";
        public const string InProgress = "in progress";
        // Provider call returned an ambiguous result (timeout / null): the balance debit stands
        // and the card may or may not have been created — reconcile, never auto-reverse (C3.c).
        public const string PendingProvider = "pending provider";
        public const string OnHold = "on hold";
        public const string Pending = "pending";
        public const string Archived = "archived";
        public const string Failed = "failed";
        public const string Gone = "gone";
        // Confirmed-success terminal status a finalized card order carries (set by the Callback-tier
        // PGStatusModel.Success = "success"); mirrored here so the INT-tier refund path can gate on it.
        public const string Success = "success";
        // Admin refund lifecycle: the card's open order is claimed Success -> RefundPending while the
        // WasabiCard cancel is in flight, then -> Refunded once the buyer credit commits (or reverted
        // to Success on a definitive cancel rejection). A cancel that returns an ambiguous result
        // leaves the order at RefundPending for operator review — the buyer is never credited on an
        // unconfirmed cancel.
        public const string RefundPending = "refund pending";
        public const string Refunded = "refunded";

        public const string PartiallyPaid = "partially paid";
        public const string OverPaid = "over paid";
    }
}