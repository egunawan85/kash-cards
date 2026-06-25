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
    [RoutePrefix("v1/transaction")]
    [BasicAuthentication]
    public class TransactionV1Controller : ApiController
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


        [Route("card/purchase/list")]
        [HttpGet]
        public OutputModel getTypes()
        {
            try
            {
                tblT_Card x = new tblT_Card();
                op = sr.getCardListAll(getKey(), x);
                if (op.Status == "success")
                    op.Data = JsonConvert.DeserializeObject<List<CardPurchaseModel>>(op.Data.ToString());
            }
            catch (Exception ex)
            {
                op.Status = "error";
                op.Message = ex.Message;
                op.Data = null;
            }

            return op;
        }

        [Route("card/purchase/detail")]
        [HttpPost]
        public OutputModel getCardPurchaseDetail(tblT_Card x)
        {
            try
            {
                op = sr.getCardPurchaseDetail(getKey(), x);
                if (op.Status == "success")
                    op.Data = JsonConvert.DeserializeObject<CardPurchaseModel>(op.Data.ToString());
            }
            catch (Exception ex)
            {
                op.Status = "error";
                op.Message = ex.Message;
                op.Data = null;
            }

            return op;
        }

        [Route("card/purchase")]
        [HttpPost]
        public OutputModel purchaseCard(tblT_Card x)
        {
            try
            {
                op = sr.openCard(getKey(), x);
                if (op.Status == "success")
                    op.Data = JsonConvert.DeserializeObject<CardPurchaseModel>(op.Data.ToString());
            }
            catch (Exception ex)
            {
                op.Status = "error";
                op.Message = ex.Message;
                op.Data = null;
            }

            return op;
        }

        [Route("card/purchase")]
        [HttpDelete]
        public OutputModel cancelPurchaseCard(tblT_Card x)
        {
            try
            {
                op = sr.cancelCardPurchase(getKey(), x);
                if (op.Status == "success")
                    op.Data = JsonConvert.DeserializeObject<CardPurchaseModel>(op.Data.ToString());
            }
            catch (Exception ex)
            {
                op.Status = "error";
                op.Message = ex.Message;
                op.Data = null;
            }

            return op;
        }



        [Route("card/deposit/list")]
        [HttpPost]
        public OutputModel depositList(tblT_Card x)
        {
            try
            {
                op = sr.getCardDepositList(getKey(), x);
                if (op.Status == "success")
                    op.Data = JsonConvert.DeserializeObject<List<CardDepositModel>>(op.Data.ToString());
            }
            catch (Exception ex)
            {
                op.Status = "error";
                op.Message = ex.Message;
                op.Data = null;
            }

            return op;
        }

        [Route("card/deposit/detail")]
        [HttpPost]
        public OutputModel getCardDepositDetail(tblT_Card_Deposit x)
        {
            try
            {
                op = sr.getCardDepositDetail(getKey(), x);
                if (op.Status == "success")
                    op.Data = JsonConvert.DeserializeObject<CardDepositModel>(op.Data.ToString());
            }
            catch (Exception ex)
            {
                op.Status = "error";
                op.Message = ex.Message;
                op.Data = null;
            }

            return op;
        }

        [Route("card/deposit")]
        [HttpPost]
        public OutputModel depositCard(tblT_Card_Deposit x)
        {
            try
            {
                op = sr.depositCard(getKey(), x);
                if (op.Status == "success")
                    op.Data = JsonConvert.DeserializeObject<CardDepositModel>(op.Data.ToString());
            }
            catch (Exception ex)
            {
                op.Status = "error";
                op.Message = ex.Message;
                op.Data = null;
            }

            return op;
        }

        [Route("card/deposit")]
        [HttpDelete]
        public OutputModel cancelDepositard(tblT_Card_Deposit x)
        {
            try
            {
                op = sr.cancelDepositTransaction(getKey(), x);
                if (op.Status == "success")
                    op.Data = JsonConvert.DeserializeObject<CardDepositModel>(op.Data.ToString());
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
