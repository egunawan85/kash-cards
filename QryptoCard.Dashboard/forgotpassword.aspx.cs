using QryptoCard.Dashboard.Models.Service;
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
    public partial class forgotpassword : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                logout();
            }
        }

        public void logout()
        {

            SessionLib.Current.SessionID = string.Empty;
            SessionLib.Current.UserID = string.Empty;
            SessionLib.Current.FirstName = string.Empty;
            SessionLib.Current.LastName = string.Empty;
            SessionLib.Current.Email = string.Empty;
            SessionLib.Current.DateJoin = null;
        }

        protected void btnfailed_ServerClick(object sender, EventArgs e)
        {
            divfailed.Visible = false;
        }

        protected void btnReset_ServerClick(object sender, EventArgs e)
        {
            divfailed.Visible = false;
            if (txtEmail.Value == "")
            {
                divfailed.Visible = true;
                lblFailed.Text = "Email and password cannot be empty";
                return;
            }
            try
            {
                UserService ad = new UserService();
                UserModel adm = new UserModel();
                adm.Email = txtEmail.Value.Trim();
                var admin = ad.forgotPassword(adm);
                if (admin.Status == "success")
                {
                    divfinish.Visible = true;
                    divforgot.Visible = false;
                    btnCancel.Visible = false;
                    btnReset.Visible = false;
                    btnLogin.Visible = true;
                }
                else
                {
                    divfailed.Visible = true;
                    lblFailed.Text = admin.Message;
                }
            }
            catch (Exception ex)
            {
                divfailed.Visible = true;
                lblFailed.Text = ex.Message;
            }
        }
    }
}