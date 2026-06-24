using Newtonsoft.Json;
using QryptoCard.API.Admin.DashboardV1Service;
using QryptoCard.API.Admin.Models.Service;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Web;
using System.Web.Http;

namespace QryptoCard.API.Admin.Controllers.v1
{
    [RoutePrefix("v1/dashboard")]
    [BearerAuthentication]
    public class DashboardV1Controller : QryptoCardApiController
    {
        DashboardV1ServiceClient sr = new DashboardV1ServiceClient();
        OutputModel op = new OutputModel();


        [Route("data")]
        [HttpGet]
        public OutputModel getTypes()
        {
            try
            {
                DashboardAdminModel x = new DashboardAdminModel();
                op = sr.getDashboardData(getEmail(), x);
                if (op.Status == "success")
                    op.Data = JsonConvert.DeserializeObject<DashboardAdminModel>(op.Data.ToString());
            }
            catch (Exception ex)
            {
                op.Status = "error";
                op.Message = ex.Message;
                op.Data = null;
            }

            return op;
        }

        [Route("card/trx")]
        [HttpGet]
        public OutputModel getCardTrx()
        {
            try
            {
                op = sr.get10ActiveCardTransaction(getEmail());
                if (op.Status == "success")
                    op.Data = JsonConvert.DeserializeObject<List<CardModel>>(op.Data.ToString());
            }
            catch (Exception ex)
            {
                op.Status = "error";
                op.Message = ex.Message;
                op.Data = null;
            }

            return op;
        }
    }
}
