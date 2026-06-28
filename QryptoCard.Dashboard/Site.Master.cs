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
            // Inject the app stylesheets into the <head> from code-behind. They can't be emitted with
            // an inline <%= Asset(...) %> in the .master markup: the <head runat="server"> renders a
            // code block inside a <link> attribute literally, which blanked the page styling.
            AddStylesheet("~/Content/css/premium.css");
            AddStylesheet("~/Content/css/app.css");

            if (SessionLib.Current.UserID != null)
            {
                lblFullname.Text = "Hi there!";
                lblEmail.Text = SessionLib.Current.Email;
            }
        }

        // Cache-busting URL for a static asset: appends ?v=<file last-write ticks> so a NEW url is
        // emitted whenever the file actually changes on deploy — busting browser AND CDN caches
        // automatically, with no hand-bumped version number to forget. Falls back to the plain url
        // if the file can't be stat'd.
        protected string Asset(string virtualPath)
        {
            string url = ResolveUrl(virtualPath);
            try
            {
                long ticks = System.IO.File.GetLastWriteTimeUtc(Server.MapPath(virtualPath)).Ticks;
                return url + "?v=" + ticks;
            }
            catch
            {
                return url;
            }
        }

        // Inject a cache-busted stylesheet <link> into the page <head> from code-behind — the head is
        // runat="server", where an inline <%= %> in a <link> attribute does not evaluate.
        void AddStylesheet(string virtualPath)
        {
            if (Page == null || Page.Header == null) return;
            var link = new System.Web.UI.HtmlControls.HtmlLink();
            link.Attributes["rel"] = "stylesheet";
            link.Href = Asset(virtualPath);
            Page.Header.Controls.Add(link);
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
