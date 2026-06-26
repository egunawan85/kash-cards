using System;
using QryptoCard.Sec;

namespace QryptoCard.INT.Security
{
    /// <summary>
    /// Fail-closed environment gate for the dev-only wallet test-credit tool.
    ///
    /// Crediting a wallet is minting money, so this gate is the load-bearing wall:
    /// the test-credit path runs ONLY when QRYPTO_ENVIRONMENT explicitly names a
    /// non-production environment. Everything else — production, an unset/blank
    /// variable, or any unrecognised value — is treated as production and refused.
    ///
    /// Two design choices make this fail closed rather than fail open:
    ///
    ///   1. It is an ALLOW-LIST, never a "!= prod" check. A negative check would
    ///      treat a typo ("prdo"), a renamed environment, or an unset variable as
    ///      non-prod and mint real money. The allow-list inverts the default: unknown
    ///      means deny.
    ///
    ///   2. It reads the RAW QRYPTO_ENVIRONMENT variable and does NOT go through
    ///      KeyModel.QRYPTO_ENVIRONMENT, which defaults to "dev" when unset. That
    ///      default is fine for picking a synthetic-deposit branch, but for a
    ///      money-minting capability it would be a fail-OPEN hole: a production box
    ///      that lost the variable would be mistaken for dev. Here, unset stays unset
    ///      (and is therefore denied).
    ///
    /// Server-IP whitelisting still independently keeps the dev box off real
    /// WasabiCard prod, but this gate is the in-process wall that makes prod
    /// money-minting impossible even for a full root-admin caller.
    /// </summary>
    public static class TestCreditGate
    {
        /// <summary>
        /// The only environments where dev-only money-minting tooling may run.
        /// Compared case- and whitespace-insensitively.
        /// </summary>
        public static readonly string[] AllowedEnvironments = { "dev", "sandbox" };

        /// <summary>
        /// True only when QRYPTO_ENVIRONMENT is explicitly one of
        /// <see cref="AllowedEnvironments"/>. Unset, blank, "prod", or any unknown
        /// value returns false (fail closed).
        /// </summary>
        public static bool IsAllowedEnvironment()
        {
            // GetOptional with a null fallback so an unset variable stays null (NOT
            // defaulted to "dev"). GetOptional reads the live process environment and
            // is uncached, so a misconfiguration that drops the variable is seen
            // immediately, and tests can flip it without a cache reset.
            return IsAllowed(SecretsConfig.GetOptional("QRYPTO_ENVIRONMENT", null));
        }

        /// <summary>
        /// Pure allow-list decision over an explicit environment value, split out so
        /// the fail-closed logic can be tested exhaustively without mutating the
        /// process environment. Null, blank, "prod", or any value not in
        /// <see cref="AllowedEnvironments"/> returns false.
        /// </summary>
        public static bool IsAllowed(string environment)
        {
            string env = (environment ?? "").Trim();
            if (env.Length == 0) return false;

            foreach (var allowed in AllowedEnvironments)
                if (env.Equals(allowed, StringComparison.OrdinalIgnoreCase))
                    return true;

            return false;
        }
    }
}
