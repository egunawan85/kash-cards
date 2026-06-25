using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Net.Http;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Web;
using QryptoCard.INT.Model.WasabiCard;
using QryptoCard.INT.Model;

namespace QryptoCard.INT.Script.Gateway.WasabiCard
{
    public class WasabiCardService
    {
        //dev
        //private static string loadRsaPrivateKeyPem()
        //{
            //return KeyModel.WASABICARD_PRIVATE_KEY_XML;
        //}

        //prod
        private static string loadRsaPrivateKeyPem()
        {
            return KeyModel.WASABICARD_PRIVATE_KEY_XML;
        }

        public static string signData(string strText, string privateKey)
        {
            var testData = Encoding.UTF8.GetBytes(strText);

            using (var rsa = new RSACryptoServiceProvider(2048))
            {
                try
                {
                    // client encrypting data with public key issued by server                    
                    rsa.FromXmlString(privateKey.ToString());

                    //var encryptedData = rsa.Encrypt(testData, true);
                    var encryptedData = rsa.SignData(testData, new System.Security.Cryptography.SHA256CryptoServiceProvider());

                    var base64Encrypted = Convert.ToBase64String(encryptedData);

                    return base64Encrypted;
                }
                finally
                {
                    rsa.PersistKeyInCsp = false;
                }
            }
        }

        public static string decrypt(string strText, string privateKey)
        {
            var testData = Encoding.UTF8.GetBytes(strText);

            using (var rsa = new RSACryptoServiceProvider(2048))
            {
                try
                {
                    var base64Encrypted = strText;

                    // server decrypting data with private key                    
                    rsa.FromXmlString(privateKey);

                    var resultBytes = Convert.FromBase64String(base64Encrypted);
                    var decryptedBytes = rsa.Decrypt(resultBytes, RSAEncryptionPadding.Pkcs1);
                    var decryptedData = Encoding.UTF8.GetString(decryptedBytes);
                    return decryptedData.ToString();
                }
                finally
                {
                    rsa.PersistKeyInCsp = false;
                }
            }
        }

        public static WCAccountInfoResponseModel getAccountInfo()
        {
            DBEntities db = new DBEntities();
            tblH_API_Log api = new tblH_API_Log();
            WCAccountInfoResponseModel response = new WCAccountInfoResponseModel();
            try
            {
                HttpClient clients = new HttpClient();
                clients.BaseAddress = new Uri(KeyModel.WASABICARD_API_URL);
                clients.Timeout.Add(new TimeSpan(0, 0, 5));
                clients.DefaultRequestHeaders.Accept.Clear();

                clients.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

                string path = "/merchant/core/mcb/account/info";
                clients.DefaultRequestHeaders.Add("X-WSB-API-KEY", KeyModel.WASABICARD_API_KEY);
                clients.DefaultRequestHeaders.Add("X-WSB-SIGNATURE", signData("{}", loadRsaPrivateKeyPem()));
                var httpContent = new StringContent("{}", Encoding.UTF8, "application/json");
                //var xxx = JsonConvert.SerializeObject(req);
                HttpResponseMessage responses = clients.PostAsync(path, httpContent).Result;

                api.Type = "Wasabi Card - Get Account Info";
                //api.Request = xxx;
                api.RequestDate = DateTime.Now;

                if (responses.IsSuccessStatusCode)
                {
                    //att = await response.Content.ReadAsAsync<AirportTransferTransaction>();
                    string resultJSON = responses.Content.ReadAsStringAsync().Result;
                    if (responses.StatusCode == HttpStatusCode.OK)
                    {
                        api.Response = resultJSON;
                        api.ResponseDate = DateTime.Now;
                        db.tblH_API_Log.Add(api);
                        db.SaveChanges();
                        response = JsonConvert.DeserializeObject<WCAccountInfoResponseModel>(resultJSON);
                        return response;
                    }
                    else
                    {
                        var x = JsonConvert.DeserializeObject<WCAccountInfoResponseModel>(resultJSON);

                        api.Response = responses.StatusCode.ToString() + " - " + responses.Content.ReadAsStringAsync().Result;
                        api.ResponseDate = DateTime.Now;
                        db.tblH_API_Log.Add(api);
                        db.SaveChanges();
                        return response = null;
                    }
                }
                else
                {
                    api.Response = responses.StatusCode.ToString() + " - " + responses.Content.ReadAsStringAsync().Result;
                    api.ResponseDate = DateTime.Now;
                    db.tblH_API_Log.Add(api);
                    db.SaveChanges();
                    return response = null;
                }
            }
            catch (Exception ex)
            {
                api.Response = ex.Message;
                api.ResponseDate = DateTime.Now;
                db.tblH_API_Log.Add(api);
                db.SaveChanges();
                return response = null;
            }
        }

