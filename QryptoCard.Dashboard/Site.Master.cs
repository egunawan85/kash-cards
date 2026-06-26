using QryptoCard.Dashboard.Models;
using System;
using System.Web;
using System.Web.UI;

namespace QryptoCard.Dashboard
{
    public partial class SiteMaster : MasterPage
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            if (SessionLib.Current.UserID != null)
            {
                lblFullname.Text = "Hi there!";
                lblEmail.Text = SessionLib.Current.Email;
            }
        }

        // Single-letter avatar glyph for the sidebar user block. Derived from the
        // signed-in email (we don't capture a display name); falls back to the brand
        // initial when no email is available.
        protected string AvatarInitial()
        {
            var email = SessionLib.Current.Email;
            if (!string.IsNullOrWhiteSpace(email))
                return email.Trim().Substring(0, 1).ToUpperInvariant();
            return "K";
        }

        // Returns "active" when the current page is one of the given .aspx files, so
        // the matching sidebar nav item is highlighted. Matches on the file name only
        // (exact) to avoid "cardlist.aspx" matching inside "mycardlist.aspx".
        protected string NavClass(params string[] pages)
        {
            var file = System.IO.Path.GetFileName(Page.AppRelativeVirtualPath ?? string.Empty);
            foreach (var p in pages)
                if (string.Equals(file, p, StringComparison.OrdinalIgnoreCase))
                    return "active";
            return string.Empty;
        }

        // Neutered under the Bearer migration: credential cookies are no longer
        // written or trusted, so there is no cookie-based session rehydrate. Always
        // returns false — callers fall through to forceLogin() when the in-memory
        // session is empty. Kept as a method so existing call sites still compile.
        public bool checkCookies()
        {
            return false;
        }

        public void forceLogin()
        {
            Response.Redirect("~/login");
        }
    }
}
