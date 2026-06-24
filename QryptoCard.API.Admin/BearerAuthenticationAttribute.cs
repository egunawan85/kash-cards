using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Web.Http.Controllers;
using System.Web.Http.Filters;

namespace QryptoCard.API.Admin
{
    // Admin-tier mirror of QryptoCard.API.BearerAuthenticationAttribute.
    // Default ExpectedSubjectType is "admin" — the user tier's default is "user".
    // The cross-tier guard ensures a user token cannot authenticate to Admin-tier
    // controllers and vice-versa.
    //
    // See ../QryptoCard.API/BearerAuthenticationAttribute.cs for full design
    // rationale.
    public class BearerAuthenticationAttribute : AuthorizationFilterAttribute
    {
        public const string SubjectPropertyKey     = "qc_subject";
        public const string SubjectTypePropertyKey = "qc_subject_type";
        public const string EmailPropertyKey       = "qc_email";

        public string ExpectedSubjectType { get; set; } = "admin";

        public override void OnAuthorization(HttpActionContext actionContext)
        {
            var header = actionContext.Request.Headers.Authorization;
            if (header == null ||
                !string.Equals(header.Scheme, "Bearer", StringComparison.OrdinalIgnoreCase) ||
                string.IsNullOrEmpty(header.Parameter))
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

            if (!string.Equals(verify.SubjectType, ExpectedSubjectType, StringComparison.OrdinalIgnoreCase))
            {
                Reject(actionContext);
                return;
            }

            // A valid token with no subject must not authenticate — make the attribute the gate.
            if (string.IsNullOrEmpty(verify.Subject))
            {
                Reject(actionContext);
                return;
            }

            actionContext.Request.Properties[SubjectPropertyKey]     = verify.Subject;
            actionContext.Request.Properties[SubjectTypePropertyKey] = verify.SubjectType;
            actionContext.Request.Properties[EmailPropertyKey]       = verify.Email;
        }

        static void Reject(HttpActionContext actionContext)
        {
            var resp = actionContext.Request.CreateResponse(HttpStatusCode.Unauthorized);
            resp.Content = new StringContent(
                "{\"invalid_grant\" : \"Your credential is incorrect.\"}",
                Encoding.UTF8, "application/json");
            resp.Headers.WwwAuthenticate.Add(new AuthenticationHeaderValue("Bearer", "realm=\"qryptocard\""));
            actionContext.Response = resp;
        }
    }
}