        public static WCCityListRequestModel getCityList()
        {
            DBEntities db = new DBEntities();
            tblH_API_Log api = new tblH_API_Log();
            WCCityListRequestModel response = new WCCityListRequestModel();
            try
            {
                HttpClient clients = new HttpClient();
                clients.BaseAddress = new Uri(KeyModel.WASABICARD_API_URL);
                clients.Timeout.Add(new TimeSpan(0, 0, 5));
                clients.DefaultRequestHeaders.Accept.Clear();

                clients.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

                string path = "/merchant/core/mcb/common/city";
                clients.DefaultRequestHeaders.Add("X-WSB-API-KEY", KeyModel.WASABICARD_API_KEY);
                clients.DefaultRequestHeaders.Add("X-WSB-SIGNATURE", signData("{}", loadRsaPrivateKeyPem()));
                var httpContent = new StringContent("{}", Encoding.UTF8, "application/json");
                //var xxx = JsonConvert.SerializeObject(req);
                HttpResponseMessage responses = clients.PostAsync(path, httpContent).Result;

                api.Type = "Wasabi Card - Get Account Info";
                //api.Request = xxx;
                api.RequestDate = DateTime.Now;

                if (responses.IsSuccessStatusCode)
                {
                    //att = await response.Content.ReadAsAsync<AirportTransferTransaction>();
                    string resultJSON = responses.Content.ReadAsStringAsync().Result;
                    if (responses.StatusCode == HttpStatusCode.OK)
                    {
                        api.Response = resultJSON;
                        api.ResponseDate = DateTime.Now;
                        db.tblH_API_Log.Add(api);
                        db.SaveChanges();
                        response = JsonConvert.DeserializeObject<WCCityListRequestModel>(resultJSON);
                        return response;
                    }
                    else
                    {
                        var x = JsonConvert.DeserializeObject<WCCityListRequestModel>(resultJSON);

                        api.Response = responses.StatusCode.ToString() + " - " + responses.Content.ReadAsStringAsync().Result;
                        api.ResponseDate = DateTime.Now;
                        db.tblH_API_Log.Add(api);
                        db.SaveChanges();
                        return response = null;
                    }
                }
                else
                {
                    api.Response = responses.StatusCode.ToString() + " - " + responses.Content.ReadAsStringAsync().Result;
                    api.ResponseDate = DateTime.Now;
                    db.tblH_API_Log.Add(api);
                    db.SaveChanges();
                    return response = null;
                }
            }
            catch (Exception ex)
            {
                api.Response = ex.Message;
                api.ResponseDate = DateTime.Now;
                db.tblH_API_Log.Add(api);
                db.SaveChanges();
                return response = null;
            }
        }

        public static WCCardTypeResponseModel getCardType()
        {
            DBEntities db = new DBEntities();
            tblH_API_Log api = new tblH_API_Log();
            WCCardTypeResponseModel response = new WCCardTypeResponseModel();
            try
            {
                HttpClient clients = new HttpClient();
                clients.BaseAddress = new Uri(KeyModel.WASABICARD_API_URL);
                clients.Timeout.Add(new TimeSpan(0, 0, 5));
                clients.DefaultRequestHeaders.Accept.Clear();

                clients.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

                string path = "/merchant/core/mcb/card/cardTypes";
                clients.DefaultRequestHeaders.Add("X-WSB-API-KEY", KeyModel.WASABICARD_API_KEY);
                clients.DefaultRequestHeaders.Add("X-WSB-SIGNATURE", signData("{}", loadRsaPrivateKeyPem()));
                var httpContent = new StringContent("{}", Encoding.UTF8, "application/json");
                //var xxx = JsonConvert.SerializeObject(req);
                HttpResponseMessage responses = clients.PostAsync(path, httpContent).Result;

                api.Type = "Wasabi Card - Get Card Type";
                //api.Request = xxx;
                api.RequestDate = DateTime.Now;

                if (responses.IsSuccessStatusCode)
                {
                    //att = await response.Content.ReadAsAsync<AirportTransferTransaction>();
                    string resultJSON = responses.Content.ReadAsStringAsync().Result;
                    if (responses.StatusCode == HttpStatusCode.OK)
                    {
                        api.Response = resultJSON;
                        api.ResponseDate = DateTime.Now;
                        db.tblH_API_Log.Add(api);
                        db.SaveChanges();
                        response = JsonConvert.DeserializeObject<WCCardTypeResponseModel>(resultJSON);
                        return response;
                    }
                    else
                    {
                        var x = JsonConvert.DeserializeObject<WCCardTypeResponseModel>(resultJSON);

                        api.Response = responses.StatusCode.ToString() + " - " + responses.Content.ReadAsStringAsync().Result;
                        api.ResponseDate = DateTime.Now;
                        db.tblH_API_Log.Add(api);
                        db.SaveChanges();
                        return response = null;
                    }
                }
                else
                {
                    api.Response = responses.StatusCode.ToString() + " - " + responses.Content.ReadAsStringAsync().Result;
                    api.ResponseDate = DateTime.Now;
                    db.tblH_API_Log.Add(api);
                    db.SaveChanges();
                    return response = null;
                }
            }
            catch (Exception ex)
            {
                api.Response = ex.Message;
                api.ResponseDate = DateTime.Now;
                db.tblH_API_Log.Add(api);
                db.SaveChanges();
                return response = null;
            }
        }

