using QryptoCard.Dashboard.Admin.Models;
using QryptoCard.Dashboard.Admin.Models.Service;
using QryptoCard.Dashboard.Admin.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace QryptoCard.Dashboard.Admin
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
            SessionLib.Current.AdminID = string.Empty;
            SessionLib.Current.FirstName = string.Empty;
            SessionLib.Current.LastName = string.Empty;
            SessionLib.Current.UserName = string.Empty;
            SessionLib.Current.Email = string.Empty;
            SessionLib.Current.Phone = string.Empty;
            SessionLib.Current.Role = string.Empty;
            SessionLib.Current.DateJoin = null;
            SessionLib.Current.Password = string.Empty;
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
                AdminService ad = new AdminService();
                AdminModel adm = new AdminModel();
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