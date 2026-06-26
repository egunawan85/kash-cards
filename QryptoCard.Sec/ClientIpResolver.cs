using System;

namespace QryptoCard.Sec
{
    // Single-source "what is the real client IP?" resolution core (pure, web-framework-free
    // so it is directly unit-testable).
    //
    // Threat model: under Cloudflare Tunnel, cloudflared connects to IIS over loopback and
    // the authoritative client IP is in `CF-Connecting-IP`. Cloudflare APPENDS to
    // `X-Forwarded-For` — it does NOT strip — so a left-most-XFF parse alone is bypassable by
    // any client that prepends a spoofed leading entry. Header trust is therefore gated on the
    // request's source peer being the cloudflared loopback. Direct-ingress callers (no proxy
    // in path) get the literal peer IP and cannot influence the result via headers.
    //
    // Precedence inside the trusted (loopback) path:
    //   1. CF-Connecting-IP (Cloudflare-injected, single value, cannot be forged through edge).
    //   2. True-Client-IP   (Cloudflare-Enterprise / Akamai variants).
    //   3. X-Forwarded-For left-most (preserves dev-mode and any future direct-proxy topology).
    //   4. The peer itself (when nothing usable is in any header).
    //
    // The thin per-framework wrapper (QryptoCard.INT.Security.TrustedClientIp) resolves the
    // peer + a header accessor from the live request and calls Resolve here.
    public static class ClientIpResolver
    {
        // Cloudflare-authoritative client IP. Cloudflare overwrites this on every forwarded
        // request — clients cannot forge it through the edge.
        public const string CloudflareClientIpHeader = "CF-Connecting-IP";

        // Fallback for Cloudflare Enterprise (when configured) and Akamai-style providers.
        public const string TrueClientIpHeader = "True-Client-IP";

        // Legacy (pre-Cloudflare) header. Trusted only when the source peer is loopback.
        public const string XForwardedForHeader = "X-Forwarded-For";

        // Sentinel returned when nothing usable can be resolved. Callers (rate-limit bucket
        // key) receive a stable string instead of null — the bucket shards everyone with no
        // resolvable IP into one shared "unknown" group, conservative but never silently
        // spoofable.
        public const string Unknown = "unknown";

        // The peer-gated trust ladder. getHeader returns null when the header is absent.
        public static string Resolve(Func<string, string> getHeader, string peer)
        {
            if (getHeader == null) return Unknown;

            // No peer = no trust. Synthetic / test contexts that don't surface a peer fall
            // through to the "unknown" sentinel rather than getting silent header-trust.
            if (string.IsNullOrEmpty(peer)) return Unknown;

            // Direct-ingress: the request came straight to the origin (or through an unknown
            // proxy). Headers are unverifiable, so the peer IP is the only trustworthy signal.
            if (!LoopbackPredicate.IsLoopback(peer)) return peer;

            // Loopback peer => cloudflared / known reverse proxy. Walk the precedence ladder.
            var cf = getHeader(CloudflareClientIpHeader);
            var cfTrim = cf == null ? null : cf.Trim();
            if (!string.IsNullOrEmpty(cfTrim)) return cfTrim;

            var tcip = getHeader(TrueClientIpHeader);
            var tcipTrim = tcip == null ? null : tcip.Trim();
            if (!string.IsNullOrEmpty(tcipTrim)) return tcipTrim;

            var xffLeftmost = ExtractXffLeftmost(getHeader(XForwardedForHeader));
            if (!string.IsNullOrEmpty(xffLeftmost)) return xffLeftmost;

            // No header surfaced a usable IP. Fall back to peer (loopback) — every same-host
            // caller shares one bucket, conservative but never silently spoofable.
            return peer;
        }

        private static string ExtractXffLeftmost(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return null;
            int comma = raw.IndexOf(',');
            string first = comma < 0 ? raw : raw.Substring(0, comma);
            first = first.Trim();
            return string.IsNullOrEmpty(first) ? null : first;
        }
    }
}