        public static WCOpenCardResponseModel openCard(WCOpenCardRequestModel req)
        {
            DBEntities db = new DBEntities();
            tblH_API_Log api = new tblH_API_Log();
            WCOpenCardResponseModel response = new WCOpenCardResponseModel();
            try
            {
                HttpClient clients = new HttpClient();
                clients.BaseAddress = new Uri(KeyModel.WASABICARD_API_URL);
                clients.Timeout.Add(new TimeSpan(0, 0, 5));
                clients.DefaultRequestHeaders.Accept.Clear();

                clients.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

                var httpContent = new StringContent(JsonConvert.SerializeObject(req), Encoding.UTF8, "application/json");
                var xxx = JsonConvert.SerializeObject(req);

                string path = "/merchant/core/mcb/card/openCard";
                clients.DefaultRequestHeaders.Add("X-WSB-API-KEY", KeyModel.WASABICARD_API_KEY);
                clients.DefaultRequestHeaders.Add("X-WSB-SIGNATURE", signData(xxx, loadRsaPrivateKeyPem()));
                HttpResponseMessage responses = clients.PostAsync(path, httpContent).Result;

                api.Type = "Wasabi Card - Open Card";
                api.Request = xxx;
                api.RequestDate = DateTime.Now;

                if (responses.IsSuccessStatusCode)
                {
                    //att = await response.Content.ReadAsAsync<AirportTransferTransaction>();
                    string resultJSON = responses.Content.ReadAsStringAsync().Result;
                    if (responses.StatusCode == HttpStatusCode.OK)
                    {
                        api.Response = resultJSON;
                        api.ResponseDate = DateTime.Now;
                        db.tblH_API_Log.Add(api);
                        db.SaveChanges();
                        response = JsonConvert.DeserializeObject<WCOpenCardResponseModel>(resultJSON);
                        return response;
                    }
                    else
                    {
                        var x = JsonConvert.DeserializeObject<WCOpenCardResponseModel>(resultJSON);

                        api.Response = responses.StatusCode.ToString() + " - " + responses.Content.ReadAsStringAsync().Result;
                        api.ResponseDate = DateTime.Now;
                        db.tblH_API_Log.Add(api);
                        db.SaveChanges();
                        return response = null;
                    }
                }
                else
                {
                    api.Response = responses.StatusCode.ToString() + " - " + responses.Content.ReadAsStringAsync().Result;
                    api.ResponseDate = DateTime.Now;
                    db.tblH_API_Log.Add(api);
                    db.SaveChanges();
                    return response = null;
                }
            }
            catch (Exception ex)
            {
                api.Response = ex.Message;
                api.ResponseDate = DateTime.Now;
                db.tblH_API_Log.Add(api);
                db.SaveChanges();
                return response = null;
            }
        }

