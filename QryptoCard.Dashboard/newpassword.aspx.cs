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

namespace QryptoCard.Dashboard
{
    public partial class newpassword : System.Web.UI.Page
    {
        UserService us = new UserService();
        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                string id = Request.QueryString["id"];
                if (id != null)
                    check(id);
                else
                    Response.Redirect("~/404.html");
            }
        }

        public void check(string id)
        {

            id = Common.Base64Decode(id);
            if (id == "")
            {
                Response.Redirect("~/404.html");
                return;
            }
            UserForgotPasswordModel x = new UserForgotPasswordModel();
            x.Hash = id;
            var ck = us.checkForgotPassword(x);
            if (ck.Status == "success")
            {
                hfID.Value = id;
            }
            else
            {
                divpassword.Visible = false;
                divpasswordconfirm.Visible = false;
                btnSubmit.Visible = false;
                divfailed.Visible = true;
                lblFailed.Text = ck.Message;
                return;
            }
        }
        protected void btnfailed_ServerClick(object sender, EventArgs e)
        {
            divfailed.Visible = false;
        }

        protected void btnSubmit_ServerClick(object sender, EventArgs e)
        {
            if (txtPassword.Value == "")
            {
                divfailed.Visible = true;
                lblFailed.Text = "password cannot be empty";
                return;
            }
            if (txtPasswordConfirm.Value == "")
            {
                divfailed.Visible = true;
                lblFailed.Text = "Confirm password cannot be empty";
                return;
            }
            string pwMsg;
            if (!PasswordPolicy.Validate((txtPassword.Value ?? "").Trim(), out pwMsg))
            {
                divfailed.Visible = true;
                lblFailed.Text = pwMsg;
                return;
            }
            if (txtPassword.Value.Trim() != txtPasswordConfirm.Value.Trim())
            {
                divfailed.Visible = true;
                lblFailed.Text = "Your password is not match your confirm password";
                return;
            }

            UserForgotPasswordModel x = new UserForgotPasswordModel();
            x.Hash = hfID.Value;
            x.Param1 = txtPassword.Value.Trim();
            var ck = us.changeForgotPassword(x);
            if (ck.Status == "success")
            {
                divfinish.Visible = true;
                divpassword.Visible = false;
                divpasswordconfirm.Visible = false;
                btnSubmit.Visible = false;
                btnLogin.Visible = true;
            }
            else
            {
                divfailed.Visible = true;
                lblFailed.Text = ck.Message;
                return;
            }
        }
    }
}