using System;
using System.Collections.Generic;
using QryptoCard.Sec;
using Xunit;

namespace QryptoCard.Tests.Unit
{
    // Peer-gated client-IP resolution core. getHeader is a simple dictionary lookup so the
    // cases are framework-free and deterministic.
    public class ClientIpResolverTests
    {
        private static Func<string, string> Headers(params string[] kv)
        {
            var d = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i + 1 < kv.Length; i += 2) d[kv[i]] = kv[i + 1];
            return name => { string v; return d.TryGetValue(name, out v) ? v : null; };
        }

        [Fact]
        public void LoopbackPeer_CfConnectingIp_BeatsSpoofedXff()
        {
            var get = Headers("CF-Connecting-IP", "1.2.3.4", "X-Forwarded-For", "9.9.9.9");
            Assert.Equal("1.2.3.4", ClientIpResolver.Resolve(get, "127.0.0.1"));
        }

        [Fact]
        public void NonLoopbackPeer_IgnoresForgedHeaders_ReturnsPeer()
        {
            var get = Headers("CF-Connecting-IP", "1.2.3.4", "X-Forwarded-For", "9.9.9.9");
            Assert.Equal("203.0.113.7", ClientIpResolver.Resolve(get, "203.0.113.7"));
        }

        [Fact]
        public void LoopbackPeer_TrueClientIp_BeatsXff()
        {
            var get = Headers("True-Client-IP", "5.5.5.5", "X-Forwarded-For", "9.9.9.9");
            Assert.Equal("5.5.5.5", ClientIpResolver.Resolve(get, "127.0.0.1"));
        }

        [Fact]
        public void LoopbackPeer_XffOnly_TakesLeftmost()
        {
            var get = Headers("X-Forwarded-For", "8.8.8.8, 10.0.0.1, 127.0.0.1");
            Assert.Equal("8.8.8.8", ClientIpResolver.Resolve(get, "127.0.0.1"));
        }

        [Fact]
        public void Ipv4MappedLoopbackPeer_OpensGate()
        {
            var get = Headers("CF-Connecting-IP", "1.2.3.4");
            Assert.Equal("1.2.3.4", ClientIpResolver.Resolve(get, "::ffff:127.0.0.1"));
        }

        [Fact]
        public void LoopbackPeer_NoHeaders_FallsBackToPeer()
        {
            Assert.Equal("127.0.0.1", ClientIpResolver.Resolve(Headers(), "127.0.0.1"));
        }

        [Fact]
        public void NoPeer_ReturnsUnknown()
        {
            Assert.Equal(ClientIpResolver.Unknown, ClientIpResolver.Resolve(Headers("CF-Connecting-IP", "1.2.3.4"), null));
            Assert.Equal(ClientIpResolver.Unknown, ClientIpResolver.Resolve(Headers(), ""));
        }
    }
}
