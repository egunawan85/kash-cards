using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace QryptoCard.INT.Model.WasabiCard
{
    public class WCGetHolderResponseModel
    {
        public bool success { get; set; }
        public int code { get; set; }
        public string msg { get; set; }
        public Data data { get; set; }

        public class Data
        {
            public int total { get; set; }
            public List<Record> records { get; set; }
        }

        public class Record
        {
            public int holderId { get; set; }
            public int userId { get; set; }
            public string areaCode { get; set; }
            public string mobile { get; set; }
            public string email { get; set; }
            public string firstName { get; set; }
            public string lastName { get; set; }
            public string birthday { get; set; }
            public string country { get; set; }
            public string countryStr { get; set; }
            public string state { get; set; }
            public string stateStr { get; set; }
            public string town { get; set; }
            public string townStr { get; set; }
            public string address { get; set; }
            public string postCode { get; set; }
            public string status { get; set; }
            public string statusStr { get; set; }
            public object createTime { get; set; }
            public object updateTime { get; set; }
            public string respMsg { get; set; }
        }
    }
}