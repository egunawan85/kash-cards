using QryptoCard.API.Public.CardV1Service;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using System.Text;
using System.Web;
using Newtonsoft.Json;
using QryptoCard.API.Public.Models;

namespace QryptoCard.API.Public.Controllers.v1
{
    [RoutePrefix("v1/card")]
    [BasicAuthentication]
    public class CardV1Controller : ApiController
    {
        CardV1ServiceClient sr = new CardV1ServiceClient();
        OutputModel op = new OutputModel();

        private string getKey()
        {
            // Gets header parameters  
            HttpContext httpContext = HttpContext.Current;
            string authenticationString = httpContext.Request.Headers["Authorization"];
            string originalString = Encoding.UTF8.GetString(Convert.FromBase64String(authenticationString.Split(' ')[1]));
            return originalString.Split(':')[0];
        }

        private void trustConnection()
        {
            ServicePointManager.ServerCertificateValidationCallback +=
                (se, cert, chain, sslerror) =>
                {
                    return true;
                };
        }

        [Route("active")]
        [HttpGet]
        public OutputModel getTypes()
        {
            try
            {
                tblT_Card x = new tblT_Card();
                trustConnection();
                op = sr.getCardListActive(getKey(), x);
                if (op.Status == "success")
                    op.Data = JsonConvert.DeserializeObject<List<CardActiveModel>>(op.Data.ToString());
            }
            catch (Exception ex)
            {
                op.Status = "error";
                op.Message = ex.Message;
                op.Data = null;
            }

            return op;
        }

        [Route("detail")]
        [HttpPost]
        public OutputModel getCardDetail(tblT_Card x)
        {
            try
            {
                trustConnection();
                op = sr.getCardDetail(getKey(), x);
                if (op.Status == "success")
                    op.Data = JsonConvert.DeserializeObject<CardActiveModel>(op.Data.ToString());
            }
            catch (Exception ex)
            {
                op.Status = "error";
                op.Message = ex.Message;
                op.Data = null;
            }

            return op;
        }

        [Route("detail/sensitive")]
        [HttpPost]
        public OutputModel getCardDetailSensitive(tblT_Card x)
        {
            try
            {
                trustConnection();
                op = sr.getCardDetail(getKey(), x);
                if (op.Status == "success")
                    op.Data = JsonConvert.DeserializeObject<CardSensitiveModel>(op.Data.ToString());
            }
            catch (Exception ex)
            {
                op.Status = "error";
                op.Message = ex.Message;
                op.Data = null;
            }

            return op;
        }

        [Route("balance")]
        [HttpPost]
        public OutputModel getCardBalance(CardBalanceModel x)
        {
            try
            {
                trustConnection();
                op = sr.getCardBalance(getKey(), x);
                if (op.Status == "success")
                    op.Data = JsonConvert.DeserializeObject<CardBalanceModel>(op.Data.ToString());
            }
            catch (Exception ex)
            {
                op.Status = "error";
                op.Message = ex.Message;
                op.Data = null;
            }

            return op;
        }

        [Route("transaction")]
        [HttpPost]
        public OutputModel getCardTransaction(tblT_Card x)
        {
            try
            {
                trustConnection();
                op = sr.getCardTransaction(getKey(), x);
                if (op.Status == "success")
                    op.Data = JsonConvert.DeserializeObject<List<CardTransactionModel>>(op.Data.ToString());
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
