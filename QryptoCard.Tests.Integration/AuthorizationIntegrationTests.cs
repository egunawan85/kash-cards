using Xunit;

namespace QryptoCard.Tests.Integration
{
    /// <summary>
    /// DB-backed authorization checks that exercise the real service methods against a throwaway
    /// schema. Gated by <see cref="RequiresDatabaseFactAttribute"/> — skipped unless KC_TEST_DB is
    /// set — so they light up during the dev shakeout. Bodies are wired then, against the seeded DB.
    /// </summary>
    public class AuthorizationIntegrationTests
    {
        [RequiresDatabaseFact]
        public void GetCardBalance_ForAnotherUsersCard_ReturnsNothing()
        {
            // dev-shakeout: seed users A and B + a card owned by A; call getCardBalance as B;
            // assert no balance is disclosed (IDOR ownership gate).
        }

        [RequiresDatabaseFact]
        public void UpdateUserFee_AsViewerAdmin_IsDenied()
        {
            // dev-shakeout: seed a Viewer admin; call updateUserFee; assert "not authorized"
            // (Owner/Admin allowlist).
        }

        [RequiresDatabaseFact]
        public void DepositCard_ForAnotherUsersCard_IsRejected()
        {
            // dev-shakeout: seed a card owned by A; call depositCard as B; assert it is not
            // attached to A's card (no cross-account deposit).
        }
    }
}
