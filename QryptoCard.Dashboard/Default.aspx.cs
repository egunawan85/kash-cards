using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace QryptoCard.Dashboard
{
    public partial class _Default : Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            if (Request.Cookies["QryptoCardEmail"] != null)
            {
                btnLogin.Visible = false;
                btnRegister.Visible = false;
                btnRegister2.Visible = false;
                txtGetStarted.Visible = false;
            }
            else
            {
                btnDashboard.Visible = false;
            }
        }
    }
}