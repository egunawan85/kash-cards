using System.Net;
using System.Threading.Tasks;
using Xunit;

namespace QryptoCard.Tests.Smoke
{
    // Tier 2 — read coverage. Read-only; safe in ALL environments. Confirms the
    // public read surface answers for an authenticated caller (the seeded card type
    // makes the active-cards list resolvable on the dev box).
    public class Tier2_ReadSmokeTests
    {
        [Fact]
        public async Task CardActive_returns_200_with_body()
        {
            if (!SmokeEnv.IsConfigured) return;
            using (var http = SmokeHttpClient.Authenticated())
            {
                var resp = await http.GetAsync("v1/card/active");
                Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
                var body = await resp.Content.ReadAsStringAsync();
                Assert.False(string.IsNullOrWhiteSpace(body));
            }
        }
    }
}
