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
                Common.trustConnection();
                string path = "/v1/user/list/active";
                WebClient client = new WebClient();
                client.Headers["Content-type"] = "application/json";
                client.Headers[HttpRequestHeader.Authorization] = string.Format("Basic {0}", Security.credentials(SessionLib.Current.Email, SessionLib.Current.Password));
                //client.Headers[HttpRequestHeader.Authorization] = string.Format("Basic {0}", Security.credentialsNoAuthAdmin());
                client.Encoding = Encoding.UTF8;
                ServicePointManager.ServerCertificateValidationCallback += (sender, certificate, chain, sslPolicyErrors) => true;
                return op = JsonConvert.DeserializeObject<OutputModel>(client.DownloadString(KeyModel.API_URL + path));
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
                Common.trustConnection();
                string path = "/v1/user/commission/list";
                WebClient client = new WebClient();
                client.Headers["Content-type"] = "application/json";
                client.Headers[HttpRequestHeader.Authorization] = string.Format("Basic {0}", Security.credentials(SessionLib.Current.Email, SessionLib.Current.Password));
                //client.Headers[HttpRequestHeader.Authorization] = string.Format("Basic {0}", Security.credentialsNoAuthAdmin());
                client.Encoding = Encoding.UTF8;
                ServicePointManager.ServerCertificateValidationCallback += (sender, certificate, chain, sslPolicyErrors) => true;
                return op = JsonConvert.DeserializeObject<OutputModel>(client.DownloadString(KeyModel.API_URL + path));
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
                Common.trustConnection();
                string path = "/v1/user/commission";
                WebClient client = new WebClient();
                client.Headers["Content-type"] = "application/json";
                string inputJson = JsonConvert.SerializeObject(adm);
                client.Headers[HttpRequestHeader.Authorization] = string.Format("Basic {0}", Security.credentials(SessionLib.Current.Email, SessionLib.Current.Password));
                //client.Headers[HttpRequestHeader.Authorization] = string.Format("Basic {0}", Security.credentialsNoAuthAdmin());
                client.Encoding = Encoding.UTF8;
                ServicePointManager.ServerCertificateValidationCallback += (sender, certificate, chain, sslPolicyErrors) => true;
                return op = JsonConvert.DeserializeObject<OutputModel>(client.UploadString(KeyModel.API_URL + path, "PUT", inputJson));
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
                Common.trustConnection();
                string path = "/v1/user/fee/list";
                WebClient client = new WebClient();
                client.Headers["Content-type"] = "application/json";
                client.Headers[HttpRequestHeader.Authorization] = string.Format("Basic {0}", Security.credentials(SessionLib.Current.Email, SessionLib.Current.Password));
                //client.Headers[HttpRequestHeader.Authorization] = string.Format("Basic {0}", Security.credentialsNoAuthAdmin());
                client.Encoding = Encoding.UTF8;
                ServicePointManager.ServerCertificateValidationCallback += (sender, certificate, chain, sslPolicyErrors) => true;
                return op = JsonConvert.DeserializeObject<OutputModel>(client.DownloadString(KeyModel.API_URL + path));
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
                Common.trustConnection();
                string path = "/v1/user/fee";
                WebClient client = new WebClient();
                client.Headers["Content-type"] = "application/json";
                string inputJson = JsonConvert.SerializeObject(adm);
                client.Headers[HttpRequestHeader.Authorization] = string.Format("Basic {0}", Security.credentials(SessionLib.Current.Email, SessionLib.Current.Password));
                //client.Headers[HttpRequestHeader.Authorization] = string.Format("Basic {0}", Security.credentialsNoAuthAdmin());
                client.Encoding = Encoding.UTF8;
                ServicePointManager.ServerCertificateValidationCallback += (sender, certificate, chain, sslPolicyErrors) => true;
                return op = JsonConvert.DeserializeObject<OutputModel>(client.UploadString(KeyModel.API_URL + path, "PUT", inputJson));
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