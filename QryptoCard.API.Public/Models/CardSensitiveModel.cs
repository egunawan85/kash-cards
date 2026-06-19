using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace QryptoCard.API.Public.Models
{
    public class CardSensitiveModel
    {
        public string ID { get; set; }
        public Nullable<long> CardTypeId { get; set; }
        public string CardNumber { get; set; }
        public string CVV { get; set; }
        public string ValidPeriod { get; set; }
    }
}