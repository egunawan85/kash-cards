using Newtonsoft.Json;
using QryptoCard.API.CardV1Service;
using QryptoCard.API.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Web;
using System.Web.Http;

namespace QryptoCard.API.Controllers.v1
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


        [Route("holder/detail")]
        [HttpPost]
        public OutputModel getCardList(tblM_Cardholder x)
        {
            try
            {
                trustConnection();
                op = sr.getHolderDetail(getKey(), x);
                if (op.Status == "success")
                    op.Data = JsonConvert.DeserializeObject<tblM_Cardholder>(op.Data.ToString());
            }
            catch (Exception ex)
            {
                op.Status = "error";
                op.Message = ex.Message;
                op.Data = null;
            }

            return op;
        }


        [Route("holder/check/cardtypeid")]
        [HttpPost]
        public OutputModel checkHolder(tblM_Cardholder x)
        {
            try
            {
                trustConnection();
                op = sr.checkHolderByCardTypeId(getKey(), x);
                if (op.Status == "success")
                    op.Data = JsonConvert.DeserializeObject<tblM_Cardholder>(op.Data.ToString());
            }
            catch (Exception ex)
            {
                op.Status = "error";
                op.Message = ex.Message;
                op.Data = null;
            }

            return op;
        }


        [Route("open")]
        [HttpPost]
        public OutputModel openCard(tblT_Card x)
        {
            try
            {
                trustConnection();
                op = sr.openCard(getKey(), x);
                if (op.Status == "success")
                    op.Data = JsonConvert.DeserializeObject<tblT_Card>(op.Data.ToString());
            }
            catch (Exception ex)
            {
                op.Status = "error";
                op.Message = ex.Message;
                op.Data = null;
            }

            return op;
        }


        [Route("list")]
        [HttpPost]
        public OutputModel getCardList(tblT_Card x)
        {
            try
            {
                trustConnection();
                op = sr.getCardList(getKey(), x);
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

        [Route("list/all")]
        [HttpPost]
        public OutputModel getCardListAll(tblT_Card x)
        {
            try
            {
                trustConnection();
                op = sr.getCardListAll(getKey(), x);
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


        [Route("detail/id")]
        [HttpPost]
        public OutputModel getCardByID(vw_Card x)
        {
            try
            {
                trustConnection();
                op = sr.getCardDetail(getKey(), x);
                if (op.Status == "success")
                    op.Data = JsonConvert.DeserializeObject<vw_Card>(op.Data.ToString());
            }
            catch (Exception ex)
            {
                op.Status = "error";
                op.Message = ex.Message;
                op.Data = null;
            }

            return op;
        }

        [Route("trx/cancel")]
        [HttpPost]
        public OutputModel cancelCardTrx(vw_Card x)
        {
            try
            {
                trustConnection();
                op = sr.cancelCardTransaction(getKey(), x);
                //if (op.Status == "success")
                //    op.Data = JsonConvert.DeserializeObject<vw_Card>(op.Data.ToString());
            }
            catch (Exception ex)
            {
                op.Status = "error";
                op.Message = ex.Message;
                op.Data = null;
            }

            return op;
        }

        [Route("deposit")]
        [HttpPost]
        public OutputModel depositCard(tblT_Card_Deposit x)
        {
            try
            {
                trustConnection();
                op = sr.depositCard(getKey(), x);
                if (op.Status == "success")
                    op.Data = JsonConvert.DeserializeObject<tblT_Card_Deposit>(op.Data.ToString());
            }
            catch (Exception ex)
            {
                op.Status = "error";
                op.Message = ex.Message;
                op.Data = null;
            }

            return op;
        }

        [Route("deposit/detail")]
        [HttpPost]
        public OutputModel depositCardDetail(tblT_Card_Deposit x)
        {
            try
            {
                trustConnection();
                op = sr.getCardDepositDetail(getKey(), x);
                if (op.Status == "success")
                    op.Data = JsonConvert.DeserializeObject<tblT_Card_Deposit>(op.Data.ToString());
            }
            catch (Exception ex)
            {
                op.Status = "error";
                op.Message = ex.Message;
                op.Data = null;
            }

            return op;
        }

        [Route("deposit/list")]
        [HttpPost]
        public OutputModel depositCardList(tblT_Card x)
        {
            try
            {
                trustConnection();
                op = sr.getCardDepositList(getKey(), x);
                if (op.Status == "success")
                    op.Data = JsonConvert.DeserializeObject<List<tblT_Card_Deposit>>(op.Data.ToString());
            }
            catch (Exception ex)
            {
                op.Status = "error";
                op.Message = ex.Message;
                op.Data = null;
            }

            return op;
        }

        [Route("deposit/cancel")]
        [HttpPost]
        public OutputModel cancelCardDeposit(tblT_Card_Deposit x)
        {
            try
            {
                trustConnection();
                op = sr.cancelDepositTransaction(getKey(), x);
                //if (op.Status == "success")
                //    op.Data = JsonConvert.DeserializeObject<vw_Card>(op.Data.ToString());
            }
            catch (Exception ex)
            {
                op.Status = "error";
                op.Message = ex.Message;
                op.Data = null;
            }

            return op;
        }

        [Route("trx/list")]
        [HttpPost]
        public OutputModel getCardTransaction(tblT_Card x)
        {
            try
            {
                trustConnection();
                op = sr.getCardTransaction(getKey(), x);
                if (op.Status == "success")
                    op.Data = JsonConvert.DeserializeObject<List<tblT_Card_Transaction>>(op.Data.ToString());
            }
            catch (Exception ex)
            {
                op.Status = "error";
                op.Message = ex.Message;
                op.Data = null;
            }

            return op;
        }

        [Route("trx/detail")]
        [HttpPost]
        public OutputModel getCardTransactionDetail(tblT_Card_Transaction x)
        {
            try
            {
                trustConnection();
                op = sr.getCardTransactionDetail(getKey(), x);
                if (op.Status == "success")
                    op.Data = JsonConvert.DeserializeObject<tblT_Card_Transaction>(op.Data.ToString());
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
