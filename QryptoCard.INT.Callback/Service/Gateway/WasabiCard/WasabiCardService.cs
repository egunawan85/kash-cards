using Newtonsoft.Json;
using QryptoCard.INT.Callback.Model;
using QryptoCard.INT.Callback.Model.WasabiCard;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Web;

namespace QryptoCard.INT.Callback.Service.Gateway.WasabiCard
{
    public class WasabiCardService
    {
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

        /// <summary>
        /// Read the merchant USD wallet float (availableBalance) that card opens/top-ups draw
        /// against. Read-only — no money moves. Used by the balance monitor / coverage check and
        /// (when auto-funding is enabled) the floor-refill trigger. Returns null on any failure so
        /// callers fail closed (never act on a missing/garbled balance).
        /// </summary>
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
                HttpResponseMessage responses = clients.PostAsync(path, httpContent).Result;

                api.Type = "Wasabi Card - Get Account Info";
                api.RequestDate = DateTime.Now;

                if (responses.IsSuccessStatusCode)
                {
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

        // Post-verify cross-check support: fetch the canonical record for a single card-deposit
        // order from WasabiCard's /card/v2/transaction query, so the callback can confirm a
        // claimed deposit outcome against the provider before crediting. Returns the matching
        // record, or null on any error / non-OK / not-found (the caller treats null as
        // "unconfirmed" and withholds the credit — fail-closed).
        public static WCCardTransactionResponseModel.Record getDepositOperation(string merchantOrderNo)
        {
            DBEntities db = new DBEntities();
            tblH_API_Log api = new tblH_API_Log();
            try
            {
                if (string.IsNullOrEmpty(merchantOrderNo)) return null;

                WCCardTransactionRequestModel req = new WCCardTransactionRequestModel();
                req.pageNum = 1;
                req.pageSize = 50;
                req.type = "deposit";
                req.merchantOrderNo = merchantOrderNo;

                HttpClient clients = new HttpClient();
                clients.BaseAddress = new Uri(KeyModel.WASABICARD_API_URL);
                clients.Timeout = new TimeSpan(0, 0, 5); // 5s cap (Timeout.Add was a no-op -> 100s default); the sweep iterates many orders
                clients.DefaultRequestHeaders.Accept.Clear();
                clients.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

                var xxx = JsonConvert.SerializeObject(req);
                var httpContent = new StringContent(xxx, Encoding.UTF8, "application/json");

                string path = "/merchant/core/mcb/card/v2/transaction";
                clients.DefaultRequestHeaders.Add("X-WSB-API-KEY", KeyModel.WASABICARD_API_KEY);
                clients.DefaultRequestHeaders.Add("X-WSB-SIGNATURE", signData(xxx, loadRsaPrivateKeyPem()));
                HttpResponseMessage responses = clients.PostAsync(path, httpContent).Result;

                api.Type = "Wasabi Card - Deposit Cross-Check";
                api.Request = xxx;
                api.RequestDate = DateTime.Now;

                string resultJSON = responses.Content.ReadAsStringAsync().Result;
                api.Response = (int)responses.StatusCode + " - " + resultJSON;
                api.ResponseDate = DateTime.Now;
                db.tblH_API_Log.Add(api);
                db.SaveChanges();

                if (responses.StatusCode != HttpStatusCode.OK) return null;

                var parsed = JsonConvert.DeserializeObject<WCCardTransactionResponseModel>(resultJSON);
                if (parsed == null || parsed.data == null || parsed.data.records == null) return null;

                return parsed.data.records.FirstOrDefault(r => r != null && r.merchantOrderNo == merchantOrderNo);
            }
            catch (Exception ex)
            {
                api.Response = ex.Message;
                api.ResponseDate = DateTime.Now;
                db.tblH_API_Log.Add(api);
                db.SaveChanges();
                return null;
            }
        }

        // Twin of getDepositOperation for card-OPEN orders: fetch the "create" operation for a
        // merchantOrderNo from WasabiCard's /card/v2/transaction query, so the reconciliation sweep
        // can confirm a pending open's outcome against the provider. Returns the matching record, or
        // null on any error / non-OK / not-found (the caller treats null as "unconfirmed" — fail-closed).
        public static WCCardTransactionResponseModel.Record getCreateOperation(string merchantOrderNo)
        {
            DBEntities db = new DBEntities();
            tblH_API_Log api = new tblH_API_Log();
            try
            {
                if (string.IsNullOrEmpty(merchantOrderNo)) return null;

                WCCardTransactionRequestModel req = new WCCardTransactionRequestModel();
                req.pageNum = 1;
                req.pageSize = 50;
                req.type = "create";
                req.merchantOrderNo = merchantOrderNo;

                HttpClient clients = new HttpClient();
                clients.BaseAddress = new Uri(KeyModel.WASABICARD_API_URL);
                clients.Timeout = new TimeSpan(0, 0, 5); // 5s cap (Timeout.Add was a no-op -> 100s default); the sweep iterates many orders
                clients.DefaultRequestHeaders.Accept.Clear();
                clients.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

                var xxx = JsonConvert.SerializeObject(req);
                var httpContent = new StringContent(xxx, Encoding.UTF8, "application/json");

                string path = "/merchant/core/mcb/card/v2/transaction";
                clients.DefaultRequestHeaders.Add("X-WSB-API-KEY", KeyModel.WASABICARD_API_KEY);
                clients.DefaultRequestHeaders.Add("X-WSB-SIGNATURE", signData(xxx, loadRsaPrivateKeyPem()));
                HttpResponseMessage responses = clients.PostAsync(path, httpContent).Result;

                api.Type = "Wasabi Card - Create Cross-Check";
                api.Request = xxx;
                api.RequestDate = DateTime.Now;

                string resultJSON = responses.Content.ReadAsStringAsync().Result;
                api.Response = (int)responses.StatusCode + " - " + resultJSON;
                api.ResponseDate = DateTime.Now;
                db.tblH_API_Log.Add(api);
                db.SaveChanges();

                if (responses.StatusCode != HttpStatusCode.OK) return null;

                var parsed = JsonConvert.DeserializeObject<WCCardTransactionResponseModel>(resultJSON);
                if (parsed == null || parsed.data == null || parsed.data.records == null) return null;

                return parsed.data.records.FirstOrDefault(r => r != null && r.merchantOrderNo == merchantOrderNo);
            }
            catch (Exception ex)
            {
                api.Response = ex.Message;
                api.ResponseDate = DateTime.Now;
                db.tblH_API_Log.Add(api);
                db.SaveChanges();
                return null;
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

                api.Type = "Wasabi Card - Get Card Info Sensitive";
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
    }
}