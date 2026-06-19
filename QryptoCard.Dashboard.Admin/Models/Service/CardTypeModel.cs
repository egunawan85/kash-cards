using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace QryptoCard.Dashboard.Admin.Models.Service
{
    public class CardTypeModel
    {
        public long ID { get; set; }
        public Nullable<long> CardTypeId { get; set; }
        public string Organization { get; set; }
        public string Country { get; set; }
        public string BankCardBin { get; set; }
        public string Type { get; set; }
        public string TypeStr { get; set; }
        public string CardName { get; set; }
        public string CardDesc { get; set; }
        public string CardPrice { get; set; }
        public string CardPriceCurrency { get; set; }
        public string OriginalCardPrice { get; set; }
        public string OriginalCardPriceCurrency { get; set; }
        public string RechargeFeeRate { get; set; }
        public string RechargeFee { get; set; }
        public string OriginalRechargeFeeRate { get; set; }
        public string OriginalRechargeFee { get; set; }
        public string RechargeMinQuota { get; set; }
        public string RechargeMaxQuota { get; set; }
        public string Support { get; set; }
        public string SupportHolderRegin { get; set; }
        public string SupportHolderAreaCode { get; set; }
        public Nullable<int> NeedCardHolder { get; set; }
        public Nullable<int> NeedDepositForActiveCard { get; set; }
        public string DepositAmountMinQuotaForActiveCard { get; set; }
        public string DepositAmountMaxQuotaForActiveCard { get; set; }
        public string FiatCurrency { get; set; }
        public Nullable<int> MaxCount { get; set; }
        public string Status { get; set; }
        public Nullable<int> isActive { get; set; }
        public Nullable<System.DateTime> DateCreated { get; set; }
    }
}