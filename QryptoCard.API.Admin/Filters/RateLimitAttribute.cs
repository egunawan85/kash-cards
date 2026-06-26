using System;
using System.Net;
using System.Net.Http;
using System.Web.Http.Controllers;
using System.Web.Http.Filters;
using QryptoCard.INT.Model.Service;
using QryptoCard.INT.Security;

namespace QryptoCard.API.Admin.Filters
{
    // Per-IP rate-limit attribute for Web API actions (admin-tier mirror of the user-tier
    // QryptoCard.API.Filters.RateLimitAttribute). Applied at class or method scope; runs in
    // OnActionExecuting. Claims from QryptoCard.Sec.RateLimiter with a bucket key of
    // "ip:<client-ip>:<route>" so different endpoints don't share quota.
    //
    // Client IP comes from TrustedClientIp.Extract, which gates header-trust on the request's
    // source peer being the cloudflared loopback (CF-Connecting-IP -> True-Client-IP -> XFF
    // leftmost -> peer) and returns "unknown" when nothing is resolvable, so this filter never
    // throws from a rate-limit code path.
    //
    // On rejection: 429 with the existing OutputModel shape (Status="failed",
    // Message="Too many requests") plus a Retry-After hint — no new client parse path.
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
    public sealed class RateLimitAttribute : ActionFilterAttribute
    {
        public int Limit { get; set; }
        public int WindowSeconds { get; set; }

        public RateLimitAttribute(int limit, int windowSeconds)
        {
            if (limit < 1) throw new ArgumentOutOfRangeException("limit", "must be >= 1");
            if (windowSeconds < 1) throw new ArgumentOutOfRangeException("windowSeconds", "must be >= 1");
            Limit = limit;
            WindowSeconds = windowSeconds;
        }

        public override void OnActionExecuting(HttpActionContext actionContext)
        {
            string ip = ExtractClientIp(actionContext);
            string route = actionContext.ActionDescriptor.ControllerDescriptor.ControllerName
                + "." + actionContext.ActionDescriptor.ActionName;
            string key = "ip:" + ip + ":" + route;

            if (!QryptoCard.Sec.RateLimiter.TryClaim(key, Limit, WindowSeconds))
            {
                var op = new OutputModel
                {
                    Status = "failed",
                    Message = "Too many requests",
                    Data = null
                };
                actionContext.Response = actionContext.Request.CreateResponse(
                    (HttpStatusCode)429,
                    op);
                actionContext.Response.Headers.Add("Retry-After", WindowSeconds.ToString());
            }
        }

        internal static string ExtractClientIp(HttpActionContext actionContext)
        {
            if (actionContext == null) return TrustedClientIp.Unknown;
            return TrustedClientIp.Extract(actionContext.Request);
        }
    }
}
