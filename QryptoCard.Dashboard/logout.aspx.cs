using QryptoCard.Dashboard.Models;
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
            SessionLib.Current.SessionID = string.Empty;
            SessionLib.Current.UserID = string.Empty;
            SessionLib.Current.FirstName = string.Empty;
            SessionLib.Current.LastName = string.Empty;
            SessionLib.Current.Email = string.Empty;
            SessionLib.Current.DateJoin = null;
            SessionLib.Current.Password = string.Empty;

            Response.Redirect("~/login");
        }
    }
}