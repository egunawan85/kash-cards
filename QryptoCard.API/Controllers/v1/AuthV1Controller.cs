using Newtonsoft.Json;
using QryptoCard.API.Filters;
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


        [RateLimit(5, 60)]
        [Route("register")]
        [HttpPost]
        public OutputModel register(tblM_User x)
        {
            try
            {
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



        [RateLimit(3, 300)]
        [Route("register/resend")]
        [HttpPost]
        public OutputModel regenerateOTPRegister(tblH_User_Register x)
        {
            try
            {
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

        [RateLimit(10, 60)]
        [Route("register/verify")]
        [HttpPost]
        public OutputModel registerVerify(tblH_User_Register x)
        {
            try
            {
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




        [RateLimit(5, 60)]
        [Route("login")]
        [HttpPost]
        public OutputModel login(tblM_User x)
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
        public OutputModel regenrerateOTP(tblH_User_Login x)
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
        public OutputModel loginVerify(tblH_User_Login x)
        {
            try
            {
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

        [RateLimit(5, 60)]
        [Route("password/forgot")]
        [HttpPost]
        public OutputModel forgotPassword(tblM_User x)
        {
            try
            {
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

        [RateLimit(10, 60)]
        [Route("password/forgot/check")]
        [HttpPost]
        public OutputModel checkforgotPassword(tblT_User_ForgotPassword x)
        {
            try
            {
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

        [RateLimit(10, 60)]
        [Route("password/forgot/change")]
        [HttpPost]
        public OutputModel changeforgotPassword(tblT_User_ForgotPassword x)
        {
            try
            {
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


        // ---------- auth-token routes ----------
        //
        // Dashboard flow:
        //   POST /v1/auth/login          -> existing Login() above (unchanged) — sends OTP to user
        //   POST /v1/auth/mint-after-otp -> THIS route — verifies OTP + mints token pair
        //   POST /v1/auth/refresh        -> THIS route — rotates refresh token, issues new access
        //   POST /v1/auth/revoke         -> THIS route — logout (kills entire chain, idempotent)
        //
        // The existing /v1/auth/login/verify route is left in place untouched for
        // legacy callers. The response shape is QryptoCard.INT.Model.Service.AuthMintResponse
        // (deserialized client-side by the dashboard from op.Data).
        //
        // OutputModel is fully qualified to QryptoCard.INT.Model.Service.OutputModel
        // here (the class' field-level `op` is the proxy-generated UserV1Service.OutputModel).

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
            // subjectType is server-controlled per URL tier — the user-tier route
            // always mints "user" tokens regardless of any req.SubjectType. Locks
            // the namespace at the URL tier (defense-in-depth on top of
            // AuthV1Service's SubjectType-routed OTP lookup).
            return AuthTokenSecurity.MintAfterOtpVerify(req.OtpSessionId, req.OtpCode, "user");
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
            // Logout is idempotent — a null body is treated as a no-op success so
            // the dashboard can clear its session unconditionally.
            if (req == null)
            {
                return new QryptoCard.INT.Model.Service.OutputModel { Status = "success", Message = "ok" };
            }
            return AuthTokenSecurity.Revoke(req.RefreshToken);
        }
    }

    // Auth-token request DTOs. Plain POCOs (no [DataContract]) — these are
    // ASP.NET Web API JSON deserialization targets, not WCF DataContracts.
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