        public static WCOpenCardWithHolderResponseModel openCardWithHolder(WCOpenCardWithHolderRequestModel req)
        {
            DBEntities db = new DBEntities();
            tblH_API_Log api = new tblH_API_Log();
            WCOpenCardWithHolderResponseModel response = new WCOpenCardWithHolderResponseModel();
            try
            {
                HttpClient clients = new HttpClient();
                clients.BaseAddress = new Uri(KeyModel.WASABICARD_API_URL);
                clients.Timeout.Add(new TimeSpan(0, 0, 5));
                clients.DefaultRequestHeaders.Accept.Clear();

                clients.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

                var httpContent = new StringContent(JsonConvert.SerializeObject(req), Encoding.UTF8, "application/json");
                var xxx = JsonConvert.SerializeObject(req);

                string path = "/merchant/core/mcb/card/openCard";
                clients.DefaultRequestHeaders.Add("X-WSB-API-KEY", KeyModel.WASABICARD_API_KEY);
                clients.DefaultRequestHeaders.Add("X-WSB-SIGNATURE", signData(xxx, loadRsaPrivateKeyPem()));
                HttpResponseMessage responses = clients.PostAsync(path, httpContent).Result;

                api.Type = "Wasabi Card - Open Card With Holder";
                api.Request = xxx;
                api.RequestDate = DateTime.Now;

                if (responses.IsSuccessStatusCode)
                {
                    //att = await response.Content.ReadAsAsync<AirportTransferTransaction>();
                    string resultJSON = responses.Content.ReadAsStringAsync().Result;
                    if (responses.StatusCode == HttpStatusCode.OK)
                    {
                        api.Response = resultJSON;
                        api.ResponseDate = DateTime.Now;
                        db.tblH_API_Log.Add(api);
                        db.SaveChanges();
                        response = JsonConvert.DeserializeObject<WCOpenCardWithHolderResponseModel>(resultJSON);
                        return response;
                    }
                    else
                    {
                        var x = JsonConvert.DeserializeObject<WCOpenCardWithHolderResponseModel>(resultJSON);

                        api.Response = responses.StatusCode.ToString() + " - " + responses.Content.ReadAsStringAsync().Result;
                        api.ResponseDate = DateTime.Now;
                        db.tblH_API_Log.Add(api);
                        db.SaveChanges();
                        return response = null;
                    }
                }
                else
                {
                    api.Response = responses.StatusCode.ToString() + " - " + responses.Content.ReadAsStringAsync().Result;
                    api.ResponseDate = DateTime.Now;
                    db.tblH_API_Log.Add(api);
                    db.SaveChanges();
                    return response = null;
                }
            }
            catch (Exception ex)
            {
                api.Response = ex.Message;
                api.ResponseDate = DateTime.Now;
                db.tblH_API_Log.Add(api);
                db.SaveChanges();
                return response = null;
            }
        }

        public static WCCardTransactionResponseModel getCardTransaction(WCCardTransactionRequestModel req)
        {
            DBEntities db = new DBEntities();
            tblH_API_Log api = new tblH_API_Log();
            WCCardTransactionResponseModel response = new WCCardTransactionResponseModel();
            try
            {
                HttpClient clients = new HttpClient();
                clients.BaseAddress = new Uri(KeyModel.WASABICARD_API_URL);
                clients.Timeout.Add(new TimeSpan(0, 0, 5));
                clients.DefaultRequestHeaders.Accept.Clear();

                clients.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

                var httpContent = new StringContent(JsonConvert.SerializeObject(req), Encoding.UTF8, "application/json");
                var xxx = JsonConvert.SerializeObject(req);

                string path = "/merchant/core/mcb/card/transaction";
                clients.DefaultRequestHeaders.Add("X-WSB-API-KEY", KeyModel.WASABICARD_API_KEY);
                clients.DefaultRequestHeaders.Add("X-WSB-SIGNATURE", signData(xxx, loadRsaPrivateKeyPem()));
                HttpResponseMessage responses = clients.PostAsync(path, httpContent).Result;

                api.Type = "Wasabi Card - Get Card Transaction";
                api.Request = xxx;
                api.RequestDate = DateTime.Now;

                if (responses.IsSuccessStatusCode)
                {
                    //att = await response.Content.ReadAsAsync<AirportTransferTransaction>();
                    string resultJSON = responses.Content.ReadAsStringAsync().Result;
                    if (responses.StatusCode == HttpStatusCode.OK)
                    {
                        api.Response = resultJSON;
                        api.ResponseDate = DateTime.Now;
                        db.tblH_API_Log.Add(api);
                        db.SaveChanges();
                        response = JsonConvert.DeserializeObject<WCCardTransactionResponseModel>(resultJSON);
                        return response;
                    }
                    else
                    {
                        var x = JsonConvert.DeserializeObject<WCCardTransactionResponseModel>(resultJSON);

                        api.Response = responses.StatusCode.ToString() + " - " + responses.Content.ReadAsStringAsync().Result;
                        api.ResponseDate = DateTime.Now;
                        db.tblH_API_Log.Add(api);
                        db.SaveChanges();
                        return response = null;
                    }
                }
                else
                {
                    api.Response = responses.StatusCode.ToString() + " - " + responses.Content.ReadAsStringAsync().Result;
                    api.ResponseDate = DateTime.Now;
                    db.tblH_API_Log.Add(api);
                    db.SaveChanges();
                    return response = null;
                }
            }
            catch (Exception ex)
            {
                api.Response = ex.Message;
                api.ResponseDate = DateTime.Now;
                db.tblH_API_Log.Add(api);
                db.SaveChanges();
                return response = null;
            }
        }

