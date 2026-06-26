using System.Net;

namespace QryptoCard.Sec
{
    // Single-source "is this request peer the cloudflared loopback?" predicate.
    //
    // The Cloudflare Tunnel topology terminates TLS at the edge and forwards to IIS over
    // plain HTTP loopback. Security primitives that read client-supplied forwarding headers
    // (the rate limiter's client-IP extraction) gate header trust on the request peer being
    // loopback, so a direct-ingress caller can't influence the rate-limit bucket key by
    // setting those headers itself. Lives in QryptoCard.Sec so every tier can share one
    // predicate without pulling in EF/the entity model.
    public static class LoopbackPredicate
    {
        public static bool IsLoopback(string peer)
        {
            // Production cloudflared typically binds 127.0.0.1; some configs bind ::1. Both are
            // loopback by IPAddress.IsLoopback.
            //
            // IPAddress.IsLoopback does NOT detect the IPv4-mapped IPv6 form
            // `::ffff:127.0.0.1` on .NET Framework 4.6.2 (only `::1` and `127.0.0.0/8`
            // short-form trigger true). Without this guard, a dual-stack-bound cloudflared
            // surfacing the peer as the mapped form would fall through to the direct-ingress
            // branch and forwarding headers would be ignored, breaking rate-limit fairness.
            // No spoof primitive opens (the gate stays closed under genuine direct-ingress),
            // but operational correctness matters. Map down to v4 and re-check.
            IPAddress ip;
            if (!IPAddress.TryParse(peer, out ip)) return false;
            if (IPAddress.IsLoopback(ip)) return true;
            if (ip.IsIPv4MappedToIPv6)
            {
                IPAddress v4 = ip.MapToIPv4();
                if (v4 != null && IPAddress.IsLoopback(v4)) return true;
            }
            return false;
        }
    }
}
