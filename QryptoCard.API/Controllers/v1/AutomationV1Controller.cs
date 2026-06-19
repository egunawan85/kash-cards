using Newtonsoft.Json;
using QryptoCard.API.AutomationService;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.ServiceModel;
using System.Web.Http;

namespace QryptoCard.API.Controllers.v1
{
    [RoutePrefix("v1/address")]
    public class AutomationV1Controller : ApiController
    {
        AutomationV1ServiceClient sr = new AutomationV1ServiceClient();

        private void trustConnection()
        {
            ServicePointManager.ServerCertificateValidationCallback +=
                (se, cert, chain, sslerror) =>
                {
                    return true;
                };
        }

        [Route("add/address")]
        [HttpPost]
        public void addAddress(string[][] data)
        {
            try
            {
                trustConnection();
                sr.InsertAddress(data);
            }
            catch (Exception ex)
            {
                //op.Status = "error";
                //op.Message = ex.Message;
                //op.Data = null;
            }

            return;
        }
    }
}
