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
    [RoutePrefix("v1/master")]
    [BasicAuthentication]
    public class MasterV1Controller : ApiController
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

        [Route("card/type")]
        [HttpGet]
        public OutputModel getTypes()
        {
            try
            {
                tblM_Card_Type x = new tblM_Card_Type();
                trustConnection();
                op = sr.CardType(getKey(), x);
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

        [Route("card/type/detail")]
        [HttpPost]
        public OutputModel getTypeDetail(tblM_Card_Type x)
        {
            try
            {
                trustConnection();
                op = sr.getCardTypeById(getKey(), x);
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
    }
}
