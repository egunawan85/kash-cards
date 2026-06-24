using Newtonsoft.Json;
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

        private static string Header(string name)
        {
            HttpContext ctx = HttpContext.Current;
            return ctx != null ? ctx.Request.Headers[name] : null;
        }

        // WasabiCard signs the exact raw request body (SHA256withRSA), base64 in X-WSB-SIGNATURE,
        // verified with the platform public key. Verify BEFORE any parse or forward to the INT tier.
        [Route("wasabicard")]
        [HttpPost]
        public async Task<HttpResponseMessage> wasabi()
        {
            byte[] rawBody = await Request.Content.ReadAsByteArrayAsync() ?? new byte[0];

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

        // Runegate/PGCrypto signs "t=<unix>,v1=hex(HMAC-SHA256(secret,'<ts>.<rawBody>'))" in
        // X-Runegate-Signature. Verify over the raw bytes BEFORE parsing or crediting.
        [Route("pgcrypto")]
        [HttpPost]
        public async Task<HttpResponseMessage> pgcrypto()
        {
            byte[] rawBody = await Request.Content.ReadAsByteArrayAsync() ?? new byte[0];

            string signature = Header("X-Runegate-Signature");
            if (!RunegateWebhookVerifier.Verify(signature, rawBody, SecretsConfig.Require("PGCRYPTO_WEBHOOK_SECRET")))
                return Request.CreateResponse(HttpStatusCode.Unauthorized);

            PGCryptoModel model;
            try
            {
                model = JsonConvert.DeserializeObject<PGCryptoModel>(Encoding.UTF8.GetString(rawBody));
            }
            catch (JsonException)
            {
                return Request.CreateResponse(HttpStatusCode.BadRequest);
            }
            if (model == null)
                return Request.CreateResponse(HttpStatusCode.BadRequest);

            // Verified: forward to the INT tier. Any downstream error surfaces as 500 so the
            // provider retries (no silent exception swallowing).
            sr.PGCrypto(model);
            return Request.CreateResponse(HttpStatusCode.OK);
        }
    }
}
