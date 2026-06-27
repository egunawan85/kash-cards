using QryptoCard.Dashboard.Models;
using QryptoCard.Dashboard.Models.Service;
using QryptoCard.Dashboard.Services;
using QryptoCard.Sec;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Windows.Controls;

namespace QryptoCard.Dashboard
{
    public partial class login : System.Web.UI.Page
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
            // Reaching the login page (session timeout / direct nav) must revoke the outstanding
            // refresh chain server-side, not abandon it live. Mirrors logout.aspx; Revoke() also
            // clears the local token fields.
            AuthClient.Revoke();

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

        protected void btnLogin_ServerClick(object sender, EventArgs e)
        {
            divfailed.Visible = false;
            if (txtEmail.Value == "" || txtPassword.Value == "")
            {
                divfailed.Visible = true;
                lblFailed.Text = "Email and password cannot be empty";
                btnLogin.Enabled = true;
                return;
            }
            try
            {
                //bool checkLogin = Common.checkUserLogin(txtEmail.Text, txtPassword.Text);
                //if (checkLogin)
                //{
                //    checkRole();
                //}
                UserService ad = new UserService();
                UserModel adm = new UserModel();
                adm.Email = txtEmail.Value;
                // Login endpoint still expects the encrypted password; the OTP
                // step then mints tokens. No longer cache it in session — the
                // dashboard authenticates with Bearer tokens after mint.
                adm.Password = Secure.EncryptAPP((txtPassword.Value ?? "").Trim());
                var admin = ad.login(adm);
                if (admin.Status == "success")
                {
                    Session["OTPIDC"] = admin.Data.ToString();
                    Response.Redirect("otplogin");
                    //var dt = JsonConvert.DeserializeObject<UserModel>(admin.Data.ToString());
                    ////SessionLib.Current.SessionID = dt.SessionID.ToString();
                    //SessionLib.Current.user_id = dt.user_id.ToString();
                    //SessionLib.Current.first_name = dt.first_name.ToString();
                    //SessionLib.Current.last_name = dt.last_name.ToString();
                    //SessionLib.Current.user_name = dt.user_name.ToString();
                    //SessionLib.Current.user_phone = dt.user_phone.ToString();
                    //SessionLib.Current.user_email = dt.user_email.ToString();
                    //SessionLib.Current.role = "User";
                    //SessionLib.Current.created_at = dt.created_at;
                    //SessionLib.Current.user_password = Security.EncryptAPP(txtPassword.Text);
                    //checkRole();
                }
                else
                {
                    divfailed.Visible = true;
                    lblFailed.Text = admin.Message;
                }
                btnLogin.Enabled = true;
            }
            catch (Exception ex)
            {
                divfailed.Visible = true;
                lblFailed.Text = ex.Message;
                btnLogin.Enabled = true;
            }
        }
    }
}