using System;
using Xunit;

namespace QryptoCard.Tests.Integration
{
    /// <summary>
    /// Marks an integration test that needs a live database. Skips (rather than fails) when
    /// KC_TEST_DB is not set, so the suite stays green in CI/dev without a DB. These run during
    /// the dev shakeout, with KC_TEST_DB pointing at a throwaway database.
    /// </summary>
    public sealed class RequiresDatabaseFactAttribute : FactAttribute
    {
        public RequiresDatabaseFactAttribute()
        {
            if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("KC_TEST_DB")))
                Skip = "Requires a database (set KC_TEST_DB); runs in the dev shakeout.";
        }
    }
}
