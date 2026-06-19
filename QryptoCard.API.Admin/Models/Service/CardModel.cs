using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace QryptoCard.API.Admin.Models.Service
{
    public class CardModel
    {
        public string ID { get; set; }
        public string UserID { get; set; }
        public Nullable<long> CardTypeId { get; set; }
        public string OrderNo { get; set; }
        public string CardNo { get; set; }
        public string CardNumber { get; set; }
        public string Currency { get; set; }
        public Nullable<double> Price { get; set; }
        public Nullable<double> InitialDeposit { get; set; }
        public Nullable<double> Fee { get; set; }
        public Nullable<double> FeeInPercentage { get; set; }
        public Nullable<double> Total { get; set; }
        public Nullable<double> ReceivedAmount { get; set; }
        public string ReceivedCurrency { get; set; }
        public string Status { get; set; }
        public Nullable<System.DateTime> DateCreated { get; set; }
        public Nullable<int> isActive { get; set; }
        public Nullable<System.DateTime> DateModified { get; set; }
        public Nullable<int> isNeedCardholder { get; set; }
        public string AddressID { get; set; }
        public string Address { get; set; }
        public string Param1 { get; set; }
        public string Param2 { get; set; }
        public string Param3 { get; set; }
        public string Param4 { get; set; }
        public string Param5 { get; set; }
        public Nullable<long> HolderID { get; set; }
        public string AreaCode { get; set; }
        public string Mobile { get; set; }
        public string Email { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Birthday { get; set; }
        public string Country { get; set; }
        public string CountryStr { get; set; }
        public string State { get; set; }
        public string StateStr { get; set; }
        public string Town { get; set; }
        public string TownStr { get; set; }
        public string HolderAddress { get; set; }
        public string PostCode { get; set; }
        public string Organization { get; set; }
        public string Type { get; set; }
        public string CardName { get; set; }
        public string CardDesc { get; set; }
        public string Support { get; set; }
        public string RechargeFeeRate { get; set; }
        public string RechargeFee { get; set; }
        public string OriginalRechargeFeeRate { get; set; }
        public string OriginalRechargeFee { get; set; }
        public Nullable<System.DateTime> DateExpired { get; set; }
    }
}