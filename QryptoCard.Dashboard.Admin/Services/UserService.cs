using Newtonsoft.Json;
using QryptoCard.Dashboard.Admin.Models;
using QryptoCard.Dashboard.Admin.Models.Service;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Web;

namespace QryptoCard.Dashboard.Admin.Services
{
    public class UserService
    {
        OutputModel op = new OutputModel();

        public OutputModel getUserActive()
        {
            try
            {
                string path = "/v1/user/list/active";
                return op = AuthClient.ExecuteJsonGet(path);
            }
            catch (Exception ex)
            {
                op.Message = ex.ToString();
                op.Status = "error";
                return op;
            }
        }

        public OutputModel getUserComm()
        {
            try
            {
                string path = "/v1/user/commission/list";
                return op = AuthClient.ExecuteJsonGet(path);
            }
            catch (Exception ex)
            {
                op.Message = ex.ToString();
                op.Status = "error";
                return op;
            }
        }

        public OutputModel updateComm(UserCommissionModel adm)
        {
            try
            {
                string path = "/v1/user/commission";
                return op = AuthClient.ExecuteJsonPut(path, adm);
            }
            catch (Exception ex)
            {
                op.Message = ex.ToString();
                op.Status = "error";
                return op;
            }
        }

        public OutputModel getUserFee()
        {
            try
            {
                string path = "/v1/user/fee/list";
                return op = AuthClient.ExecuteJsonGet(path);
            }
            catch (Exception ex)
            {
                op.Message = ex.ToString();
                op.Status = "error";
                return op;
            }
        }

        public OutputModel updateFee(UserFeeModel adm)
        {
            try
            {
                string path = "/v1/user/fee";
                return op = AuthClient.ExecuteJsonPut(path, adm);
            }
            catch (Exception ex)
            {
                op.Message = ex.ToString();
                op.Status = "error";
                return op;
            }
        }
    }
}