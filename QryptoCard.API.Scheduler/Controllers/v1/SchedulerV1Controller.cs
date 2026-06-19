using QryptoCard.API.Scheduler.SchedulerV1Service;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;

namespace QryptoCard.API.Scheduler.Controllers.v1
{
    [RoutePrefix("v1/secure")]

    public class SchedulerV1Controller : ApiController
    {
        SchedulerV1ServiceClient sr = new SchedulerV1ServiceClient();

        private void trustConnection()
        {
            ServicePointManager.ServerCertificateValidationCallback +=
                (se, cert, chain, sslerror) =>
                {
                    return true;
                };
        }

        
        [Route("expired/transaction")]
        [HttpGet]
        public void checkExpiredTransaction()
        {
            try
            {
                trustConnection();
                sr.checkExpiredTransaction();
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
