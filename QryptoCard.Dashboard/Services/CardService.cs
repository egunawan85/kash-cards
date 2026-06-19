using Newtonsoft.Json;
using QryptoCard.Dashboard.Models;
using QryptoCard.Dashboard.Models.Service;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Web;

namespace QryptoCard.Dashboard.Services
{
    public class CardService
    {
        OutputModel op = new OutputModel();

        public OutputModel getCardTypes()
        {
            try
            {
                Common.trustConnection();
                string path = "/v1/card/type";
                WebClient client = new WebClient();
                client.Headers["Content-type"] = "application/json";
                //string inputJson = JsonConvert.SerializeObject(adm);
                client.Headers[HttpRequestHeader.Authorization] = string.Format("Basic {0}", Security.credentials(SessionLib.Current.Email, SessionLib.Current.Password));
                //client.Headers[HttpRequestHeader.Authorization] = string.Format("Basic {0}", Security.credentialsNoAuthUser());
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

        public OutputModel getCardTypesByID(CardTypeModel x)
        {
            try
            {
                Common.trustConnection();
                string path = "/v1/card/type/id";
                WebClient client = new WebClient();
                client.Headers["Content-type"] = "application/json";
                string inputJson = JsonConvert.SerializeObject(x);
                client.Headers[HttpRequestHeader.Authorization] = string.Format("Basic {0}", Security.credentials(SessionLib.Current.Email, SessionLib.Current.Password));
                //client.Headers[HttpRequestHeader.Authorization] = string.Format("Basic {0}", Security.credentialsNoAuthUser());
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

        public OutputModel getHolderDetail(CardholderModel x)
        {
            try
            {
                Common.trustConnection();
                string path = "/v1/card/holder/detail";
                WebClient client = new WebClient();
                client.Headers["Content-type"] = "application/json";
                string inputJson = JsonConvert.SerializeObject(x);
                client.Headers[HttpRequestHeader.Authorization] = string.Format("Basic {0}", Security.credentials(SessionLib.Current.Email, SessionLib.Current.Password));
                //client.Headers[HttpRequestHeader.Authorization] = string.Format("Basic {0}", Security.credentialsNoAuthUser());
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

        public OutputModel checkHolder(CardholderModel x)
        {
            try
            {
                Common.trustConnection();
                string path = "/v1/card/holder/check/cardtypeid";
                WebClient client = new WebClient();
                client.Headers["Content-type"] = "application/json";
                string inputJson = JsonConvert.SerializeObject(x);
                client.Headers[HttpRequestHeader.Authorization] = string.Format("Basic {0}", Security.credentials(SessionLib.Current.Email, SessionLib.Current.Password));
                //client.Headers[HttpRequestHeader.Authorization] = string.Format("Basic {0}", Security.credentialsNoAuthUser());
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

        public OutputModel getCardList(CardModel x)
        {
            try
            {
                Common.trustConnection();
                string path = "/v1/card/list";
                WebClient client = new WebClient();
                client.Headers["Content-type"] = "application/json";
                string inputJson = JsonConvert.SerializeObject(x);
                client.Headers[HttpRequestHeader.Authorization] = string.Format("Basic {0}", Security.credentials(SessionLib.Current.Email, SessionLib.Current.Password));
                //client.Headers[HttpRequestHeader.Authorization] = string.Format("Basic {0}", Security.credentialsNoAuthUser());
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

        public OutputModel getCardListAll(CardModel x)
        {
            try
            {
                Common.trustConnection();
                string path = "/v1/card/list/all";
                WebClient client = new WebClient();
                client.Headers["Content-type"] = "application/json";
                string inputJson = JsonConvert.SerializeObject(x);
                client.Headers[HttpRequestHeader.Authorization] = string.Format("Basic {0}", Security.credentials(SessionLib.Current.Email, SessionLib.Current.Password));
                //client.Headers[HttpRequestHeader.Authorization] = string.Format("Basic {0}", Security.credentialsNoAuthUser());
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

        public OutputModel getCardDetail(CardModel x)
        {
            try
            {
                Common.trustConnection();
                string path = "/v1/card/detail/id";
                WebClient client = new WebClient();
                client.Headers["Content-type"] = "application/json";
                string inputJson = JsonConvert.SerializeObject(x);
                client.Headers[HttpRequestHeader.Authorization] = string.Format("Basic {0}", Security.credentials(SessionLib.Current.Email, SessionLib.Current.Password));
                //client.Headers[HttpRequestHeader.Authorization] = string.Format("Basic {0}", Security.credentialsNoAuthUser());
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

        public OutputModel openCard(CardModel x)
        {
            try
            {
                Common.trustConnection();
                string path = "/v1/card/open";
                WebClient client = new WebClient();
                client.Headers["Content-type"] = "application/json";
                string inputJson = JsonConvert.SerializeObject(x);
                client.Headers[HttpRequestHeader.Authorization] = string.Format("Basic {0}", Security.credentials(SessionLib.Current.Email, SessionLib.Current.Password));
                //client.Headers[HttpRequestHeader.Authorization] = string.Format("Basic {0}", Security.credentialsNoAuthUser());
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

        public OutputModel cancelCardTransaction(CardModel x)
        {
            try
            {
                Common.trustConnection();
                string path = "/v1/card/trx/cancel";
                WebClient client = new WebClient();
                client.Headers["Content-type"] = "application/json";
                string inputJson = JsonConvert.SerializeObject(x);
                client.Headers[HttpRequestHeader.Authorization] = string.Format("Basic {0}", Security.credentials(SessionLib.Current.Email, SessionLib.Current.Password));
                //client.Headers[HttpRequestHeader.Authorization] = string.Format("Basic {0}", Security.credentialsNoAuthUser());
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

        public OutputModel depositCard(CardDepositModel x)
        {
            try
            {
                Common.trustConnection();
                string path = "/v1/card/deposit";
                WebClient client = new WebClient();
                client.Headers["Content-type"] = "application/json";
                string inputJson = JsonConvert.SerializeObject(x);
                client.Headers[HttpRequestHeader.Authorization] = string.Format("Basic {0}", Security.credentials(SessionLib.Current.Email, SessionLib.Current.Password));
                //client.Headers[HttpRequestHeader.Authorization] = string.Format("Basic {0}", Security.credentialsNoAuthUser());
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

        public OutputModel depositCardList(CardDepositModel x)
        {
            try
            {
                Common.trustConnection();
                string path = "/v1/card/deposit/list";
                WebClient client = new WebClient();
                client.Headers["Content-type"] = "application/json";
                string inputJson = JsonConvert.SerializeObject(x);
                client.Headers[HttpRequestHeader.Authorization] = string.Format("Basic {0}", Security.credentials(SessionLib.Current.Email, SessionLib.Current.Password));
                //client.Headers[HttpRequestHeader.Authorization] = string.Format("Basic {0}", Security.credentialsNoAuthUser());
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

        public OutputModel depositCardDetail(CardDepositModel x)
        {
            try
            {
                Common.trustConnection();
                string path = "/v1/card/deposit/detail";
                WebClient client = new WebClient();
                client.Headers["Content-type"] = "application/json";
                string inputJson = JsonConvert.SerializeObject(x);
                client.Headers[HttpRequestHeader.Authorization] = string.Format("Basic {0}", Security.credentials(SessionLib.Current.Email, SessionLib.Current.Password));
                //client.Headers[HttpRequestHeader.Authorization] = string.Format("Basic {0}", Security.credentialsNoAuthUser());
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

        public OutputModel cancelDeposit(CardModel x)
        {
            try
            {
                Common.trustConnection();
                string path = "/v1/card/deposit/cancel";
                WebClient client = new WebClient();
                client.Headers["Content-type"] = "application/json";
                string inputJson = JsonConvert.SerializeObject(x);
                client.Headers[HttpRequestHeader.Authorization] = string.Format("Basic {0}", Security.credentials(SessionLib.Current.Email, SessionLib.Current.Password));
                //client.Headers[HttpRequestHeader.Authorization] = string.Format("Basic {0}", Security.credentialsNoAuthUser());
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

        public OutputModel trxCardList(CardModel x)
        {
            try
            {
                Common.trustConnection();
                string path = "/v1/card/trx/list";
                WebClient client = new WebClient();
                client.Headers["Content-type"] = "application/json";
                string inputJson = JsonConvert.SerializeObject(x);
                client.Headers[HttpRequestHeader.Authorization] = string.Format("Basic {0}", Security.credentials(SessionLib.Current.Email, SessionLib.Current.Password));
                //client.Headers[HttpRequestHeader.Authorization] = string.Format("Basic {0}", Security.credentialsNoAuthUser());
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

        public OutputModel trxCardDetail(CardTransactionModel x)
        {
            try
            {
                Common.trustConnection();
                string path = "/v1/card/trx/detail";
                WebClient client = new WebClient();
                client.Headers["Content-type"] = "application/json";
                string inputJson = JsonConvert.SerializeObject(x);
                client.Headers[HttpRequestHeader.Authorization] = string.Format("Basic {0}", Security.credentials(SessionLib.Current.Email, SessionLib.Current.Password));
                //client.Headers[HttpRequestHeader.Authorization] = string.Format("Basic {0}", Security.credentialsNoAuthUser());
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

        //public OutputModel getStaticAddress(AddressStaticFilterModel adm)
        //{
        //    try
        //    {
        //        Common.trustConnection();
        //        string path = "/v1/address/static";
        //        WebClient client = new WebClient();
        //        client.Headers["Content-type"] = "application/json";
        //        string inputJson = JsonConvert.SerializeObject(adm);
        //        client.Headers[HttpRequestHeader.Authorization] = string.Format("Basic {0}", Security.credentials(SessionLib.Current.Email, SessionLib.Current.Password));
        //        //client.Headers[HttpRequestHeader.Authorization] = string.Format("Basic {0}", Security.credentialsNoAuthUser());
        //        client.Encoding = Encoding.UTF8;
        //        ServicePointManager.ServerCertificateValidationCallback += (sender, certificate, chain, sslPolicyErrors) => true;
        //        return op = JsonConvert.DeserializeObject<OutputModel>(client.UploadString(KeyModel.API_URL + path, "POST", inputJson));
        //    }
        //    catch (Exception ex)
        //    {
        //        op.Message = ex.ToString();
        //        op.Status = "error";
        //        return op;
        //    }
        //}
    }
}