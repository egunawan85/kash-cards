using System.Web.Http;

namespace QryptoCard.API
{
    // Base class for all Bearer-authenticated controllers in the user-facing API
    // tier. Hoists the getEmail() / getSubject() helpers that read the identity
    // BearerAuthenticationAttribute stashes onto Request.Properties on a
    // successful token verify.
    //
    // CRITICAL — email vs subject: the legacy WCF services are keyed on EMAIL, not
    // on the opaque subject id. Controllers pass the caller's email to the WCF
    // layer (e.g. getDashboardData(em) -> getUserId(em)). getEmail() therefore
    // returns qc_email (the verified subject's email), which is the value those
    // service calls expect. getSubject() returns qc_subject (the UserID/AdminID)
    // for the rare caller that needs the opaque id directly. Using getSubject()
    // where getEmail() is expected would break every WCF lookup.
    //
    // The helpers return null when the request stash is missing (e.g. a route that
    // bypasses [BearerAuthentication]). Callers that require an authenticated
    // identity are already inside a Bearer-protected route.
    //
    // Two separate base classes (one per project) because each project ships its
    // own BearerAuthenticationAttribute in its own namespace — the helper bodies
    // are identical but the type reference differs. Matches the existing
    // per-project duplication of BasicAuthenticationAttribute and ApiSecurity.
    public abstract class QryptoCardApiController : ApiController
    {
        protected string getEmail()
        {
            object email;
            return Request.Properties.TryGetValue(BearerAuthenticationAttribute.EmailPropertyKey, out email)
                ? email as string
                : null;
        }

        protected string getSubject()
        {
            object subject;
            return Request.Properties.TryGetValue(BearerAuthenticationAttribute.SubjectPropertyKey, out subject)
                ? subject as string
                : null;
        }
    }
}
