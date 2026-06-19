using Newtonsoft.Json;
using QryptoCard.API.Admin.AdminV1Service;
using QryptoCard.API.Admin.Models.Service;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;

namespace QryptoCard.API.Admin.Controllers.v1
{
    [RoutePrefix("v1/auth")]
    public class AuthV1Controller : ApiController
    {
        AdminV1ServiceClient sr = new AdminV1ServiceClient();
        OutputModel op = new OutputModel();

        private void trustConnection()
        {
            ServicePointManager.ServerCertificateValidationCallback +=
                (se, cert, chain, sslerror) =>
                {
                    return true;
                };
        }

        [Route("login")]
        [HttpPost]
        public OutputModel login(tblM_Admin x)
        {
            try
            {
                trustConnection();
                op = sr.Login(x);
            }
            catch (Exception ex)
            {
                op.Status = "error";
                op.Message = ex.Message;
                op.Data = null;
            }

            return op;
        }

        [Route("login/resend")]
        [HttpPost]
        public OutputModel regenrerateOTP(tblH_Admin_Login x)
        {
            try
            {
                trustConnection();
                op = sr.regenerateOTP(x);
            }
            catch (Exception ex)
            {
                op.Status = "error";
                op.Message = ex.Message;
                op.Data = null;
            }

            return op;
        }

        [Route("login/verify")]
        [HttpPost]
        public OutputModel loginVerify(tblH_Admin_Login x)
        {
            try
            {
                trustConnection();
                op = sr.LoginVerify(x);
                if (op.Status == "success")
                    op.Data = JsonConvert.DeserializeObject<vw_Admin>(op.Data.ToString());
            }
            catch (Exception ex)
            {
                op.Status = "error";
                op.Message = ex.Message;
                op.Data = null;
            }

            return op;
        }

        [Route("password/forgot")]
        [HttpPost]
        public OutputModel forgotPassword(tblM_Admin x)
        {
            try
            {
                trustConnection();
                op = sr.forgotPassword(x);
                //if (op.Status == "success")
                //    op.Data = JsonConvert.DeserializeObject<vw_Admin>(op.Data.ToString());
            }
            catch (Exception ex)
            {
                op.Status = "error";
                op.Message = ex.Message;
                op.Data = null;
            }

            return op;
        }

        [Route("password/forgot/check")]
        [HttpPost]
        public OutputModel checkforgotPassword(tblT_Admin_ForgotPassword x)
        {
            try
            {
                trustConnection();
                op = sr.checkForgotPassword(x);
                //if (op.Status == "success")
                //    op.Data = JsonConvert.DeserializeObject<vw_Admin>(op.Data.ToString());
            }
            catch (Exception ex)
            {
                op.Status = "error";
                op.Message = ex.Message;
                op.Data = null;
            }

            return op;
        }

        [Route("password/forgot/change")]
        [HttpPost]
        public OutputModel changeforgotPassword(tblT_Admin_ForgotPassword x)
        {
            try
            {
                trustConnection();
                op = sr.changePassword(x);
                //if (op.Status == "success")
                //    op.Data = JsonConvert.DeserializeObject<vw_Admin>(op.Data.ToString());
            }
            catch (Exception ex)
            {
                op.Status = "error";
                op.Message = ex.Message;
                op.Data = null;
            }

            return op;
        }


        [Route("invited/check")]
        [HttpPost]
        public OutputModel getInvitedAccount(tblM_Admin x)
        {
            try
            {
                trustConnection();
                op = sr.getInvitedAdmin(x);
                if (op.Status == "success")
                    op.Data = JsonConvert.DeserializeObject<AdminModel>(op.Data.ToString());
            }
            catch (Exception ex)
            {
                op.Status = "error";
                op.Message = ex.Message;
                op.Data = null;
            }

            return op;
        }


        [Route("invited/onboarding")]
        [HttpPut]
        public OutputModel updateInvitedAccount(tblM_Admin x)
        {
            try
            {
                trustConnection();
                op = sr.updateInvitedAdmin(x);
                //if (op.Status == "success")
                //    op.Data = JsonConvert.DeserializeObject<AdminModel>(op.Data.ToString());
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