        public static WCCardInfoResponseModel getCardInfo(WCCardInfoRequestModel req)
        {
            DBEntities db = new DBEntities();
            tblH_API_Log api = new tblH_API_Log();
            WCCardInfoResponseModel response = new WCCardInfoResponseModel();
            try
            {
                HttpClient clients = new HttpClient();
                clients.BaseAddress = new Uri(KeyModel.WASABICARD_API_URL);
                clients.Timeout.Add(new TimeSpan(0, 0, 5));
                clients.DefaultRequestHeaders.Accept.Clear();

                clients.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

                var httpContent = new StringContent(JsonConvert.SerializeObject(req), Encoding.UTF8, "application/json");
                var xxx = JsonConvert.SerializeObject(req);

                string path = "/merchant/core/mcb/card/info";
                clients.DefaultRequestHeaders.Add("X-WSB-API-KEY", KeyModel.WASABICARD_API_KEY);
                clients.DefaultRequestHeaders.Add("X-WSB-SIGNATURE", signData(xxx, loadRsaPrivateKeyPem()));
                HttpResponseMessage responses = clients.PostAsync(path, httpContent).Result;

                api.Type = "Wasabi Card - Get Card Info";
                api.Request = xxx;
                api.RequestDate = DateTime.Now;

                if (responses.IsSuccessStatusCode)
                {
                    //att = await response.Content.ReadAsAsync<AirportTransferTransaction>();
                    string resultJSON = responses.Content.ReadAsStringAsync().Result;
                    if (responses.StatusCode == HttpStatusCode.OK)
                    {
                        api.Response = resultJSON;
                        api.ResponseDate = DateTime.Now;
                        db.tblH_API_Log.Add(api);
                        db.SaveChanges();
                        response = JsonConvert.DeserializeObject<WCCardInfoResponseModel>(resultJSON);
                        return response;
                    }
                    else
                    {
                        var x = JsonConvert.DeserializeObject<WCCardInfoResponseModel>(resultJSON);

                        api.Response = responses.StatusCode.ToString() + " - " + responses.Content.ReadAsStringAsync().Result;
                        api.ResponseDate = DateTime.Now;
                        db.tblH_API_Log.Add(api);
                        db.SaveChanges();
                        return response = null;
                    }
                }
                else
                {
                    api.Response = responses.StatusCode.ToString() + " - " + responses.Content.ReadAsStringAsync().Result;
                    api.ResponseDate = DateTime.Now;
                    db.tblH_API_Log.Add(api);
                    db.SaveChanges();
                    return response = null;
                }
            }
            catch (Exception ex)
            {
                api.Response = ex.Message;
                api.ResponseDate = DateTime.Now;
                db.tblH_API_Log.Add(api);
                db.SaveChanges();
                return response = null;
            }
        }

