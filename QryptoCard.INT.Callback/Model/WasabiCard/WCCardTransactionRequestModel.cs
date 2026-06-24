using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace QryptoCard.INT.Callback.Model.WasabiCard
{
    public class WCCardTransactionRequestModel
    {
        public int pageNum { get; set; }
        public int pageSize { get; set; }
        public string type { get; set; }
        // Optional server-side filters supported by /card/v2/transaction. Used by the
        // post-verify deposit cross-check to fetch the canonical record for one order.
        public string merchantOrderNo { get; set; }
        public string orderNo { get; set; }
    }
}