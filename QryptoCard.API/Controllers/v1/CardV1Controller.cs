using Newtonsoft.Json;
using QryptoCard.API.CardV1Service;
using QryptoCard.API.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Web;
using System.Web.Http;

namespace QryptoCard.API.Controllers.v1
{
    [RoutePrefix("v1/card")]
    [BearerAuthentication]
    public class CardV1Controller : QryptoCardApiController
    {
        CardV1ServiceClient sr = new CardV1ServiceClient();
        OutputModel op = new OutputModel();



        [Route("type")]
        [HttpGet]
        public OutputModel getTypes()
        {
            try
            {
                tblM_Card_Type x = new tblM_Card_Type();
                op = sr.CardType(x);
                if (op.Status == "success")
                    op.Data = JsonConvert.DeserializeObject<List<CardTypeModel>>(op.Data.ToString());
            }
            catch (Exception ex)
            {
                op.Status = "error";
                op.Message = ex.Message;
                op.Data = null;
            }

            return op;
        }


        [Route("type/id")]
        [HttpPost]
        public OutputModel getTypesById(tblM_Card_Type x)
        {
            try
            {
                op = sr.getCardTypeById(x);
                if (op.Status == "success")
                    op.Data = JsonConvert.DeserializeObject<CardTypeModel>(op.Data.ToString());
            }
            catch (Exception ex)
            {
                op.Status = "error";
                op.Message = ex.Message;
                op.Data = null;
            }

            return op;
        }


        [Route("holder/detail")]
        [HttpPost]
        public OutputModel getCardList(tblM_Cardholder x)
        {
            try
            {
                op = sr.getHolderDetail(getEmail(), x);
                if (op.Status == "success")
                    op.Data = JsonConvert.DeserializeObject<tblM_Cardholder>(op.Data.ToString());
            }
            catch (Exception ex)
            {
                op.Status = "error";
                op.Message = ex.Message;
                op.Data = null;
            }

            return op;
        }


        [Route("holder/check/cardtypeid")]
        [HttpPost]
        public OutputModel checkHolder(tblM_Cardholder x)
        {
            try
            {
                op = sr.checkHolderByCardTypeId(getEmail(), x);
                if (op.Status == "success")
                    op.Data = JsonConvert.DeserializeObject<tblM_Cardholder>(op.Data.ToString());
            }
            catch (Exception ex)
            {
                op.Status = "error";
                op.Message = ex.Message;
                op.Data = null;
            }

            return op;
        }


        [Route("open")]
        [HttpPost]
        public OutputModel openCard(tblT_Card x)
        {
            try
            {
                op = sr.openCard(getEmail(), x);
                if (op.Status == "success")
                    op.Data = JsonConvert.DeserializeObject<tblT_Card>(op.Data.ToString());
            }
            catch (Exception ex)
            {
                op.Status = "error";
                op.Message = ex.Message;
                op.Data = null;
            }

            return op;
        }


        [Route("list")]
        [HttpPost]
        public OutputModel getCardList(tblT_Card x)
        {
            try
            {
                op = sr.getCardList(getEmail(), x);
                if (op.Status == "success")
                    op.Data = JsonConvert.DeserializeObject<List<vw_Card>>(op.Data.ToString());
            }
            catch (Exception ex)
            {
                op.Status = "error";
                op.Message = ex.Message;
                op.Data = null;
            }

            return op;
        }

        [Route("list/all")]
        [HttpPost]
        public OutputModel getCardListAll(tblT_Card x)
        {
            try
            {
                op = sr.getCardListAll(getEmail(), x);
                if (op.Status == "success")
                    op.Data = JsonConvert.DeserializeObject<List<vw_Card>>(op.Data.ToString());
            }
            catch (Exception ex)
            {
                op.Status = "error";
                op.Message = ex.Message;
                op.Data = null;
            }

            return op;
        }


