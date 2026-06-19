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
    }
}