using System.Net;
using System.Threading.Tasks;
using Xunit;

namespace QryptoCard.Tests.Smoke
{
    // Tier 1 — authentication. Read-only / negative; safe in ALL environments
    // (dev and prod), so these are the always-on prod smoke checks too.
    public class Tier1_AuthTests
    {
        [Fact]
        public async Task NoCredentials_returns_401()
        {
            if (!SmokeEnv.IsConfigured) return;
            using (var http = SmokeHttpClient.Anonymous())
            {
                var resp = await http.GetAsync("v1/card/active");
                Assert.Equal(HttpStatusCode.Unauthorized, resp.StatusCode);
            }
        }

        [Fact]
        public async Task ValidApiKey_is_not_rejected()
        {
            if (!SmokeEnv.IsConfigured) return;
            using (var http = SmokeHttpClient.Authenticated())
            {
                var resp = await http.GetAsync("v1/card/active");
                Assert.NotEqual(HttpStatusCode.Unauthorized, resp.StatusCode);
            }
        }
    }
}