        public static WCCardInfoSensitiveResponseModel getCardInfoSensitive(WCCardInfoSensitiveRequestModel req)
        {
            DBEntities db = new DBEntities();
            tblH_API_Log api = new tblH_API_Log();
            WCCardInfoSensitiveResponseModel response = new WCCardInfoSensitiveResponseModel();
            try
            {
                HttpClient clients = new HttpClient();
                clients.BaseAddress = new Uri(KeyModel.WASABICARD_API_URL);
                clients.Timeout.Add(new TimeSpan(0, 0, 5));
                clients.DefaultRequestHeaders.Accept.Clear();

                clients.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

                var httpContent = new StringContent(JsonConvert.SerializeObject(req), Encoding.UTF8, "application/json");
                var xxx = JsonConvert.SerializeObject(req);

                string path = "/merchant/core/mcb/card/sensitive";
                clients.DefaultRequestHeaders.Add("X-WSB-API-KEY", KeyModel.WASABICARD_API_KEY);
                clients.DefaultRequestHeaders.Add("X-WSB-SIGNATURE", signData(xxx, loadRsaPrivateKeyPem()));
                HttpResponseMessage responses = clients.PostAsync(path, httpContent).Result;

                api.Type = "Wasabi Card - Get Card Info";
                api.Request = xxx;
                api.RequestDate = DateTime.Now;

                if (responses.IsSuccessStatusCode)
                {
                    //att = await response.Content.ReadAsAsync<AirportTransferTransaction>();
                    string resultJSON = responses.Content.ReadAsStringAsync().Result;
                    if (responses.StatusCode == HttpStatusCode.OK)
                    {
                        api.Response = resultJSON;
                        api.ResponseDate = DateTime.Now;
                        db.tblH_API_Log.Add(api);
                        db.SaveChanges();
                        response = JsonConvert.DeserializeObject<WCCardInfoSensitiveResponseModel>(resultJSON);
                        return response;
                    }
                    else
                    {
                        var x = JsonConvert.DeserializeObject<WCCardInfoSensitiveResponseModel>(resultJSON);

                        api.Response = responses.StatusCode.ToString() + " - " + responses.Content.ReadAsStringAsync().Result;
                        api.ResponseDate = DateTime.Now;
                        db.tblH_API_Log.Add(api);
                        db.SaveChanges();
                        return response = null;
                    }
                }
                else
                {
                    api.Response = responses.StatusCode.ToString() + " - " + responses.Content.ReadAsStringAsync().Result;
                    api.ResponseDate = DateTime.Now;
                    db.tblH_API_Log.Add(api);
                    db.SaveChanges();
                    return response = null;
                }
            }
            catch (Exception ex)
            {
                api.Response = ex.Message;
                api.ResponseDate = DateTime.Now;
                db.tblH_API_Log.Add(api);
                db.SaveChanges();
                return response = null;
            }
        }

        public static WCCreateHolderResponseModel createHolder(WCCreateHolderRequestModel req)
        {
            DBEntities db = new DBEntities();
            tblH_API_Log api = new tblH_API_Log();
            WCCreateHolderResponseModel response = new WCCreateHolderResponseModel();
            try
            {
                HttpClient clients = new HttpClient();
                clients.BaseAddress = new Uri(KeyModel.WASABICARD_API_URL);
                clients.Timeout.Add(new TimeSpan(0, 0, 5));
                clients.DefaultRequestHeaders.Accept.Clear();

                clients.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

                var httpContent = new StringContent(JsonConvert.SerializeObject(req), Encoding.UTF8, "application/json");
                var xxx = JsonConvert.SerializeObject(req);

                string path = "/merchant/core/mcb/card/holder/create";
                clients.DefaultRequestHeaders.Add("X-WSB-API-KEY", KeyModel.WASABICARD_API_KEY);
                clients.DefaultRequestHeaders.Add("X-WSB-SIGNATURE", signData(xxx, loadRsaPrivateKeyPem()));
                HttpResponseMessage responses = clients.PostAsync(path, httpContent).Result;

                api.Type = "Wasabi Card - Create Holder";
                api.Request = xxx;
                api.RequestDate = DateTime.Now;

                if (responses.IsSuccessStatusCode)
                {
                    //att = await response.Content.ReadAsAsync<AirportTransferTransaction>();
                    string resultJSON = responses.Content.ReadAsStringAsync().Result;
                    if (responses.StatusCode == HttpStatusCode.OK)
                    {
                        api.Response = resultJSON;
                        api.ResponseDate = DateTime.Now;
                        db.tblH_API_Log.Add(api);
                        db.SaveChanges();
                        response = JsonConvert.DeserializeObject<WCCreateHolderResponseModel>(resultJSON);
                        return response;
                    }
                    else
                    {
                        var x = JsonConvert.DeserializeObject<WCCreateHolderResponseModel>(resultJSON);

                        api.Response = responses.StatusCode.ToString() + " - " + responses.Content.ReadAsStringAsync().Result;
                        api.ResponseDate = DateTime.Now;
                        db.tblH_API_Log.Add(api);
                        db.SaveChanges();
                        return response;
                    }
                }
                else
                {
                    api.Response = responses.StatusCode.ToString() + " - " + responses.Content.ReadAsStringAsync().Result;
                    api.ResponseDate = DateTime.Now;
                    db.tblH_API_Log.Add(api);
                    db.SaveChanges();
                    return response;
                }
            }
            catch (Exception ex)
            {
                api.Response = ex.Message;
                api.ResponseDate = DateTime.Now;
                db.tblH_API_Log.Add(api);
                db.SaveChanges();
                return response = null;
            }
        }

