using Newtonsoft.Json;
using QryptoCard.API.Admin.AdminV1Service;
using QryptoCard.API.Admin.Filters;
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


        [RateLimit(5, 60)]
        [Route("login")]
        [HttpPost]
        public OutputModel login(tblM_Admin x)
        {
            try
            {
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

        [RateLimit(3, 300)]
        [Route("login/resend")]
        [HttpPost]
        public OutputModel regenrerateOTP(tblH_Admin_Login x)
        {
            try
            {
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

        [RateLimit(10, 60)]
        [Route("login/verify")]
        [HttpPost]
        public OutputModel loginVerify(tblH_Admin_Login x)
        {
            try
            {
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

        [RateLimit(5, 60)]
        [Route("password/forgot")]
        [HttpPost]
        public OutputModel forgotPassword(tblM_Admin x)
        {
            try
            {
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

        [RateLimit(10, 60)]
        [Route("password/forgot/check")]
        [HttpPost]
        public OutputModel checkforgotPassword(tblT_Admin_ForgotPassword x)
        {
            try
            {
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

        [RateLimit(10, 60)]
        [Route("password/forgot/change")]
        [HttpPost]
        public OutputModel changeforgotPassword(tblT_Admin_ForgotPassword x)
        {
            try
            {
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


        [RateLimit(10, 60)]
        [Route("invited/check")]
        [HttpPost]
        public OutputModel getInvitedAccount(tblM_Admin x)
        {
            try
            {
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


        [RateLimit(10, 60)]
        [Route("invited/onboarding")]
        [HttpPut]
        public OutputModel updateInvitedAccount(tblM_Admin x)
        {
            try
            {
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

        // ---------- auth-token routes ----------
        //
        // Admin-tier mirror of QryptoCard.API/Controllers/v1/AuthV1Controller.cs.
        // The URL tier locks the minted SubjectType to "admin" regardless of the
        // request body. See the user-tier controller for the full flow comment.
        //
        // OutputModel is fully qualified to QryptoCard.INT.Model.Service.OutputModel
        // here (the class' field-level `op` is the proxy-generated AdminV1Service.OutputModel).

        [RateLimit(10, 60)]
        [Route("mint-after-otp")]
        [HttpPost]
        public QryptoCard.INT.Model.Service.OutputModel mintAfterOtp(MintAfterOtpRequest req)
        {
            if (req == null)
            {
                return new QryptoCard.INT.Model.Service.OutputModel
                {
                    Status = "failed",
                    Message = "Invalid OTP code or session"
                };
            }
            // Admin-tier route always mints "admin" tokens regardless of req.SubjectType.
            return AuthTokenSecurity.MintAfterOtpVerify(req.OtpSessionId, req.OtpCode, "admin");
        }

        [RateLimit(10, 60)]
        [Route("refresh")]
        [HttpPost]
        public QryptoCard.INT.Model.Service.OutputModel refresh(RefreshTokenRequest req)
        {
            if (req == null)
            {
                return new QryptoCard.INT.Model.Service.OutputModel
                {
                    Status = "failed",
                    Message = "Invalid refresh token"
                };
            }
            return AuthTokenSecurity.Refresh(req.RefreshToken);
        }

        [RateLimit(10, 60)]
        [Route("revoke")]
        [HttpPost]
        public QryptoCard.INT.Model.Service.OutputModel revoke(RefreshTokenRequest req)
        {
            // Logout is idempotent — a null body is a no-op success.
            if (req == null)
            {
                return new QryptoCard.INT.Model.Service.OutputModel { Status = "success", Message = "ok" };
            }
            return AuthTokenSecurity.Revoke(req.RefreshToken);
        }
    }

    // Auth-token request DTOs (Admin-tier mirror of the user tier). Plain POCOs
    // (no [DataContract]) — ASP.NET Web API JSON deserialization targets.
    public class MintAfterOtpRequest
    {
        public string OtpSessionId { get; set; }
        public string OtpCode      { get; set; }
        public string SubjectType  { get; set; }   // "user" | "admin"
    }

    public class RefreshTokenRequest
    {
        public string RefreshToken { get; set; }
    }
}
