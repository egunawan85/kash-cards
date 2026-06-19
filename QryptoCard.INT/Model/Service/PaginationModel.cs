using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace QryptoCard.INT.Model.Service
{
    public class PaginationModel
    {
        public int Page { get; set; }
        public int Limit { get; set; }
        public int TotalPage { get; set; }
    }
}