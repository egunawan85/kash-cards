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
    public class CardService
    {
        OutputModel op = new OutputModel();

        public OutputModel getCardType()
        {
            try
            {
                Common.trustConnection();
                string path = "/v1/card/type";
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

        public OutputModel getCardTypeDetail(CardTypeModel adm)
        {
            try
            {
                Common.trustConnection();
                string path = "/v1/card/type/id";
                WebClient client = new WebClient();
                client.Headers["Content-type"] = "application/json";
                string inputJson = JsonConvert.SerializeObject(adm);
                client.Headers[HttpRequestHeader.Authorization] = string.Format("Basic {0}", Security.credentials(SessionLib.Current.Email, SessionLib.Current.Password));
                //client.Headers[HttpRequestHeader.Authorization] = string.Format("Basic {0}", Security.credentialsNoAuthAdmin());
                client.Encoding = Encoding.UTF8;
                ServicePointManager.ServerCertificateValidationCallback += (sender, certificate, chain, sslPolicyErrors) => true;
                return op = JsonConvert.DeserializeObject<OutputModel>(client.UploadString(KeyModel.API_URL + path, "POST", inputJson));
            }
            catch (Exception ex)
            {
                op.Message = ex.ToString();
                op.Status = "error";
                return op;
            }
        }

        public OutputModel updateCardPrice(CardTypeModel adm)
        {
            try
            {
                Common.trustConnection();
                string path = "/v1/card/type/price";
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

        public OutputModel updateCardDepositFee(CardTypeModel adm)
        {
            try
            {
                Common.trustConnection();
                string path = "/v1/card/type/deposit/fee";
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

        public OutputModel getActiveCards()
        {
            try
            {
                Common.trustConnection();
                string path = "/v1/card/active";
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

        public OutputModel getAllCards()
        {
            try
            {
                Common.trustConnection();
                string path = "/v1/card/all";
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

        public OutputModel getCardTtransaction(CardFilterModel adm)
        {
            try
            {
                Common.trustConnection();
                string path = "/v1/card/transaction";
                WebClient client = new WebClient();
                client.Headers["Content-type"] = "application/json";
                string inputJson = JsonConvert.SerializeObject(adm);
                client.Headers[HttpRequestHeader.Authorization] = string.Format("Basic {0}", Security.credentials(SessionLib.Current.Email, SessionLib.Current.Password));
                //client.Headers[HttpRequestHeader.Authorization] = string.Format("Basic {0}", Security.credentialsNoAuthAdmin());
                client.Encoding = Encoding.UTF8;
                ServicePointManager.ServerCertificateValidationCallback += (sender, certificate, chain, sslPolicyErrors) => true;
                return op = JsonConvert.DeserializeObject<OutputModel>(client.UploadString(KeyModel.API_URL + path, "POST", inputJson));
            }
            catch (Exception ex)
            {
                op.Message = ex.ToString();
                op.Status = "error";
                return op;
            }
        }

        public OutputModel getDepositTtransaction(DepositFilterModel adm)
        {
            try
            {
                Common.trustConnection();
                string path = "/v1/card/deposit/transaction";
                WebClient client = new WebClient();
                client.Headers["Content-type"] = "application/json";
                string inputJson = JsonConvert.SerializeObject(adm);
                client.Headers[HttpRequestHeader.Authorization] = string.Format("Basic {0}", Security.credentials(SessionLib.Current.Email, SessionLib.Current.Password));
                //client.Headers[HttpRequestHeader.Authorization] = string.Format("Basic {0}", Security.credentialsNoAuthAdmin());
                client.Encoding = Encoding.UTF8;
                ServicePointManager.ServerCertificateValidationCallback += (sender, certificate, chain, sslPolicyErrors) => true;
                return op = JsonConvert.DeserializeObject<OutputModel>(client.UploadString(KeyModel.API_URL + path, "POST", inputJson));
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