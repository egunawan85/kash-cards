using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Web;
using System.Web.Http;
using Newtonsoft.Json;
using QryptoCard.API.Models;
using QryptoCard.API.UserV1Service;

namespace QryptoCard.API.Controllers.v1
{
    [RoutePrefix("v1/user")]
    [BasicAuthentication]
    public class UserV1Controller : ApiController
    {
        UserV1ServiceClient sr = new UserV1ServiceClient();
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


        [Route("dashboard/data")]
        [HttpGet]
        public OutputModel depositCardList()
        {
            try
            {
                var x = new DashboardModel();
                trustConnection();
                op = sr.getDashboardData(getKey(), x);
                if (op.Status == "success")
                    op.Data = JsonConvert.DeserializeObject<DashboardModel>(op.Data.ToString());
            }
            catch (Exception ex)
            {
                op.Status = "error";
                op.Message = ex.Message;
                op.Data = null;
            }

            return op;
        }


        [Route("referral/code")]
        [HttpPost]
        public OutputModel depositCardList(tblM_User_Referral x)
        {
            try
            {
                trustConnection();
                op = sr.getReferralCode(getKey(), x);
                if (op.Status == "success")
                    op.Data = JsonConvert.DeserializeObject<tblM_User_Referral>(op.Data.ToString());
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
        public OutputModel depositCardList(tblM_User_Balance x)
        {
            try
            {
                trustConnection();
                op = sr.getBalance(getKey(), x);
                if (op.Status == "success")
                    op.Data = JsonConvert.DeserializeObject<tblM_User_Balance>(op.Data.ToString());
            }
            catch (Exception ex)
            {
                op.Status = "error";
                op.Message = ex.Message;
                op.Data = null;
            }

            return op;
        }

        [Route("otp/generate")]
        [HttpGet]
        public OutputModel otpg(tblM_User_Balance x)
        {
            try
            {
                trustConnection();
                op = sr.generateKeyOTP(getKey());
            }
            catch (Exception ex)
            {
                op.Status = "error";
                op.Message = ex.Message;
                op.Data = null;
            }

            return op;
        }

        [Route("otp/validate")]
        [HttpPost]
        public OutputModel otpv(tblH_User_OTP x)
        {
            try
            {
                trustConnection();
                op = sr.validateKeyOTP(getKey(), x);
            }
            catch (Exception ex)
            {
                op.Status = "error";
                op.Message = ex.Message;
                op.Data = null;
            }

            return op;
        }

        [Route("referral/joined")]
        [HttpGet]
        public OutputModel getReferralJoined()
        {
            try
            {
                var x = new DashboardModel();
                trustConnection();
                op = sr.getReferralJoined(getKey());
                if (op.Status == "success")
                    op.Data = JsonConvert.DeserializeObject<List<tblM_User>>(op.Data.ToString());
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
