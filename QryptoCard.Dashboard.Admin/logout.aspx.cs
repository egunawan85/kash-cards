using QryptoCard.Dashboard.Admin.Models;
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
            SessionLib.Current.SessionID = string.Empty;
            SessionLib.Current.AdminID = string.Empty;
            SessionLib.Current.FirstName = string.Empty;
            SessionLib.Current.LastName = string.Empty;
            SessionLib.Current.UserName = string.Empty;
            SessionLib.Current.Email = string.Empty;
            SessionLib.Current.Phone = string.Empty;
            SessionLib.Current.Role = string.Empty;
            SessionLib.Current.DateJoin = null;
            SessionLib.Current.Password = string.Empty;

            Response.Redirect("~/login");
        }
    }
}