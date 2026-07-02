using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Web;
using Newtonsoft.Json;
using QryptoCard.INT.Model;
using QryptoCard.INT.Model.PGCrypto;

namespace QryptoCard.INT.Script.Gateway.PGCrypto
{
    public static class PGCryptoService
    {
        private static string credentials()
        {
            return Convert.ToBase64String(Encoding.ASCII.GetBytes(KeyModel.PGCRYPTO_API_KEY + ":" + KeyModel.PGCRYPTO_SECRET_KEY));
        }

        public static AddressStaticModel addressStaticCreation(AddressStaticModel req)
        {
            DBEntities db = new DBEntities();
            tblH_API_Log api = new tblH_API_Log();
            AddressStaticModel response = new AddressStaticModel();
            try
            {
                HttpClient clients = new HttpClient();
                clients.BaseAddress = new Uri(KeyModel.PGCRYPTO_API_URL);
                clients.Timeout.Add(new TimeSpan(0, 0, 5));
                clients.DefaultRequestHeaders.Accept.Clear();

                clients.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

                string path = "/v1/address/static/generate";
                clients.DefaultRequestHeaders.Add("Authorization", "Basic " + credentials());
                var httpContent = new StringContent(JsonConvert.SerializeObject(req), Encoding.UTF8, "application/json");
                var xxx = JsonConvert.SerializeObject(req);
                HttpResponseMessage responses = clients.PostAsync(path, httpContent).Result;

                api.Type = "PGCrypto - Static Address Creation";
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
                        var x = JsonConvert.DeserializeObject<PGOutputModel>(resultJSON);
                        if (x.Status == "success")
                            return response = JsonConvert.DeserializeObject<AddressStaticModel>(x.Data.ToString());
                        else
                            return response = null;
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
                api.Response = ex.ToString();
                api.ResponseDate = DateTime.Now;
                db.tblH_API_Log.Add(api);
                db.SaveChanges();
                return response = null;
            }
        }

        public static List<CoinModel> getCoin()
        {
            DBEntities db = new DBEntities();
            tblH_API_Log api = new tblH_API_Log();
            List<CoinModel> response = new List<CoinModel>();
            try
            {
                HttpClient clients = new HttpClient();
                clients.BaseAddress = new Uri(KeyModel.PGCRYPTO_API_URL);
                clients.Timeout.Add(new TimeSpan(0, 0, 5));
                clients.DefaultRequestHeaders.Accept.Clear();

                clients.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

                string path = "/v1/master/data/coin";
                clients.DefaultRequestHeaders.Add("Authorization", "Basic " + credentials());
                HttpResponseMessage responses = clients.GetAsync(path).Result;

                api.Type = "PGCrypto - List of Coin";
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
                        var x = JsonConvert.DeserializeObject<PGOutputModel>(resultJSON);
                        if (x.Status == "success")
                            return response = JsonConvert.DeserializeObject<List<CoinModel>>(x.Data.ToString());
                        else
                            return response = null;
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
                api.Response = ex.ToString();
                api.ResponseDate = DateTime.Now;
                db.tblH_API_Log.Add(api);
                db.SaveChanges();
                return response = null;
            }
        }

        public static List<TokenModel> getToken(string netw)
        {
            DBEntities db = new DBEntities();
            tblH_API_Log api = new tblH_API_Log();
            List<TokenModel> response = new List<TokenModel>();
            try
            {
                HttpClient clients = new HttpClient();
                clients.BaseAddress = new Uri(KeyModel.PGCRYPTO_API_URL);
                clients.Timeout.Add(new TimeSpan(0, 0, 5));
                clients.DefaultRequestHeaders.Accept.Clear();

                clients.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

                string path = "/v1/master/data/coin";
                clients.DefaultRequestHeaders.Add("Authorization", "Basic " + credentials());
                HttpResponseMessage responses = clients.GetAsync(path).Result;

                api.Type = "PGCrypto - List of Token : " + netw;
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
                        var x = JsonConvert.DeserializeObject<PGOutputModel>(resultJSON);
                        if (x.Status == "success")
                            return response = JsonConvert.DeserializeObject<List<TokenModel>>(resultJSON);
                        else
                            return response = null;
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
                api.Response = ex.ToString();
                api.ResponseDate = DateTime.Now;
                db.tblH_API_Log.Add(api);
                db.SaveChanges();
                return response = null;
            }
        }

