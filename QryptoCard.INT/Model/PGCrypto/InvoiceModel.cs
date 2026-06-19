using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace QryptoCard.INT.Model.PGCrypto
{
    public class InvoiceModel
    {
        public long ID { get; set; }
        public string UUID { get; set; }
        public string InvoiceID { get; set; }
        public string CustomerID { get; set; }
        public string CustomerName { get; set; }
        public string CustomerEmail { get; set; }
        public string CoinID { get; set; }
        public string Coin { get; set; }
        public Nullable<int> isToken { get; set; }
        public string TokenID { get; set; }
        public string Token { get; set; }
        public string Symbol { get; set; }
        public string NetworkType { get; set; }
        public string Address { get; set; }
        public string Currency { get; set; }
        public Nullable<decimal> Subtotal { get; set; }
        public Nullable<int> isDiscountFixed { get; set; }
        public Nullable<decimal> Discount { get; set; }
        public Nullable<double> DiscountInPercentage { get; set; }
        public Nullable<int> isTaxFixed { get; set; }
        public Nullable<decimal> Tax { get; set; }
        public Nullable<double> TaxInPercentage { get; set; }
        public Nullable<decimal> Total { get; set; }
        public Nullable<decimal> Commision { get; set; }
        public Nullable<double> CommisionInPercentage { get; set; }
        public Nullable<decimal> NetworkFee { get; set; }
        public Nullable<decimal> TotalReceive { get; set; }
        public string Notes { get; set; }
        public Nullable<int> isActive { get; set; }
        public Nullable<int> isPaid { get; set; }
        public string StatusMessage { get; set; }
        public string Status { get; set; }
        public Nullable<System.DateTime> DateCreated { get; set; }
        public Nullable<System.DateTime> DateModified { get; set; }
        public Nullable<System.DateTime> DateExpired { get; set; }
        public string PartnerReferenceID { get; set; }
        public string PaymentURL { get; set; }
        public string ReceiptURL { get; set; }
        public string Description { get; set; }
        public List<ProductModel> Products { get; set; }
    }
}