using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace QryptoCard.Dashboard.Models.Service
{
    // Shape of the JSON returned by GET /v1/user/ledger. A single page of the caller's
    // prepaid-balance history (Plan 07). Paging is clamped server-side (page >= 1,
    // 1 <= pageSize <= 100) so these values reflect what the server actually applied.
    public class LedgerModel
    {
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int Total { get; set; }
        public List<LedgerEntryModel> Items { get; set; }
    }

    // One ledger row. Internal surrogate keys (ID, BalanceID) are intentionally not exposed.
    public class LedgerEntryModel
    {
        public string Type { get; set; }
        public Nullable<decimal> Amount { get; set; }
        public Nullable<decimal> Commision { get; set; }
        public Nullable<decimal> BalancePrevious { get; set; }
        public Nullable<decimal> Balance { get; set; }
        public string TransactionID { get; set; }
        public string Status { get; set; }
        public Nullable<System.DateTime> CreatedDate { get; set; }
    }
}
