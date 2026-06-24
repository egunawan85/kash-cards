using QryptoCard.Dashboard.Admin.Models;
using QryptoCard.Dashboard.Admin.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace QryptoCard.Dashboard.Admin
{
    public partial class logout : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            // Revoke the refresh-token chain server-side BEFORE clearing local
            // session. Best-effort — AuthClient.Revoke() never throws; on network
            // failure it clears local tokens and proceeds.
            AuthClient.Revoke();

            SessionLib.Current.SessionID = string.Empty;
            SessionLib.Current.AdminID = string.Empty;
            SessionLib.Current.FirstName = string.Empty;
            SessionLib.Current.LastName = string.Empty;
            SessionLib.Current.UserName = string.Empty;
            SessionLib.Current.Email = string.Empty;
            SessionLib.Current.Phone = string.Empty;
            SessionLib.Current.Role = string.Empty;
            SessionLib.Current.DateJoin = null;

            // Evict any already-planted legacy credential cookies (pre-migration
            // sessions wrote a 1-month QryptoCardAdmin* credential blob).
            expireLegacyCookie("QryptoCardAdminData");
            expireLegacyCookie("QryptoCardAdminEmail");
            expireLegacyCookie("QryptoCardAdminPassword");

            Response.Redirect("~/login");
        }

        void expireLegacyCookie(string name)
        {
            Response.Cookies[name].Value = "";
            Response.Cookies[name].Expires = DateTime.Now.AddDays(-1);
        }
    }
}