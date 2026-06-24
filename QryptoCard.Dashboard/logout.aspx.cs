using QryptoCard.Dashboard.Models;
using QryptoCard.Dashboard.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace QryptoCard.Dashboard
{
    public partial class logout : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            // Revoke server-side BEFORE clearing local session. Best-effort —
            // AuthClient.Revoke() never throws; on network failure it clears the
            // local tokens and proceeds.
            AuthClient.Revoke();

            SessionLib.Current.SessionID = string.Empty;
            SessionLib.Current.UserID = string.Empty;
            SessionLib.Current.FirstName = string.Empty;
            SessionLib.Current.LastName = string.Empty;
            SessionLib.Current.Email = string.Empty;
            SessionLib.Current.DateJoin = null;

            // Evict any legacy credential cookies planted before the Bearer
            // migration (the dashboard no longer writes or reads these).
            ExpireCookie("QryptoCardData");
            ExpireCookie("QryptoCardEmail");
            ExpireCookie("QryptoCardPassword");

            Response.Redirect("~/login");
        }

        private void ExpireCookie(string name)
        {
            Response.Cookies[name].Value = string.Empty;
            Response.Cookies[name].Expires = DateTime.Now.AddDays(-1);
        }
    }
}