        public static WCGetHolderResponseModel getHolderList(WCGetHolderRequestModel req)
        {
            DBEntities db = new DBEntities();
            tblH_API_Log api = new tblH_API_Log();
            WCGetHolderResponseModel response = new WCGetHolderResponseModel();
            try
            {
                HttpClient clients = new HttpClient();
                clients.BaseAddress = new Uri(KeyModel.WASABICARD_API_URL);
                clients.Timeout.Add(new TimeSpan(0, 0, 5));
                clients.DefaultRequestHeaders.Accept.Clear();

                clients.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

                var httpContent = new StringContent(JsonConvert.SerializeObject(req), Encoding.UTF8, "application/json");
                var xxx = JsonConvert.SerializeObject(req);

                string path = "/merchant/core/mcb/card/holder/query";
                clients.DefaultRequestHeaders.Add("X-WSB-API-KEY", KeyModel.WASABICARD_API_KEY);
                clients.DefaultRequestHeaders.Add("X-WSB-SIGNATURE", signData(xxx, loadRsaPrivateKeyPem()));
                HttpResponseMessage responses = clients.PostAsync(path, httpContent).Result;

                api.Type = "Wasabi Card - Get Holder List";
                api.Request = xxx;
                api.RequestDate = DateTime.Now;

                if (responses.IsSuccessStatusCode)
                {
                    //att = await response.Content.ReadAsAsync<AirportTransferTransaction>();
                    string resultJSON = responses.Content.ReadAsStringAsync().Result;
                    if (responses.StatusCode == HttpStatusCode.OK)
                    {
                        api.Response = resultJSON;
                        api.ResponseDate = DateTime.Now;
                        db.tblH_API_Log.Add(api);
                        db.SaveChanges();
                        response = JsonConvert.DeserializeObject<WCGetHolderResponseModel>(resultJSON);
                        return response;
                    }
                    else
                    {
                        var x = JsonConvert.DeserializeObject<WCGetHolderResponseModel>(resultJSON);

                        api.Response = responses.StatusCode.ToString() + " - " + responses.Content.ReadAsStringAsync().Result;
                        api.ResponseDate = DateTime.Now;
                        db.tblH_API_Log.Add(api);
                        db.SaveChanges();
                        return response = null;
                    }
                }
                else
                {
                    api.Response = responses.StatusCode.ToString() + " - " + responses.Content.ReadAsStringAsync().Result;
                    api.ResponseDate = DateTime.Now;
                    db.tblH_API_Log.Add(api);
                    db.SaveChanges();
                    return response = null;
                }
            }
            catch (Exception ex)
            {
                api.Response = ex.Message;
                api.ResponseDate = DateTime.Now;
                db.tblH_API_Log.Add(api);
                db.SaveChanges();
                return response = null;
            }
        }

