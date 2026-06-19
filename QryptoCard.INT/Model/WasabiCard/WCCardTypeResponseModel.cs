using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace QryptoCard.INT.Model.WasabiCard
{
    public class WCCardTypeResponseModel
    {
        public bool success { get; set; }
        public int code { get; set; }
        public string msg { get; set; }
        public List<Datum> data { get; set; }
        
        public class Datum
        {
            public int cardTypeId { get; set; }
            public string organization { get; set; }
            public string country { get; set; }
            public string bankCardBin { get; set; }
            public string type { get; set; }
            public string typeStr { get; set; }
            public string cardName { get; set; }
            public string cardDesc { get; set; }
            public string cardPrice { get; set; }
            public string cardPriceCurrency { get; set; }
            public List<string> support { get; set; }
            public List<string> supportHolderRegin { get; set; }
            public List<string> supportHolderAreaCode { get; set; }
            public bool needCardHolder { get; set; }
            public bool needDepositForActiveCard { get; set; }
            public string depositAmountMinQuotaForActiveCard { get; set; }
            public string depositAmountMaxQuotaForActiveCard { get; set; }
            public string fiatCurrency { get; set; }
            public int maxCount { get; set; }
            public string status { get; set; }
            public ExtFieldVO extFieldVO { get; set; }
        }

        public class ExtFieldVO
        {
            public List<RechargeCurrencyInfo> rechargeCurrencyInfos { get; set; }
        }

        public class RechargeCurrencyInfo
        {
            public string currency { get; set; }
            public string rechargeMinQuota { get; set; }
            public string rechargeMaxQuota { get; set; }
            public string rechargeFeeRate { get; set; }
            public string rechargeFee { get; set; }
            public int digital { get; set; }
        }


    }
}