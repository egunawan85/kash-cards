using Newtonsoft.Json;
using QryptoCard.API.Admin.Models.Service;
using QryptoCard.API.Admin.UserV1Service;
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

        [Route("list/active")]
        [HttpGet]
        public OutputModel getUserActive()
        {
            try
            {
                trustConnection();
                var x = new tblM_User();
                op = sr.getUser(x);
                if (op.Status == "success")
                    op.Data = JsonConvert.DeserializeObject<List<UserModel>>(op.Data.ToString());
            }
            catch (Exception ex)
            {
                op.Status = "error";
                op.Message = ex.Message;
                op.Data = null;
            }

            return op;
        }

        [Route("commission/list")]
        [HttpGet]
        public OutputModel getUserComm()
        {
            try
            {
                trustConnection();
                var x = new vw_User_Commission();
                op = sr.getUserCommissionList(x);
                if (op.Status == "success")
                    op.Data = JsonConvert.DeserializeObject<List<vw_User_Commission>>(op.Data.ToString());
            }
            catch (Exception ex)
            {
                op.Status = "error";
                op.Message = ex.Message;
                op.Data = null;
            }

            return op;
        }

        [Route("commission")]
        [HttpPut]
        public OutputModel getUserComm(tblM_User_Commission x)
        {
            try
            {
                trustConnection();
                op = sr.updateUserCommission(x);
                //if (op.Status == "success")
                //    op.Data = JsonConvert.DeserializeObject<List<vw_User_Commission>>(op.Data.ToString());
            }
            catch (Exception ex)
            {
                op.Status = "error";
                op.Message = ex.Message;
                op.Data = null;
            }

            return op;
        }

        [Route("fee/list")]
        [HttpGet]
        public OutputModel getUserFee()
        {
            try
            {
                trustConnection();
                var x = new vw_User_Fee();
                op = sr.getUserFeeList(x);
                if (op.Status == "success")
                    op.Data = JsonConvert.DeserializeObject<List<vw_User_Fee>>(op.Data.ToString());
            }
            catch (Exception ex)
            {
                op.Status = "error";
                op.Message = ex.Message;
                op.Data = null;
            }

            return op;
        }

        [Route("fee")]
        [HttpPut]
        public OutputModel updateUserComm(tblM_User_Fee x)
        {
            try
            {
                trustConnection();
                op = sr.updateUserFee(x);
                //if (op.Status == "success")
                //    op.Data = JsonConvert.DeserializeObject<List<vw_User_Commission>>(op.Data.ToString());
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
