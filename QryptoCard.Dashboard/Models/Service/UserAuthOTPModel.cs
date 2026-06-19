using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace QryptoCard.Dashboard.Models.Service
{
    public class UserAuthOTPModel
    {
        public string ID { get; set; }
        public string UserID { get; set; }
        public string Code { get; set; }
        public string Longitude { get; set; }
        public string Latitude { get; set; }
        public string Altitude { get; set; }
        public string IPAddress { get; set; }
        public Nullable<System.DateTime> DateCreated { get; set; }
        public Nullable<System.DateTime> DateExpired { get; set; }
        public Nullable<int> isVerify { get; set; }
        public string DeviceType { get; set; }
        public string DeviceName { get; set; }
        public string Manufacturer { get; set; }
        public string Brand { get; set; }
        public string Model { get; set; }
        public string IMEI { get; set; }
        public string MEID { get; set; }
        public string Param1 { get; set; }
        public string Param2 { get; set; }
        public string Param3 { get; set; }
    }
}