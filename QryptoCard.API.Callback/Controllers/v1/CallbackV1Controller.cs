using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using QryptoCard.API.Callback.CallbackV1Service;
using QryptoCard.API.Callback.Models;
using QryptoCard.Sec;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;

namespace QryptoCard.API.Callback.Controllers.v1
{
    [RoutePrefix("v1/payment")]
    public class CallbackV1Controller : ApiController
    {
        CallbackV1ServiceClient sr = new CallbackV1ServiceClient();

        public CallbackV1Controller()
        {
            // Authenticate calls to the INT money tier with the shared secret.
            sr.Endpoint.EndpointBehaviors.Add(new QryptoCard.API.Callback.Security.IntAuthClientBehavior());
        }

        private static string Header(string name)
        {
            HttpContext ctx = HttpContext.Current;
            return ctx != null ? ctx.Request.Headers[name] : null;
        }

        // Scheduled reconciliation-sweep trigger. The API.Callback host is publicly reachable via the
        // Cloudflare tunnel, so this money-moving endpoint is defended two ways, both BEFORE any work:
        //   1. Reject anything that arrived THROUGH Cloudflare. The tunnel stamps CF-* headers on every
        //      proxied request; the on-box scheduled task hits 127.0.0.1:8084 directly and has none.
        //      So in practice only the box can reach this. 404 (not 401) so an external prober can't
        //      even tell the endpoint exists.
        //   2. A shared secret in X-Scheduler-Auth (fail-closed, constant-time).
        [Route("reconcile/pending")]
        [HttpPost]
        public HttpResponseMessage reconcilePending()
        {
            // Arrived through the public Cloudflare tunnel -> not the on-box scheduler -> pretend absent.
            if (!string.IsNullOrEmpty(Header("CF-Connecting-IP")) || !string.IsNullOrEmpty(Header("CF-Ray")))
                return Request.CreateResponse(HttpStatusCode.NotFound);

            if (!SharedSecretAuth.IsAuthorized(Header("X-Scheduler-Auth"), "SCHEDULER_SHARED_SECRET"))
                return Request.CreateResponse(HttpStatusCode.Unauthorized);

            int resolved = sr.ReconcilePendingProvider();
            HttpResponseMessage response = Request.CreateResponse(HttpStatusCode.OK);
            response.Content = new StringContent("{\"resolved\":" + resolved + "}", Encoding.UTF8, "application/json");
            return response;
        }

        // WasabiCard signs the exact raw request body (SHA256withRSA), base64 in X-WSB-SIGNATURE,
        // verified with the platform public key. Verify BEFORE any parse or forward to the INT tier.
        [Route("wasabicard")]
        [HttpPost]
        public async Task<HttpResponseMessage> wasabi()
        {
            byte[] rawBody = await Request.Content.ReadAsByteArrayAsync() ?? new byte[0];
            if (rawBody.Length == 0) return Request.CreateResponse(HttpStatusCode.Unauthorized);

            string signature = Header("X-WSB-SIGNATURE");
            if (!WasabiSignatureVerifier.Verify(signature, rawBody, SecretsConfig.Require("WASABICARD_WSBPUBLIC_KEY")))
                return Request.CreateResponse(HttpStatusCode.Unauthorized);

            // Verified: forward the EXACT bytes received (not a re-serialized copy) to the INT tier.
            sr.Wasabi(Header("X-WSB-CATEGORY"), signature, Header("X-WSB-REQUEST-ID"),
                      Encoding.UTF8.GetString(rawBody));

            WasabiResponseModel responseModel = new WasabiResponseModel { success = true, msg = "success", code = 200 };
            HttpResponseMessage response = Request.CreateResponse(HttpStatusCode.OK);
            response.Content = new StringContent(JsonConvert.SerializeObject(responseModel), Encoding.UTF8, "application/json");
            return response;
        }

        // Runegate signs "t=<unix>,v1=hex(HMAC-SHA256(secret,'<ts>.<rawBody>'))" in
        // X-Runegate-Signature. Verify over the raw bytes BEFORE parsing or crediting.
        // Public path is /v1/payment/runegate (the provider name); the internal model,
        // WCF forward (sr.PGCrypto), secret name, and dedup Type stay "PGCrypto" — legacy
        // internal naming that does not affect this URL.
        [Route("runegate")]
        [HttpPost]
        public async Task<HttpResponseMessage> runegate()
        {
            byte[] rawBody = await Request.Content.ReadAsByteArrayAsync() ?? new byte[0];
            if (rawBody.Length == 0) return Request.CreateResponse(HttpStatusCode.Unauthorized);

            string signature = Header("X-Runegate-Signature");
            if (!RunegateWebhookVerifier.Verify(signature, rawBody, SecretsConfig.Require("PGCRYPTO_WEBHOOK_SECRET")))
                return Request.CreateResponse(HttpStatusCode.Unauthorized);

            PGCryptoModel model;
            string json = Encoding.UTF8.GetString(rawBody);
            try
            {
                model = JsonConvert.DeserializeObject<PGCryptoModel>(json);
                if (model != null)
                {
                    // Runegate's webhook spells the gateway-fee fields "Commission" /
                    // "CommissionInPercentage" (double-'s'); our model -- like kash's DB columns
                    // and the referral ledger -- uses the single-'s' "Commision", so Newtonsoft
                    // won't bind them by name. Map them explicitly at this adapter boundary so the
                    // fee reaches the ledger. The CREDITED amount is Total (net), which binds
                    // directly and is unaffected; this only restores the informational fee detail.
                    JObject jo = JObject.Parse(json);
                    model.Commision = (decimal?)jo["Commission"];
                    model.CommisionInPercentage = (double?)jo["CommissionInPercentage"];
                }
            }
            catch (JsonException)
            {
                return Request.CreateResponse(HttpStatusCode.BadRequest);
            }
            if (model == null)
                return Request.CreateResponse(HttpStatusCode.BadRequest);

            // Verified: forward to the INT tier. (Downstream error propagation and replay
            // idempotency are tracked as coupled follow-up hardening of the INT callback tier.)
            sr.PGCrypto(model);
            return Request.CreateResponse(HttpStatusCode.OK);
        }
    }
}
