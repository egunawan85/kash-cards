using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace QryptoCard.INT.Model.PGCrypto
{
    public class ProductModel
    {
        public string ProductID { get; set; }
        public string ProductName { get; set; }
        public string ProductDesc { get; set; }
        public string PaymentID { get; set; }
        public Nullable<decimal> Price { get; set; }
        public Nullable<int> Quantity { get; set; }
        public Nullable<int> isActive { get; set; }
        public Nullable<int> Sold { get; set; }
        public Nullable<System.DateTime> DateCreated { get; set; }
        public Nullable<System.DateTime> DateUpdated { get; set; }
    }
}