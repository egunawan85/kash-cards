using Newtonsoft.Json;
using QryptoCard.API.Callback.CallbackV1Service;
using QryptoCard.API.Callback.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;

namespace QryptoCard.API.Callback.Controllers.v1
{
    [RoutePrefix("v1/payment")]
    public class CallbackV1Controller : ApiController
    {
        CallbackV1ServiceClient sr = new CallbackV1ServiceClient();
        //OutputModel op = new OutputModel();

        private string getWSBCategory()
        {
            // Gets header parameters  
            HttpContext httpContext = HttpContext.Current;
            return httpContext.Request.Headers["X-WSB-CATEGORY"];
        }
        private string getWSBSignature()
        {
            // Gets header parameters  
            HttpContext httpContext = HttpContext.Current;
            return httpContext.Request.Headers["X-WSB-SIGNATURE"];
        }
        private string getWSBRequestID()
        {
            // Gets header parameters  
            HttpContext httpContext = HttpContext.Current;
            return httpContext.Request.Headers["X-WSB-REQUEST-ID"];
        }

        private void trustConnection()
        {
            ServicePointManager.ServerCertificateValidationCallback +=
                (se, cert, chain, sslerror) =>
                {
                    return true;
                };
        }

        [Route("wasabicard")]
        [HttpPost]
        public async Task<HttpResponseMessage> wasabi(object x)
        {
            try
            {
                trustConnection();
                var z = x.ToString();
                sr.Wasabi(getWSBCategory(), getWSBSignature(), getWSBRequestID(), x.ToString());

                WasabiResponseModel responseModel = new WasabiResponseModel();
                responseModel.success = true;
                responseModel.msg = "success";
                responseModel.code = 200;

                var response = new HttpResponseMessage();
                response.EnsureSuccessStatusCode();
                response.StatusCode = HttpStatusCode.OK;
                response.Content = new StringContent(JsonConvert.SerializeObject(responseModel), Encoding.UTF8, "application/json");
                return response;
            }
            catch (Exception ex)
            {
            }

            return null;
        }

        [Route("pgcrypto")]
        [HttpPost]
        public void pgcrypto([FromBody] PGCryptoModel x)
        {
            try
            {
                trustConnection();
                sr.PGCrypto(x);
            }
            catch (Exception ex)
            {
            }

            return;
        }
    }
}
