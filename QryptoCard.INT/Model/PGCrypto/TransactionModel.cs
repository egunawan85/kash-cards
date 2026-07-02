using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace QryptoCard.INT.Model.PGCrypto
{
    public class TransactionModel
    {
        public string TransactionID { get; set; }
        public string MerchantID { get; set; }       // merchant that owns the payment request (createPayment input)
        public string IdempotencyKey { get; set; }   // dedup on (Company,Merchant,key) — we set it = intentId
        public string PaymentType { get; set; }
        public string CoinID { get; set; }
        public string Coin { get; set; }
        public Nullable<int> isToken { get; set; }
        public string TokenID { get; set; }
        public string Token { get; set; }
        public string Symbol { get; set; }
        public string NetworkType { get; set; }
        public string Address { get; set; }
        public Nullable<decimal> Amount { get; set; }
        public string Currency { get; set; }
        public Nullable<decimal> Commision { get; set; }
        public Nullable<double> CommisionInPercentage { get; set; }
        public Nullable<decimal> Total { get; set; }
        public Nullable<int> isActive { get; set; }
        public Nullable<int> isPaid { get; set; }
        public string StatusMessage { get; set; }
        public string Status { get; set; }
        public Nullable<System.DateTime> DateCreated { get; set; }
        public Nullable<System.DateTime> DateExpired { get; set; }
        public string PartnerReferenceID { get; set; }
        public string ReceiptURL { get; set; }
    }
}