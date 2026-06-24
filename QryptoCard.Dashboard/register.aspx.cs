using QryptoCard.Dashboard.Models.Service;
using QryptoCard.Dashboard.Models;
using QryptoCard.Dashboard.Services;
using QryptoCard.Sec;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace QryptoCard.Dashboard
{
    public partial class register : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                logout();
                string id = Request.QueryString["id"];
                if (id == null || id == "")
                {
                    
                }
                else
                {
                    txtReferralCode.Value = id;
                    txtReferralCode.Disabled = true;
                }
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

        void enableButton()
        {
            btnRegisterX.Enabled = true;
        }

        protected void btnRegister_ServerClick(object sender, EventArgs e)
        {
            divfailed.Visible = false;
            //if (txtFirstName.Value == "")
            //{
            //    divfailed.Visible = true;
            //    enableButton();
            //    lblFailed.Text = "Your first name cannot be empty";
            //    return;
            //}
            //if (txtLastName.Value == "")
            //{
            //    divfailed.Visible = true;
            //    enableButton();
            //    lblFailed.Text = "Your last name cannot be empty";
            //    return;
            //}
            if (txtEmail.Value == "")
            {
                divfailed.Visible = true;
                enableButton();
                lblFailed.Text = "Your email cannot be empty";
                return;
            }
            if (txtPassword.Value == "")
            {
                divfailed.Visible = true;
                enableButton();
                lblFailed.Text = "password cannot be empty";
                return;
            }
            if (txtPasswordRepeat.Value == "")
            {
                divfailed.Visible = true;
                enableButton();
                lblFailed.Text = "Repeat password cannot be empty";
                return;
            }
            if (txtPassword.Value.Length < 8)
            {
                divfailed.Visible = true;
                enableButton();
                lblFailed.Text = "Your password should be 8 characters in minimum";
                return;
            }
            if (txtPassword.Value.Trim() != txtPasswordRepeat.Value.Trim())
            {
                divfailed.Visible = true;
                enableButton();
                lblFailed.Text = "Your password is not match your repeat password";
                return;
            }
            //if (!ckTerms.Checked)
            //{
            //    divfailed.Visible = true;
            //    lblFailed.Text = "Your need to accept the terms first";
            //    return;
            //}
            try
            {
                //bool checkLogin = Common.checkUserLogin(txtEmail.Text, txtPassword.Text);
                //if (checkLogin)
                //{
                //    checkRole();
                //}
                UserService ad = new UserService();
                UserModel adm = new UserModel();
                //adm.FirstName = txtFirstName.Value;
                //adm.LastName = txtLastName.Value;
                adm.Email = txtEmail.Value;
                adm.InvitedBy = txtReferralCode.Value;
                // Register endpoint still expects the encrypted password. No
                // longer cached in session — registration completes via the OTP
                // verify step; the user authenticates with Bearer tokens after a
                // subsequent login/mint.
                adm.Password = Secure.EncryptAPP(txtPassword.Value);
                var admin = ad.register(adm);
                if (admin.Status == "success")
                {
                    Session["OTPIDC"] = admin.Data.ToString();
                    Response.Redirect("otpregister");
                }
                else
                {
                    divfailed.Visible = true;
                    lblFailed.Text = admin.Message;
                }
                enableButton();
            }
            catch (Exception ex)
            {
                divfailed.Visible = true;
                lblFailed.Text = ex.Message;
                enableButton();
            }
        }
    }
}