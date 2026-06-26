using System.Linq;
using System.Reflection;
using System.Web.Http;
using QryptoCard.API;
using QryptoCard.API.Controllers.v1;
using Xunit;

namespace QryptoCard.Tests.Integration
{
    // Contract guarantees for the new user-tier wallet READ endpoints exposed on UserV1Controller
    // (GET /v1/user/deposit/address and GET /v1/user/ledger).
    //
    // The actual deposit-address / ledger semantics — happy path, "only your own rows", and no
    // internal-id leakage — run end-to-end against a real LocalDB in UserWalletSurfaceTests against
    // the INT service the controller delegates to. The 401-on-missing/invalid/expired/revoked-token
    // behaviour of the [BearerAuthentication] gate is proven in BearerAuthAttributeIntegrationTests.
    // The one hop that cannot run in-process is the controller -> INT WCF call (see the note in
    // BearerAuthAttributeIntegrationTests), so this suite pins, by reflection, the two properties
    // that live in the controller's own surface and that the above suites then rely on:
    //
    //   * the endpoints REQUIRE authentication (the controller carries [BearerAuthentication], so
    //     an unauthenticated request is rejected with 401 by the proven attribute); and
    //   * they are IDOR-safe BY CONSTRUCTION — neither action accepts a caller-supplied user id /
    //     email / subject. Identity is taken only from the bearer token (getEmail()), so a caller
    //     can never address another user's wallet.
    //
    // It also confirms the generated WCF proxy was extended with the two new operations the
    // controller calls, so the delegation actually compiles against a real client method.
    public class UserV1WalletEndpointsTests
    {
        static MethodInfo Action(string name) =>
            typeof(UserV1Controller).GetMethod(name, BindingFlags.Public | BindingFlags.Instance);

        [Fact]
        public void Controller_RequiresBearerAuthentication_ForAllActions()
        {
            // Class-level [BearerAuthentication] => every action (incl. the two wallet reads) is
            // gated; an unauthenticated request gets a 401 from the attribute (proven separately).
            var attrs = typeof(UserV1Controller)
                .GetCustomAttributes(typeof(BearerAuthenticationAttribute), inherit: true);
            Assert.NotEmpty(attrs);

            var prefix = typeof(UserV1Controller)
                .GetCustomAttributes(typeof(RoutePrefixAttribute), inherit: true)
                .Cast<RoutePrefixAttribute>()
                .Single();
            Assert.Equal("v1/user", prefix.Prefix);
        }

        [Fact]
        public void GetDepositAddress_IsGetRouted_AndTakesNoCallerSuppliedIdentity()
        {
            var m = Action("getDepositAddress");
            Assert.NotNull(m);

            Assert.NotEmpty(m.GetCustomAttributes(typeof(HttpGetAttribute), inherit: true));
            var route = m.GetCustomAttributes(typeof(RouteAttribute), inherit: true)
                         .Cast<RouteAttribute>().Single();
            Assert.Equal("deposit/address", route.Template);

            // IDOR-by-construction: no parameters at all => the user id can only come from the token.
            Assert.Empty(m.GetParameters());
        }

        [Fact]
        public void GetLedger_IsGetRouted_AndOnlyTakesPagingNotIdentity()
        {
            var m = Action("getLedger");
            Assert.NotNull(m);

            Assert.NotEmpty(m.GetCustomAttributes(typeof(HttpGetAttribute), inherit: true));
            var route = m.GetCustomAttributes(typeof(RouteAttribute), inherit: true)
                         .Cast<RouteAttribute>().Single();
            Assert.Equal("ledger", route.Template);

            // IDOR-by-construction: only paging is caller-supplied; no id/email/subject parameter.
            var ps = m.GetParameters();
            Assert.Equal(2, ps.Length);
            Assert.All(ps, p => Assert.Equal(typeof(int), p.ParameterType));
            Assert.Contains(ps, p => p.Name == "page");
            Assert.Contains(ps, p => p.Name == "pageSize");
            Assert.DoesNotContain(ps, p =>
                p.Name.IndexOf("user", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                p.Name.IndexOf("email", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                p.Name.IndexOf("subject", System.StringComparison.OrdinalIgnoreCase) >= 0);
        }

        [Fact]
        public void GeneratedProxy_Exposes_TheTwoNewWalletReadOperations()
        {
            // The controller delegates to these via `new UserV1ServiceClient()`; if the generated
            // Reference.cs proxy were not extended, the controller would not compile. Pin them here
            // so a future proxy regeneration that drops them fails loudly with a clear reason.
            var client = typeof(QryptoCard.API.UserV1Service.UserV1ServiceClient);

            var addr = client.GetMethod("getDepositAddress", new[] { typeof(string) });
            Assert.NotNull(addr);

            var ledger = client.GetMethod("getLedger",
                new[] { typeof(string), typeof(int), typeof(int) });
            Assert.NotNull(ledger);
        }
    }
}
