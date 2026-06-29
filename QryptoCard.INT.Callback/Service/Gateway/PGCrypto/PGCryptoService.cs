using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using Newtonsoft.Json;
using QryptoCard.INT.Callback.Model;
using QryptoCard.INT.Callback.Model.PGCrypto;

namespace QryptoCard.INT.Callback.Service.Gateway.PGCrypto
{
    /// <summary>
    /// Callback-tier Runegate client for the OUTBOUND transfer used by WasabiCard auto-funding.
    /// Mirrors the INT-tier PGCryptoService HTTP/auth/logging pattern (Basic auth over TLS 1.2,
    /// every call journalled to tblH_API_Log). Only the methods the money-out path needs:
    /// getCoin/getToken (to resolve the TRON CoinID + USDT-TRC20 TokenID without hardcoding GUIDs)
    /// and createTransfer (POST /v1/transfer).
    /// </summary>
    public static class PGCryptoService
    {
        private static string credentials()
        {
            return Convert.ToBase64String(Encoding.ASCII.GetBytes(
                KeyModel.PGCRYPTO_API_KEY + ":" + KeyModel.PGCRYPTO_SECRET_KEY));
        }

        private static HttpClient NewClient(int timeoutSeconds)
        {
            HttpClient c = new HttpClient();
            c.BaseAddress = new Uri(KeyModel.PGCRYPTO_API_URL);
            c.Timeout = TimeSpan.FromSeconds(timeoutSeconds);
            c.DefaultRequestHeaders.Accept.Clear();
            c.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            c.DefaultRequestHeaders.Add("Authorization", "Basic " + credentials());
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            return c;
        }

        public static List<CoinModel> getCoin()
        {
            DBEntities db = new DBEntities();
            tblH_API_Log api = new tblH_API_Log { Type = "PGCrypto - List of Coin (autofund)", RequestDate = DateTime.Now };
            try
            {
                using (HttpClient clients = NewClient(10))
                {
                    HttpResponseMessage responses = clients.GetAsync("/v1/master/data/coin").Result;
                    string resultJSON = responses.Content.ReadAsStringAsync().Result;
                    api.Response = (responses.IsSuccessStatusCode ? "" : responses.StatusCode + " - ") + resultJSON;
                    api.ResponseDate = DateTime.Now;
                    db.tblH_API_Log.Add(api); db.SaveChanges();
                    if (!responses.IsSuccessStatusCode) return null;
                    var x = JsonConvert.DeserializeObject<PGOutputModel>(resultJSON);
                    if (x == null || x.Status != "success") return null;
                    return JsonConvert.DeserializeObject<List<CoinModel>>(x.Data.ToString());
                }
            }
            catch (Exception ex)
            {
                api.Response = ex.Message; api.ResponseDate = DateTime.Now;
                db.tblH_API_Log.Add(api); db.SaveChanges();
                return null;
            }
        }

        public static List<TokenModel> getToken(string network)
        {
            DBEntities db = new DBEntities();
            tblH_API_Log api = new tblH_API_Log { Type = "PGCrypto - List of Token (autofund) : " + network, RequestDate = DateTime.Now };
            try
            {
                using (HttpClient clients = NewClient(10))
                {
                    HttpResponseMessage responses = clients.GetAsync("/v1/master/data/token/" + network).Result;
                    string resultJSON = responses.Content.ReadAsStringAsync().Result;
                    api.Response = (responses.IsSuccessStatusCode ? "" : responses.StatusCode + " - ") + resultJSON;
                    api.ResponseDate = DateTime.Now;
                    db.tblH_API_Log.Add(api); db.SaveChanges();
                    if (!responses.IsSuccessStatusCode) return null;
                    var x = JsonConvert.DeserializeObject<PGOutputModel>(resultJSON);
                    if (x == null || x.Status != "success") return null;
                    return JsonConvert.DeserializeObject<List<TokenModel>>(x.Data.ToString());
                }
            }
            catch (Exception ex)
            {
                api.Response = ex.Message; api.ResponseDate = DateTime.Now;
                db.tblH_API_Log.Add(api); db.SaveChanges();
                return null;
            }
        }

        /// <summary>
        /// Submit an outbound transfer. Returns a TransferOutcome that distinguishes Submitted /
        /// DefinitiveReject / Ambiguous so the caller never double-sends on an ambiguous failure.
        /// Always logs the full request/response to tblH_API_Log (money-out audit trail).
        /// </summary>
        public static TransferOutcome createTransfer(TransferRequestModel req)
        {
            DBEntities db = new DBEntities();
            string reqJson = JsonConvert.SerializeObject(req);
            tblH_API_Log api = new tblH_API_Log
            {
                Type = "PGCrypto - Transfer (autofund)",
                Request = reqJson,
                RequestDate = DateTime.Now
            };
            var outcome = new TransferOutcome();
            try
            {
                using (HttpClient clients = NewClient(20))
                {
                    var httpContent = new StringContent(reqJson, Encoding.UTF8, "application/json");
                    HttpResponseMessage responses = clients.PostAsync("/v1/transfer", httpContent).Result;
                    string resultJSON = responses.Content.ReadAsStringAsync().Result;
                    outcome.Raw = resultJSON;

                    api.Response = (responses.IsSuccessStatusCode ? "" : responses.StatusCode + " - ") + resultJSON;
                    api.ResponseDate = DateTime.Now;
                    db.tblH_API_Log.Add(api); db.SaveChanges();

                    // Parse the envelope. A clean parse (success or explicit non-success) is a
                    // DEFINITIVE provider answer; only a 5xx/garbled/no response is ambiguous.
                    PGOutputModel env = null;
                    try { env = JsonConvert.DeserializeObject<PGOutputModel>(resultJSON); } catch { env = null; }

                    if (responses.IsSuccessStatusCode && env != null && env.Status == "success")
                    {
                        outcome.Submitted = true;
                        outcome.EnvelopeStatus = env.Status;
                        try { outcome.Result = JsonConvert.DeserializeObject<TransferResultModel>(env.Data.ToString()); } catch { }
                        return outcome;
                    }

                    // A parseable envelope with a non-success status, or a 4xx with a body, is a
                    // definitive rejection — the provider did NOT accept/execute the transfer.
                    int code = (int)responses.StatusCode;
                    if (env != null && (code == 200 || (code >= 400 && code < 500)))
                    {
                        outcome.DefinitiveReject = true;
                        outcome.EnvelopeStatus = env.Status;
                        return outcome;
                    }

                    // 5xx, empty body, or unparseable -> outcome unknown (money may have moved).
                    outcome.EnvelopeStatus = env != null ? env.Status : ("http_" + code);
                    return outcome; // Ambiguous (Submitted=false, DefinitiveReject=false)
                }
            }
            catch (Exception ex)
            {
                api.Response = ex.Message; api.ResponseDate = DateTime.Now;
                db.tblH_API_Log.Add(api); db.SaveChanges();
                outcome.EnvelopeStatus = "exception";
                outcome.Raw = ex.Message;
                return outcome; // Ambiguous
            }
        }
    }
}