        public static CustomerModel addCustomer(CustomerModel req)
        {
            DBEntities db = new DBEntities();
            tblH_API_Log api = new tblH_API_Log();
            CustomerModel response = new CustomerModel();
            try
            {
                HttpClient clients = new HttpClient();
                clients.BaseAddress = new Uri(KeyModel.PGCRYPTO_API_URL);
                clients.Timeout.Add(new TimeSpan(0, 0, 5));
                clients.DefaultRequestHeaders.Accept.Clear();

                clients.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

                string path = "/v1/customer";
                clients.DefaultRequestHeaders.Add("Authorization", "Basic " + credentials());
                var httpContent = new StringContent(JsonConvert.SerializeObject(req), Encoding.UTF8, "application/json");
                var xxx = JsonConvert.SerializeObject(req);
                HttpResponseMessage responses = clients.PostAsync(path, httpContent).Result;

                api.Type = "PGCrypto - Add Customer";
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
                        var x = JsonConvert.DeserializeObject<PGOutputModel>(resultJSON);
                        if (x.Status == "success")
                            return response = JsonConvert.DeserializeObject<CustomerModel>(x.Data.ToString());
                        else
                            return response = null;
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
                api.Response = ex.ToString();
                api.ResponseDate = DateTime.Now;
                db.tblH_API_Log.Add(api);
                db.SaveChanges();
                return response = null;
            }
        }

        public static InvoiceModel createInvoice(InvoiceModel req)
        {
            DBEntities db = new DBEntities();
            tblH_API_Log api = new tblH_API_Log();
            InvoiceModel response = new InvoiceModel();
            try
            {
                HttpClient clients = new HttpClient();
                clients.BaseAddress = new Uri(KeyModel.PGCRYPTO_API_URL);
                clients.Timeout.Add(new TimeSpan(0, 0, 5));
                clients.DefaultRequestHeaders.Accept.Clear();

                clients.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

                string path = "/v1/invoice";
                clients.DefaultRequestHeaders.Add("Authorization", "Basic " + credentials());
                var httpContent = new StringContent(JsonConvert.SerializeObject(req), Encoding.UTF8, "application/json");
                var xxx = JsonConvert.SerializeObject(req);
                HttpResponseMessage responses = clients.PostAsync(path, httpContent).Result;

                api.Type = "PGCrypto - CreateInvoice";
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
                        var x = JsonConvert.DeserializeObject<PGOutputModel>(resultJSON);
                        if (x.Status == "success")
                            return response = JsonConvert.DeserializeObject<InvoiceModel>(x.Data.ToString());
                        else
                            return response = null;
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
                api.Response = ex.ToString();
                api.ResponseDate = DateTime.Now;
                db.tblH_API_Log.Add(api);
                db.SaveChanges();
                return response = null;
            }
        }

        // Create a Runegate PAYMENT REQUEST (a dynamic-address transaction for a custom amount, tagged
        // with our PartnerReferenceID). Unlike createInvoice this needs no pre-registered product or
        // customer — just CoinID + Amount + MerchantID + PartnerReferenceID (+ optional IdempotencyKey
        // for gateway-side dedup). Returns the created transaction (Address + TransactionID + real
        // DateExpired) on success, or null on any failure. Mirrors createInvoice's proxy/log shape.
        public static TransactionModel createPayment(TransactionModel req)
        {
            DBEntities db = new DBEntities();
            tblH_API_Log api = new tblH_API_Log();
            TransactionModel response = new TransactionModel();
            try
            {
                using (HttpClient clients = new HttpClient())
                {
                    clients.BaseAddress = new Uri(KeyModel.PGCRYPTO_API_URL);
                    clients.Timeout = TimeSpan.FromSeconds(5);   // real 5s cap (HttpClient.Timeout is a value; .Add was a no-op leaving the 100s default)
                    clients.DefaultRequestHeaders.Accept.Clear();

                    clients.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                    ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

                    string path = "/v1/payment";
                    clients.DefaultRequestHeaders.Add("Authorization", "Basic " + credentials());
                    var httpContent = new StringContent(JsonConvert.SerializeObject(req), Encoding.UTF8, "application/json");
                    var xxx = JsonConvert.SerializeObject(req);
                    HttpResponseMessage responses = clients.PostAsync(path, httpContent).Result;

                    api.Type = "PGCrypto - CreatePayment";
                    api.Request = xxx;
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
                            var x = JsonConvert.DeserializeObject<PGOutputModel>(resultJSON);
                            if (x.Status == "success")
                                return response = JsonConvert.DeserializeObject<TransactionModel>(x.Data.ToString());
                            else
                                return response = null;
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
            }
            catch (Exception ex)
            {
                api.Response = ex.ToString();
                api.ResponseDate = DateTime.Now;
                db.tblH_API_Log.Add(api);
                db.SaveChanges();
                return response = null;
            }
        }
    }
}