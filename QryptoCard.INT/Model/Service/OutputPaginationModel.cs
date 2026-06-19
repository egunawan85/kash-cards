using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace QryptoCard.INT.Model.Service
{
    public class OutputPaginationModel
    {
        public string Status { get; set; }
        public object Data { get; set; }
        public string Message { get; set; }
        public object Pagination { get; set; }
    }
}