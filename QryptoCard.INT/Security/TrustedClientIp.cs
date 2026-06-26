using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Web;

namespace QryptoCard.INT.Security
{
    // Web-API wrapper over QryptoCard.Sec.ClientIpResolver. Resolves the L4 peer and a header
    // accessor from the live HttpRequestMessage, then delegates the peer-gated trust ladder to
    // the (unit-tested, framework-free) resolver in Sec. Lives in INT because it needs
    // System.Web (HttpContextBase) + System.Net.Http and every API tier already references INT.
    //
    // Production IIS hosting populates MS_HttpContext from the System.Web integration so the L4
    // peer is reachable from the WebApi request. Returns the "unknown" sentinel rather than
    // throwing, so it is safe to call from a rate-limit code path.
    public static class TrustedClientIp
    {
        // The IIS WebApi host writes the per-request System.Web HttpContextBase into
        // request.Properties under this key; we read it back here to find the L4 peer.
        public const string MsHttpContextKey = "MS_HttpContext";

        public const string Unknown = QryptoCard.Sec.ClientIpResolver.Unknown;

        public static string Extract(HttpRequestMessage request)
        {
            if (request == null) return Unknown;
            try
            {
                var peer = ResolvePeerFromRequestMessage(request);
                return QryptoCard.Sec.ClientIpResolver.Resolve(name => ReadHeader(request, name), peer);
            }
            catch
            {
                return Unknown;
            }
        }

        private static string ReadHeader(HttpRequestMessage request, string name)
        {
            IEnumerable<string> values;
            if (!request.Headers.TryGetValues(name, out values)) return null;
            foreach (var v in values)
            {
                if (!string.IsNullOrWhiteSpace(v)) return v;
            }
            return null;
        }

        private static string ResolvePeerFromRequestMessage(HttpRequestMessage request)
        {
            object obj;
            if (request.Properties.TryGetValue(MsHttpContextKey, out obj))
            {
                var ctx = obj as HttpContextBase;
                if (ctx != null && ctx.Request != null)
                {
                    var addr = ctx.Request.UserHostAddress;
                    if (!string.IsNullOrEmpty(addr)) return addr;
                }
            }
            return null;
        }
    }
}