        public static WCDepositCardResponseModel depositCard(WCDepositCardRequestModel req)
        {
            DBEntities db = new DBEntities();
            tblH_API_Log api = new tblH_API_Log();
            WCDepositCardResponseModel response = new WCDepositCardResponseModel();
            try
            {
                HttpClient clients = new HttpClient();
                clients.BaseAddress = new Uri(KeyModel.WASABICARD_API_URL);
                clients.Timeout.Add(new TimeSpan(0, 0, 5));
                clients.DefaultRequestHeaders.Accept.Clear();

                clients.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

                var httpContent = new StringContent(JsonConvert.SerializeObject(req), Encoding.UTF8, "application/json");
                var xxx = JsonConvert.SerializeObject(req);

                string path = "/merchant/core/mcb/card/deposit";
                clients.DefaultRequestHeaders.Add("X-WSB-API-KEY", KeyModel.WASABICARD_API_KEY);
                clients.DefaultRequestHeaders.Add("X-WSB-SIGNATURE", signData(xxx, loadRsaPrivateKeyPem()));
                HttpResponseMessage responses = clients.PostAsync(path, httpContent).Result;

                api.Type = "Wasabi Card - Deposit Card";
                api.Request = xxx;
                api.RequestDate = DateTime.Now;

                if (responses.IsSuccessStatusCode)
                {
                    //att = await response.Content.ReadAsAsync<AirportTransferTransaction>();
                    string resultJSON = responses.Content.ReadAsStringAsync().Result;
                    if (responses.StatusCode == HttpStatusCode.OK)
                    {
                        api.Response = resultJSON;
                        api.ResponseDate = DateTime.Now;
                        db.tblH_API_Log.Add(api);
                        db.SaveChanges();
                        response = JsonConvert.DeserializeObject<WCDepositCardResponseModel>(resultJSON);
                        return response;
                    }
                    else
                    {
                        var x = JsonConvert.DeserializeObject<WCDepositCardResponseModel>(resultJSON);

                        api.Response = responses.StatusCode.ToString() + " - " + responses.Content.ReadAsStringAsync().Result;
                        api.ResponseDate = DateTime.Now;
                        db.tblH_API_Log.Add(api);
                        db.SaveChanges();
                        return response = null;
                    }
                }
                else
                {
                    api.Response = responses.StatusCode.ToString() + " - " + responses.Content.ReadAsStringAsync().Result;
                    api.ResponseDate = DateTime.Now;
                    db.tblH_API_Log.Add(api);
                    db.SaveChanges();
                    return response = null;
                }
            }
            catch (Exception ex)
            {
                api.Response = ex.Message;
                api.ResponseDate = DateTime.Now;
                db.tblH_API_Log.Add(api);
                db.SaveChanges();
                return response = null;
            }
        }


        public static WCCancelCardResponseModel cancelCard(WCCancelCardRequestModel req)
        {
            DBEntities db = new DBEntities();
            tblH_API_Log api = new tblH_API_Log();
            WCCancelCardResponseModel response = new WCCancelCardResponseModel();
            try
            {
                HttpClient clients = new HttpClient();
                clients.BaseAddress = new Uri(KeyModel.WASABICARD_API_URL);
                clients.Timeout.Add(new TimeSpan(0, 0, 5));
                clients.DefaultRequestHeaders.Accept.Clear();

                clients.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

                var httpContent = new StringContent(JsonConvert.SerializeObject(req), Encoding.UTF8, "application/json");
                var xxx = JsonConvert.SerializeObject(req);

                string path = "/merchant/core/mcb/card/cancel";
                clients.DefaultRequestHeaders.Add("X-WSB-API-KEY", KeyModel.WASABICARD_API_KEY);
                clients.DefaultRequestHeaders.Add("X-WSB-SIGNATURE", signData(xxx, loadRsaPrivateKeyPem()));
                HttpResponseMessage responses = clients.PostAsync(path, httpContent).Result;

                api.Type = "Wasabi Card - Cancel Card";
                api.Request = xxx;
                api.RequestDate = DateTime.Now;

                if (responses.IsSuccessStatusCode)
                {
                    //att = await response.Content.ReadAsAsync<AirportTransferTransaction>();
                    string resultJSON = responses.Content.ReadAsStringAsync().Result;
                    if (responses.StatusCode == HttpStatusCode.OK)
                    {
                        api.Response = resultJSON;
                        api.ResponseDate = DateTime.Now;
                        db.tblH_API_Log.Add(api);
                        db.SaveChanges();
                        response = JsonConvert.DeserializeObject<WCCancelCardResponseModel>(resultJSON);
                        return response;
                    }
                    else
                    {
                        var x = JsonConvert.DeserializeObject<WCCancelCardResponseModel>(resultJSON);

                        api.Response = responses.StatusCode.ToString() + " - " + responses.Content.ReadAsStringAsync().Result;
                        api.ResponseDate = DateTime.Now;
                        db.tblH_API_Log.Add(api);
                        db.SaveChanges();
                        return response = null;
                    }
                }
                else
                {
                    api.Response = responses.StatusCode.ToString() + " - " + responses.Content.ReadAsStringAsync().Result;
                    api.ResponseDate = DateTime.Now;
                    db.tblH_API_Log.Add(api);
                    db.SaveChanges();
                    return response = null;
                }
            }
            catch (Exception ex)
            {
                api.Response = ex.Message;
                api.ResponseDate = DateTime.Now;
                db.tblH_API_Log.Add(api);
                db.SaveChanges();
                return response = null;
            }
        }
    }
}