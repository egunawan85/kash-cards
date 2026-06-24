using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Web.Http.Controllers;
using System.Web.Http.Filters;

namespace QryptoCard.API
{
    // Sibling of BasicAuthenticationAttribute. Verifies Authorization: Bearer <token>
    // via AuthTokenSecurity (ChannelFactory wrapper around IAuthV1Service.verify);
    // returns 401 with a JSON body on any failure.
    //
    // Coexistence: this attribute exists alongside [BasicAuthentication]; existing
    // routes keep Basic auth. Decorating controllers with this attribute (and
    // retiring Basic) is a later step.
    //
    // SubjectType enforcement: the user (App) tier expects "user" tokens; an admin
    // token presented here should 401. ExpectedSubjectType is configurable per
    // attribute instance so the Admin tier mirrors this with ExpectedSubjectType
    // = "admin". Default is "user" (App-tier shape).
    //
    // Subject stash: on success the verified subject + subject-type (+ email) land
    // on ctx.Request.Properties under stable string keys so controllers can read
    // them without depending on this assembly's types.
    public class BearerAuthenticationAttribute : AuthorizationFilterAttribute
    {
        public const string SubjectPropertyKey     = "qc_subject";
        public const string SubjectTypePropertyKey = "qc_subject_type";
        public const string EmailPropertyKey       = "qc_email";

        // Property (not constructor arg) so the attribute can be applied with a
        // single name and configured via property syntax:
        //   [BearerAuthentication(ExpectedSubjectType = "admin")]
        public string ExpectedSubjectType { get; set; } = "user";

        public override void OnAuthorization(HttpActionContext actionContext)
        {
            var header = actionContext.Request.Headers.Authorization;
            if (header == null ||
                !string.Equals(header.Scheme, "Bearer", StringComparison.OrdinalIgnoreCase) ||
                string.IsNullOrWhiteSpace(header.Parameter))
            {
                Reject(actionContext);
                return;
            }

            var verify = AuthTokenSecurity.Verify(header.Parameter);
            if (verify == null || !verify.Valid)
            {
                Reject(actionContext);
                return;
            }

            // Cross-tier guard: an "admin" token presented to a user-tier controller
            // (or vice versa) must not authenticate. Without this, a leaked admin
            // token would grant user-tier access — small attack surface but easy to
            // close.
            if (!string.Equals(verify.SubjectType, ExpectedSubjectType, StringComparison.OrdinalIgnoreCase))
            {
                Reject(actionContext);
                return;
            }

            // A valid token with no subject must not authenticate — make the attribute the gate
            // rather than relying on a downstream controller to null-check qc_subject.
            if (string.IsNullOrEmpty(verify.Subject))
            {
                Reject(actionContext);
                return;
            }

            // A valid token whose email failed to resolve (transient DB error => null) must not
            // proceed with a null identity; fail closed rather than call WCF with a null email.
            if (string.IsNullOrEmpty(verify.Email))
            {
                Reject(actionContext);
                return;
            }

            // Stash for controllers. Keys are stable strings so controller-side
            // helpers don't depend on this assembly's types.
            actionContext.Request.Properties[SubjectPropertyKey]     = verify.Subject;
            actionContext.Request.Properties[SubjectTypePropertyKey] = verify.SubjectType;
            actionContext.Request.Properties[EmailPropertyKey]       = verify.Email;

            // Don't call base.OnAuthorization — the default would re-run principal
            // checks that we don't use here.
        }

        static void Reject(HttpActionContext actionContext)
        {
            var resp = actionContext.Request.CreateResponse(HttpStatusCode.Unauthorized);
            resp.Content = new StringContent(
                "{\"invalid_grant\" : \"Your credential is incorrect.\"}",
                Encoding.UTF8, "application/json");
            // WWW-Authenticate is the RFC 6750 §3 signal for Bearer realm.
            resp.Headers.WwwAuthenticate.Add(new AuthenticationHeaderValue("Bearer", "realm=\"qryptocard\""));
            actionContext.Response = resp;
        }
    }
}
