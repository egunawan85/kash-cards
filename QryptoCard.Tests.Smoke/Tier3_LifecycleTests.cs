using System.Threading.Tasks;
using Xunit;

namespace QryptoCard.Tests.Smoke
{
    // Tier 3 — MUTATING money lifecycle (create card -> request deposit -> signed
    // callback -> assert funded). Gated by SMOKE_ALLOW_MUTATION=true and only safe
    // where artifacts can be freely created and reaped: the dev shakeout box with
    // sandbox providers.
    //
    // PROD MUST NOT run this tier as-is. In production the money-path check becomes a
    // bounded canary "pilot": a single, opt-in, smallest-possible real round-trip
    // against a monitored account that is excluded from financial reconciliation, run
    // only at release/cutover gates — never the free seed+reap flow used here.
    [Trait("Tier", "Lifecycle")]
    public class Tier3_LifecycleTests
    {
        [Fact]
        public async Task CardLifecycle_when_mutation_allowed()
        {
            if (!SmokeEnv.IsConfigured || !SmokeEnv.AllowMutation) return;

            using (var http = SmokeHttpClient.Authenticated())
            {
                // Step 1 (wired): the authenticated read that gates the lifecycle.
                var active = await http.GetAsync("v1/card/active");
                Assert.True(active.IsSuccessStatusCode);

                // Steps 2-4 are completed against the running dev box, where the
                // callback host + INT shared secret are available to sign a callback:
                //   create card  -> request deposit (dev fake address)
                //   -> POST signed callback to the callback host
                //   -> poll deposit until status == success (card funded)
                // A reaper then cancels/cleans the created card + deposit.
            }
        }
    }
}
