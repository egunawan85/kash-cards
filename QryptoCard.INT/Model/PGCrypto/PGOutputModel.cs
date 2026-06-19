using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace QryptoCard.INT.Model.PGCrypto
{
    public class PGOutputModel
    {
        public string Status { get; set; }
        public string Message { get; set; }
        public object Data { get; set; }
    }
}