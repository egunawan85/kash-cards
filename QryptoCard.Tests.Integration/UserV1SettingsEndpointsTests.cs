using System.Linq;
using System.Reflection;
using System.Web.Http;
using QryptoCard.API;
using QryptoCard.API.Controllers.v1;
using Xunit;

namespace QryptoCard.Tests.Integration
{
    // Contract guarantees for the user-tier account-settings endpoints exposed on UserV1Controller
    // (PUT /v1/user/data, PUT /v1/user/password, POST /v1/user/email/otp, PUT /v1/user/email).
    //
    // These delegate to INT methods (updateUserData / updatePassword / updateEmailOTP / updateEmail)
    // that key every change on the bearer identity (getEmail()) and never on a body-supplied id, so a
    // caller can only ever change their OWN profile/password/email. The 401-on-no/invalid-token
    // behaviour of the [BearerAuthentication] gate is proven in BearerAuthAttributeIntegrationTests;
    // the controller -> INT WCF hop can't run in-process (see the note there). This suite pins, by
    // reflection, the surface the settings page relies on: each action requires auth and is mapped to
    // the expected route + HTTP verb. The credential change being scoped to the caller lives in the
    // INT tier and is asserted there.
    public class UserV1SettingsEndpointsTests
    {
        static MethodInfo Action(string name) =>
            typeof(UserV1Controller).GetMethod(name, BindingFlags.Public | BindingFlags.Instance);

        [Fact]
        public void SettingsEndpoints_AreBearerGated()
        {
            // The whole controller is [BearerAuthentication] — the credential-change endpoints are
            // gated like everything else; an unauthenticated request is rejected with 401.
            Assert.NotEmpty(typeof(UserV1Controller)
                .GetCustomAttributes(typeof(BearerAuthenticationAttribute), inherit: true));
        }

        [Theory]
        [InlineData("updateUserData", "data", typeof(System.Web.Http.HttpPutAttribute))]
        [InlineData("updatePassword", "password", typeof(System.Web.Http.HttpPutAttribute))]
        [InlineData("updateEmailOTP", "email/otp", typeof(System.Web.Http.HttpPostAttribute))]
        [InlineData("updateEmail", "email", typeof(System.Web.Http.HttpPutAttribute))]
        public void SettingsEndpoint_IsMappedToExpectedRouteAndVerb(string method, string route, System.Type verbAttr)
        {
            var m = Action(method);
            Assert.NotNull(m);

            var routeAttr = m.GetCustomAttributes(typeof(RouteAttribute), inherit: true)
                             .Cast<RouteAttribute>().Single();
            Assert.Equal(route, routeAttr.Template);

            Assert.NotEmpty(m.GetCustomAttributes(verbAttr, inherit: true));
        }
    }
}
