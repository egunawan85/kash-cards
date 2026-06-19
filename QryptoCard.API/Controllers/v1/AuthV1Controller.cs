using Newtonsoft.Json;
using QryptoCard.API.UserV1Service;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;

namespace QryptoCard.API.Controllers.v1
{
    [RoutePrefix("v1/auth")]
    public class AuthV1Controller : ApiController
    {
        UserV1ServiceClient sr = new UserV1ServiceClient();
        OutputModel op = new OutputModel();

        private void trustConnection()
        {
            ServicePointManager.ServerCertificateValidationCallback +=
                (se, cert, chain, sslerror) =>
                {
                    return true;
                };
        }

        [Route("register")]
        [HttpPost]
        public OutputModel register(tblM_User x)
        {
            try
            {
                trustConnection();
                op = sr.Register(x);
            }
            catch (Exception ex)
            {
                op.Status = "error";
                op.Message = ex.Message;
                op.Data = null;
            }

            return op;
        }



        [Route("register/resend")]
        [HttpPost]
        public OutputModel regenerateOTPRegister(tblH_User_Register x)
        {
            try
            {
                trustConnection();
                op = sr.regenerateOTPRegister(x);
            }
            catch (Exception ex)
            {
                op.Status = "error";
                op.Message = ex.Message;
                op.Data = null;
            }

            return op;
        }

        [Route("register/verify")]
        [HttpPost]
        public OutputModel registerVerify(tblH_User_Register x)
        {
            try
            {
                trustConnection();
                op = sr.RegisterVerify(x);
                if (op.Status == "success")
                    op.Data = JsonConvert.DeserializeObject<tblM_User>(op.Data.ToString());
            }
            catch (Exception ex)
            {
                op.Status = "error";
                op.Message = ex.Message;
                op.Data = null;
            }

            return op;
        }




        [Route("login")]
        [HttpPost]
        public OutputModel login(tblM_User x)
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
        public OutputModel regenrerateOTP(tblH_User_Login x)
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
        public OutputModel loginVerify(tblH_User_Login x)
        {
            try
            {
                trustConnection();
                op = sr.LoginVerify(x);
                if (op.Status == "success")
                    op.Data = JsonConvert.DeserializeObject<tblM_User>(op.Data.ToString());
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
        public OutputModel forgotPassword(tblM_User x)
        {
            try
            {
                trustConnection();
                op = sr.forgotPassword(x);
                //if (op.Status == "success")
                //    op.Data = JsonConvert.DeserializeObject<vw_User>(op.Data.ToString());
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
        public OutputModel checkforgotPassword(tblT_User_ForgotPassword x)
        {
            try
            {
                trustConnection();
                op = sr.checkForgotPassword(x);
                //if (op.Status == "success")
                //    op.Data = JsonConvert.DeserializeObject<vw_User>(op.Data.ToString());
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
        public OutputModel changeforgotPassword(tblT_User_ForgotPassword x)
        {
            try
            {
                trustConnection();
                op = sr.changePassword(x);
                //if (op.Status == "success")
                //    op.Data = JsonConvert.DeserializeObject<vw_User>(op.Data.ToString());
            }
            catch (Exception ex)
            {
                op.Status = "error";
                op.Message = ex.Message;
                op.Data = null;
            }

            return op;
        }


        //[Route("invited/check")]
        //[HttpPost]
        //public OutputModel getInvitedAccount(tblM_User x)
        //{
        //    try
        //    {
        //        trustConnection();
        //        op = sr.getInvitedUser(x);
        //        if (op.Status == "success")
        //            op.Data = JsonConvert.DeserializeObject<UserModel>(op.Data.ToString());
        //    }
        //    catch (Exception ex)
        //    {
        //        op.Status = "error";
        //        op.Message = ex.Message;
        //        op.Data = null;
        //    }

        //    return op;
        //}


        //[Route("invited/onboarding")]
        //[HttpPut]
        //public OutputModel updateInvitedAccount(tblM_User x)
        //{
        //    try
        //    {
        //        trustConnection();
        //        op = sr.updateInvitedUser(x);
        //        //if (op.Status == "success")
        //        //    op.Data = JsonConvert.DeserializeObject<UserModel>(op.Data.ToString());
        //    }
        //    catch (Exception ex)
        //    {
        //        op.Status = "error";
        //        op.Message = ex.Message;
        //        op.Data = null;
        //    }

        //    return op;
        //}
    }
}