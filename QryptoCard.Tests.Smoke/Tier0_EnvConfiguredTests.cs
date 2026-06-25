using Xunit;

namespace QryptoCard.Tests.Smoke
{
    // Tier 0 — configuration gate. When the smoke env isn't set the whole suite
    // early-returns (pass-skip), matching the sister [Fact]+early-return convention,
    // so `dotnet test` is green in CI without a deployed target.
    public class Tier0_EnvConfiguredTests
    {
        [Fact]
        public void SmokeEnv_is_present_or_suite_is_skipped()
        {
            if (!SmokeEnv.IsConfigured) return;
            Assert.False(string.IsNullOrWhiteSpace(SmokeEnv.BaseUrl));
            Assert.False(string.IsNullOrWhiteSpace(SmokeEnv.ApiKey));
            Assert.False(string.IsNullOrWhiteSpace(SmokeEnv.ApiSecret));
        }
    }
}
