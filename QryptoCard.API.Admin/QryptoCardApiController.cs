using System.Web.Http;

namespace QryptoCard.API.Admin
{
    // Admin-tier mirror of QryptoCard.API.QryptoCardApiController. See there for
    // the full rationale, including the email-vs-subject contract: getEmail()
    // returns qc_email (the value the legacy WCF services are keyed on), and
    // getSubject() returns qc_subject (the opaque AdminID).
    //
    // Two separate base classes (one per project) because each project ships its
    // own BearerAuthenticationAttribute in its own namespace — the helper bodies
    // are identical but the type reference differs.
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