        [Route("detail/id")]
        [HttpPost]
        public OutputModel getCardByID(vw_Card x)
        {
            try
            {
                op = sr.getCardDetail(getEmail(), x);
                if (op.Status == "success")
                    op.Data = JsonConvert.DeserializeObject<vw_Card>(op.Data.ToString());
            }
            catch (Exception ex)
            {
                op.Status = "error";
                op.Message = ex.Message;
                op.Data = null;
            }

            return op;
        }

        [Route("trx/cancel")]
        [HttpPost]
        public OutputModel cancelCardTrx(vw_Card x)
        {
            try
            {
                op = sr.cancelCardTransaction(getEmail(), x);
                //if (op.Status == "success")
                //    op.Data = JsonConvert.DeserializeObject<vw_Card>(op.Data.ToString());
            }
            catch (Exception ex)
            {
                op.Status = "error";
                op.Message = ex.Message;
                op.Data = null;
            }

            return op;
        }

        [Route("deposit")]
        [HttpPost]
        public OutputModel depositCard(tblT_Card_Deposit x)
        {
            try
            {
                op = sr.depositCard(getEmail(), x);
                if (op.Status == "success")
                    op.Data = JsonConvert.DeserializeObject<tblT_Card_Deposit>(op.Data.ToString());
            }
            catch (Exception ex)
            {
                op.Status = "error";
                op.Message = ex.Message;
                op.Data = null;
            }

            return op;
        }

        [Route("deposit/detail")]
        [HttpPost]
        public OutputModel depositCardDetail(tblT_Card_Deposit x)
        {
            try
            {
                op = sr.getCardDepositDetail(getEmail(), x);
                if (op.Status == "success")
                    op.Data = JsonConvert.DeserializeObject<tblT_Card_Deposit>(op.Data.ToString());
            }
            catch (Exception ex)
            {
                op.Status = "error";
                op.Message = ex.Message;
                op.Data = null;
            }

            return op;
        }

        [Route("deposit/list")]
        [HttpPost]
        public OutputModel depositCardList(tblT_Card x)
        {
            try
            {
                op = sr.getCardDepositList(getEmail(), x);
                if (op.Status == "success")
                    op.Data = JsonConvert.DeserializeObject<List<tblT_Card_Deposit>>(op.Data.ToString());
            }
            catch (Exception ex)
            {
                op.Status = "error";
                op.Message = ex.Message;
                op.Data = null;
            }

            return op;
        }

        [Route("deposit/cancel")]
        [HttpPost]
        public OutputModel cancelCardDeposit(tblT_Card_Deposit x)
        {
            try
            {
                op = sr.cancelDepositTransaction(getEmail(), x);
                //if (op.Status == "success")
                //    op.Data = JsonConvert.DeserializeObject<vw_Card>(op.Data.ToString());
            }
            catch (Exception ex)
            {
                op.Status = "error";
                op.Message = ex.Message;
                op.Data = null;
            }

            return op;
        }

        [Route("trx/list")]
        [HttpPost]
        public OutputModel getCardTransaction(tblT_Card x)
        {
            try
            {
                op = sr.getCardTransaction(getEmail(), x);
                if (op.Status == "success")
                    op.Data = JsonConvert.DeserializeObject<List<tblT_Card_Transaction>>(op.Data.ToString());
            }
            catch (Exception ex)
            {
                op.Status = "error";
                op.Message = ex.Message;
                op.Data = null;
            }

            return op;
        }

        [Route("trx/detail")]
        [HttpPost]
        public OutputModel getCardTransactionDetail(tblT_Card_Transaction x)
        {
            try
            {
                op = sr.getCardTransactionDetail(getEmail(), x);
                if (op.Status == "success")
                    op.Data = JsonConvert.DeserializeObject<tblT_Card_Transaction>(op.Data.ToString());
            }
            catch (Exception ex)
            {
                op.Status = "error";
                op.Message = ex.Message;
                op.Data = null;
            }

            return op;
        }

