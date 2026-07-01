using Newtonsoft.Json;
using QryptoCard.Dashboard.Models;
using QryptoCard.Dashboard.Models.Service;
using QryptoCard.Dashboard.Services;
using System;
using System.Web.UI;

namespace QryptoCard.Dashboard.card
{
    // JSON polling endpoint for the deposit-into-card tracker. The funding screen's client-side
    // poller (backoff 5s->15s->30s->60s) fetches this to advance the tracker without a full postback.
    //
    // Same-origin + session-authed: the browser can't call the upstream API directly (the bearer lives
    // in server session, not JS), so this thin proxy runs the call under the signed-in session. The
    // upstream getCardFundingIntentStatus is itself STRICTLY user-scoped (it resolves em -> uid from the
    // bearer and filters WHERE UserID = @u), so a user can only ever read their OWN intent — passing
    // someone else's intentId just returns "not found". No user id is ever taken from the query string.
    public partial class fundingstatus : System.Web.UI.Page
    {
        CardService cs = new CardService();

        protected void Page_Load(object sender, EventArgs e)
        {
            Response.ContentType = "application/json";
            Response.Cache.SetCacheability(System.Web.HttpCacheability.NoCache);

            // Auth guard: an unauthenticated poll gets a clean 401 JSON, never intent data. The tracker
            // page holds a live session while it polls; a dropped session fails closed here.
            if (!Common.checkID())
            {
                if (!Master_TryCookieAuth())
                {
                    Response.StatusCode = 401;
                    Write(new { status = "error", message = "auth", data = (object)null });
                    return;
                }
            }

            string intentId = Request.QueryString["intent"];
            if (string.IsNullOrEmpty(intentId))
            {
                Write(new { status = "error", message = "missing intent", data = (object)null });
                return;
            }

            OutputModel op = cs.getFundingIntentStatus(intentId);

            object data = null;
            if (op != null && op.Data != null)
            {
                try { data = JsonConvert.DeserializeObject(op.Data.ToString()); }
                catch { data = null; }
            }
            Write(new { status = op != null ? op.Status : "error", message = op != null ? op.Message : "no response", data = data });
        }

        // Cookie-based session rehydrate (mirrors the master's checkCookies) without needing the master
        // page here; best-effort, returns false if it can't establish a session.
        private bool Master_TryCookieAuth()
        {
            try { return SessionLib.Current != null && !string.IsNullOrEmpty(SessionLib.Current.UserID); }
            catch { return false; }
        }

        private void Write(object payload)
        {
            Response.Write(JsonConvert.SerializeObject(payload));
        }
    }
}
