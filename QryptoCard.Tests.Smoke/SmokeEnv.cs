using System;

namespace QryptoCard.Tests.Smoke
{
    // Reads the smoke configuration from the environment. When the suite is not
    // configured (no base URL / credentials), tests early-return as pass-skip so
    // the project is harmless in CI without a deployed target.
    internal static class SmokeEnv
    {
        public static string BaseUrl       => Get("SMOKE_BASE_URL");
        public static string ApiKey        => Get("SMOKE_API_KEY");
        public static string ApiSecret     => Get("SMOKE_API_SECRET");
        public static string AdminEmail    => Get("SMOKE_ADMIN_EMAIL");
        public static string AdminPassword => Get("SMOKE_ADMIN_PASSWORD");

        // Mutating (money-path) tiers run only when explicitly enabled — dev/sandbox
        // only. PROD must never set this; it uses a separate bounded canary pilot.
        public static bool AllowMutation =>
            string.Equals(Get("SMOKE_ALLOW_MUTATION"), "true", StringComparison.OrdinalIgnoreCase);

        public static bool IsConfigured =>
            !string.IsNullOrWhiteSpace(BaseUrl)
            && !string.IsNullOrWhiteSpace(ApiKey)
            && !string.IsNullOrWhiteSpace(ApiSecret);

        private static string Get(string name) => Environment.GetEnvironmentVariable(name);
    }
}
