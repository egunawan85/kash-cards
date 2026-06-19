using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace QryptoCard.INT.Model.PGCrypto
{
    public class CustomerModel
    {
        public string CustomerID { get; set; }
        public string CustomerName { get; set; }
        public string CustomerEmail { get; set; }
        public string CustomerDesc { get; set; }
        public string InvoicePrefix { get; set; }
        public Nullable<int> InvoiceSequence { get; set; }
        public Nullable<int> InvoiceSequenceCount { get; set; }
        public Nullable<int> isStaticAdress { get; set; }
        public Nullable<int> isActive { get; set; }
        public Nullable<System.DateTime> DateCreated { get; set; }
        public Nullable<System.DateTime> DateUpdated { get; set; }
    }
}