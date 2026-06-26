using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Web;
using System.Web.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using QryptoCard.API.Models;
using QryptoCard.API.UserV1Service;

namespace QryptoCard.API.Controllers.v1
{
    [RoutePrefix("v1/user")]
    [BearerAuthentication]
    public class UserV1Controller : QryptoCardApiController
    {
        UserV1ServiceClient sr = new UserV1ServiceClient();
        OutputModel op = new OutputModel();



        [Route("dashboard/data")]
        [HttpGet]
        public OutputModel depositCardList()
        {
            try
            {
                var x = new DashboardModel();
                op = sr.getDashboardData(getEmail(), x);
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
                op = sr.getReferralCode(getEmail(), x);
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
                op = sr.getBalance(getEmail(), x);
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

        // Returns the authenticated caller's own static deposit address (+ network/coin for
        // a QR payload). The identity comes from the bearer token via getEmail(), never from
        // a client-supplied id, so a caller can only ever read their own address (IDOR-safe).
        [Route("deposit/address")]
        [HttpGet]
        public OutputModel getDepositAddress()
        {
            try
            {
                op = sr.getDepositAddress(getEmail());
                if (op.Status == "success")
                    op.Data = JsonConvert.DeserializeObject<JObject>(op.Data.ToString());
            }
            catch (Exception ex)
            {
                op.Status = "error";
                op.Message = ex.Message;
                op.Data = null;
            }

            return op;
        }

        // Returns a page of the authenticated caller's own prepaid-balance ledger. Scoped to
        // the caller via getEmail() (IDOR-safe); paging is clamped server-side in the INT tier.
        [Route("ledger")]
        [HttpGet]
        public OutputModel getLedger(int page = 1, int pageSize = 20)
        {
            try
            {
                op = sr.getLedger(getEmail(), page, pageSize);
                if (op.Status == "success")
                    op.Data = JsonConvert.DeserializeObject<JObject>(op.Data.ToString());
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
                op = sr.generateKeyOTP(getEmail());
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
                op = sr.validateKeyOTP(getEmail(), x);
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
                op = sr.getReferralJoined(getEmail());
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
