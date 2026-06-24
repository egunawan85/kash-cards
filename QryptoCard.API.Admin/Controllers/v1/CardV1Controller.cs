using Newtonsoft.Json;
using QryptoCard.API.Admin.CardV1Service;
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

        [Route("type")]
        [HttpGet]
        public OutputModel getTypes()
        {
            try
            {
                tblM_Card_Type x = new tblM_Card_Type();
                trustConnection();
                op = sr.CardType(x);
                if (op.Status == "success")
                    op.Data = JsonConvert.DeserializeObject<List<CardTypeModel>>(op.Data.ToString());
            }
            catch (Exception ex)
            {
                op.Status = "error";
                op.Message = ex.Message;
                op.Data = null;
            }

            return op;
        }

        [Route("type/id")]
        [HttpPost]
        public OutputModel getTypesById(tblM_Card_Type x)
        {
            try
            {
                trustConnection();
                op = sr.getCardTypeById(x);
                if (op.Status == "success")
                    op.Data = JsonConvert.DeserializeObject<CardTypeModel>(op.Data.ToString());
            }
            catch (Exception ex)
            {
                op.Status = "error";
                op.Message = ex.Message;
                op.Data = null;
            }

            return op;
        }

        [Route("active")]
        [HttpGet]
        public OutputModel getActiveCard()
        {
            try
            {
                trustConnection();
                op = sr.getActiveCard();
                if (op.Status == "success")
                    op.Data = JsonConvert.DeserializeObject<List<vw_Card>>(op.Data.ToString());
            }
            catch (Exception ex)
            {
                op.Status = "error";
                op.Message = ex.Message;
                op.Data = null;
            }

            return op;
        }

        [Route("all")]
        [HttpGet]
        public OutputModel getAllCard()
        {
            try
            {
                trustConnection();
                var x = new vw_Card();
                op = sr.getCardListAll(x);
                if (op.Status == "success")
                    op.Data = JsonConvert.DeserializeObject<List<vw_Card>>(op.Data.ToString());
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
        public OutputModel getCardPurchaseFilter(CardFilterModel x)
        {
            try
            {
                trustConnection();
                op = sr.getCardPurchaseFilter(getKey(), x);
                if (op.Status == "success")
                    op.Data = JsonConvert.DeserializeObject<List<vw_Card>>(op.Data.ToString());
            }
            catch (Exception ex)
            {
                op.Status = "error";
                op.Message = ex.Message;
                op.Data = null;
            }

            return op;
        }

        [Route("deposit/transaction")]
        [HttpPost]
        public OutputModel getDepositTrxFilter(DepositFilterModel x)
        {
            try
            {
                trustConnection();
                op = sr.getDepositTrxFilter(getKey(), x);
                if (op.Status == "success")
                    op.Data = JsonConvert.DeserializeObject<List<DepositModel>>(op.Data.ToString());
            }
            catch (Exception ex)
            {
                op.Status = "error";
                op.Message = ex.Message;
                op.Data = null;
            }

            return op;
        }

        [Route("type/price")]
        [HttpPut]
        public OutputModel updateCardPricex(tblM_Card_Type x)
        {
            try
            {
                trustConnection();
                op = sr.updateCardPrice(getKey(), x);
                //if (op.Status == "success")
                //    op.Data = JsonConvert.DeserializeObject<List<DepositModel>>(op.Data.ToString());
            }
            catch (Exception ex)
            {
                op.Status = "error";
                op.Message = ex.Message;
                op.Data = null;
            }

            return op;
        }

        [Route("type/deposit/fee")]
        [HttpPut]
        public OutputModel updateDepositFee(tblM_Card_Type x)
        {
            try
            {
                trustConnection();
                op = sr.updateCardDepositFee(getKey(), x);
                //if (op.Status == "success")
                //    op.Data = JsonConvert.DeserializeObject<List<DepositModel>>(op.Data.ToString());
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
