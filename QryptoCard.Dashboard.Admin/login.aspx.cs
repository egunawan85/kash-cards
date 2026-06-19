using QryptoCard.Dashboard.Admin.Models.Service;
using QryptoCard.Dashboard.Admin.Models;
using QryptoCard.Dashboard.Admin.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using QryptoCard.Sec;

namespace QryptoCard.Dashboard.Admin
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

        protected void btnLogin_ServerClick(object sender, EventArgs e)
        {
            divfailed.Visible = false;
            if (txtEmail.Value == "" && txtPassword.Value == "")
            {
                divfailed.Visible = true;
                lblFailed.Text = "Email and password cannot be empty";
                return;
            }
            try
            {
                //bool checkLogin = Common.checkUserLogin(txtEmail.Text, txtPassword.Text);
                //if (checkLogin)
                //{
                //    checkRole();
                //}
                AdminService ad = new AdminService();
                AdminModel adm = new AdminModel();
                adm.Email = txtEmail.Value;
                adm.Password = Secure.EncryptAPP(txtPassword.Value);
                Session["QRTYHCA"] = adm.Password;
                var admin = ad.login(adm);
                if (admin.Status == "success")
                {
                    Session["OTPIDCA"] = admin.Data.ToString();
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
            }
            catch (Exception ex)
            {
                divfailed.Visible = true;
                lblFailed.Text = ex.Message;
            }
        }
    }
}