using Newtonsoft.Json;
using QryptoCard.API.Admin.AdminV1Service;
using QryptoCard.API.Admin.Models.Service;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Web;
using System.Web.Http;

namespace QryptoCard.API.Admin.Controllers.v1
{
    [RoutePrefix("v1/admin")]
    [BearerAuthentication]
    public class AdminV1Controller : QryptoCardApiController
    {
        AdminV1ServiceClient sr = new AdminV1ServiceClient();
        OutputModel op = new OutputModel();


        [Route("list")]
        [HttpPost]
        public OutputModel getAdminFilter(AdminFilterModel x)
        {
            try
            {
                op = sr.getAdminFilter(getEmail(), x);
                if (op.Status == "success")
                    op.Data = JsonConvert.DeserializeObject<List<AdminModel>>(op.Data.ToString());
            }
            catch (Exception ex)
            {
                op.Status = "error";
                op.Message = ex.Message;
                op.Data = null;
            }

            return op;
        }

        [Route("detail")]
        [HttpPost]
        public OutputModel getAdminDetail(tblM_Admin x)
        {
            try
            {
                op = sr.getAdminDetail(getEmail(), x);
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

        [Route("invite")]
        [HttpPost]
        public OutputModel addAdmin(tblM_Admin x)
        {
            try
            {
                op = sr.addAdmin(getEmail(), x);
                //if (op.Status == "success")
                //    op.Data = JsonConvert.DeserializeObject<List<AdminModel>>(op.Data.ToString());
            }
            catch (Exception ex)
            {
                op.Status = "error";
                op.Message = ex.Message;
                op.Data = null;
            }

            return op;
        }

        // Dev-only test-credit tool (SD-2). The acting admin identity comes from the
        // bearer token (getEmail()), never the body. All three walls — environment
        // hard-gate (fail-closed), root-admin-only, and audit-logging — are enforced
        // in the INT-tier service, the authoritative money boundary that a direct WCF
        // caller cannot bypass.
        [Route("dev/credit")]
        [HttpPost]
        public OutputModel devCreditWallet(DevCreditModel x)
        {
            try
            {
                op = sr.devCreditWallet(getEmail(), x.UserId, x.Amount, x.Reference);
            }
            catch (Exception ex)
            {
                op.Status = "error";
                op.Message = ex.Message;
                op.Data = null;
            }

            return op;
        }

        [Route("ban")]
        [HttpDelete]
        public OutputModel banAdmin(tblM_Admin x)
        {
            try
            {
                op = sr.banAdmin(getEmail(), x);
                //if (op.Status == "success")
                //    op.Data = JsonConvert.DeserializeObject<List<AdminModel>>(op.Data.ToString());
            }
            catch (Exception ex)
            {
                op.Status = "error";
                op.Message = ex.Message;
                op.Data = null;
            }

            return op;
        }

        [Route("data/{id}")]
        [HttpGet]
        public OutputModel getAdminData([FromUri] string id)
        {
            try
            {
                op = sr.getAdminData(getEmail(), id);
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

        [Route("data")]
        [HttpPut]
        public OutputModel updateAdminData(tblM_Admin x)
        {
            try
            {
                op = sr.updateAdminData(getEmail(), x);
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

        [Route("password")]
        [HttpPut]
        public OutputModel updatePassword(PasswordChangeModel x)
        {
            try
            {
                op = sr.updatePassword(getEmail(), x);
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

        [Route("email/otp")]
        [HttpPost]
        public OutputModel updateEmailOTP(tblM_Admin x)
        {
            try
            {
                op = sr.updateEmailOTP(getEmail(), x);
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

        [Route("email")]
        [HttpPut]
        public OutputModel updateEmail(tblH_Admin_OTP x)
        {
            try
            {
                op = sr.updateEmail(getEmail(), x);
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