        // ---- Deposit-into-card: funding-intent lifecycle (user-authed via BearerAuthentication;
        // em = the signed-in user). Gated by CardFundingStreamingEnabled inside the service. ----

        [Route("funding/intent")]
        [HttpPost]
        public OutputModel createFundingIntent(FundingIntentRequest req)
        {
            try { op = sr.createCardFundingIntent(getEmail(), req == null ? 0 : req.cardTypeId, req == null ? 0m : req.amount); }
            catch (Exception ex) { op.Status = "error"; op.Message = ex.Message; op.Data = null; }
            return op;
        }

        [Route("funding/topup")]
        [HttpPost]
        public OutputModel createFundingTopUp(FundingTopUpRequest req)
        {
            try { op = sr.createCardFundingTopUp(getEmail(), req == null ? null : req.cardNo, req == null ? 0m : req.amount); }
            catch (Exception ex) { op.Status = "error"; op.Message = ex.Message; op.Data = null; }
            return op;
        }

        [Route("funding/status")]
        [HttpPost]
        public OutputModel getFundingIntentStatus(FundingIntentRef req)
        {
            try { op = sr.getCardFundingIntentStatus(getEmail(), req == null ? null : req.intentId); }
            catch (Exception ex) { op.Status = "error"; op.Message = ex.Message; op.Data = null; }
            return op;
        }

        [Route("funding/cancel")]
        [HttpPost]
        public OutputModel cancelFundingIntent(FundingIntentRef req)
        {
            try { op = sr.cancelCardFundingIntent(getEmail(), req == null ? null : req.intentId); }
            catch (Exception ex) { op.Status = "error"; op.Message = ex.Message; op.Data = null; }
            return op;
        }

        // The signed-in user's OPEN (in-flight) funding intents — the card list "In progress" section.
        [Route("funding/list")]
        [HttpPost]
        public OutputModel listFundingIntents()
        {
            try { op = sr.getCardFundingOpenIntents(getEmail()); }
            catch (Exception ex) { op.Status = "error"; op.Message = ex.Message; op.Data = null; }
            return op;
        }
    }

    // Request bodies for the funding-intent routes (single-object binding from the JSON body).
    public class FundingIntentRequest { public long cardTypeId { get; set; } public decimal amount { get; set; } }
    public class FundingTopUpRequest { public string cardNo { get; set; } public decimal amount { get; set; } }
    public class FundingIntentRef { public string intentId { get; set; } }

    // Scheduled deposit-into-card ISSUANCE tick trigger. SEPARATE controller (NO BearerAuthentication)
    // so the on-box scheduler can call it; defended like the API.Callback scheduler routes: reject
    // anything proxied through Cloudflare, then a constant-time shared-secret check — both before any
    // work. QryptoCard.API is loopback-only (no public host), so in practice only the box reaches this.
    [RoutePrefix("v1/card")]
    public class CardFundingSchedulerV1Controller : ApiController
    {
        CardV1ServiceClient sr = new CardV1ServiceClient();

        private static string Header(string name)
        {
            var ctx = HttpContext.Current;
            return ctx != null ? ctx.Request.Headers[name] : null;
        }

        [Route("funding/issue")]
        [HttpPost]
        public HttpResponseMessage fundingIssue()
        {
            if (!string.IsNullOrEmpty(Header("CF-Connecting-IP")) || !string.IsNullOrEmpty(Header("CF-Ray")))
                return Request.CreateResponse(HttpStatusCode.NotFound);
            if (!QryptoCard.Sec.SharedSecretAuth.IsAuthorized(Header("X-Scheduler-Auth"), "SCHEDULER_SHARED_SECRET"))
                return Request.CreateResponse(HttpStatusCode.Unauthorized);

            string summary = sr.RunCardFundingIssuance();
            HttpResponseMessage response = Request.CreateResponse(HttpStatusCode.OK);
            response.Content = new StringContent(summary ?? "{}", Encoding.UTF8, "application/json");
            return response;
        }
    }
}
