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

        private void trustConnection()
        {
            ServicePointManager.ServerCertificateValidationCallback +=
                (se, cert, chain, sslerror) =>
                {
                    return true;
                };
        }

        [Route("list")]
        [HttpPost]
        public OutputModel getAdminFilter(AdminFilterModel x)
        {
            try
            {
                trustConnection();
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
                trustConnection();
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
                trustConnection();
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

        [Route("ban")]
        [HttpDelete]
        public OutputModel banAdmin(tblM_Admin x)
        {
            try
            {
                trustConnection();
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
                trustConnection();
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
                trustConnection();
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
                trustConnection();
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
                trustConnection();
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
                trustConnection();
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
