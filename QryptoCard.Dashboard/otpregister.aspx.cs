using Newtonsoft.Json;
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
    public partial class otpregister : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                if (Session["OTPIDC"] == null)
                    Response.Redirect("Register");
            }
        }

        void enableButton()
        {
            btnAuthX.Enabled = true;
        }

        protected void btnAuth_ServerClick(object sender, EventArgs e)
        {
            if (icode1.Value == "")
            {
                enableButton();
                lblFailed.Text = "All textbox should be filled";
                divfailed.Visible = true;
                return;
            }
            if (icode2.Value == "")
            {
                enableButton();
                lblFailed.Text = "All textbox should be filled";
                divfailed.Visible = true;
                return;
            }
            if (icode3.Value == "")
            {
                enableButton();
                lblFailed.Text = "All textbox should be filled";
                divfailed.Visible = true;
                return;
            }
            if (icode4.Value == "")
            {
                enableButton();
                lblFailed.Text = "All textbox should be filled";
                divfailed.Visible = true;
                return;
            }
            if (icode5.Value == "")
            {
                enableButton();
                lblFailed.Text = "All textbox should be filled";
                divfailed.Visible = true;
                return;
            }
            if (icode6.Value == "")
            {
                enableButton();
                lblFailed.Text = "All textboc should be filled";
                divfailed.Visible = true;
                return;
            }

            UserService ad = new UserService();
            UserAuthOTPModel z = new UserAuthOTPModel();
            z.ID = Session["OTPIDC"].ToString();
            z.Code = icode1.Value + icode2.Value + icode3.Value + icode4.Value + icode5.Value + icode6.Value;

            var admin = ad.registerVerify(z);
            if (admin.Status == "success")
            {
                enableButton();
                var dt = JsonConvert.DeserializeObject<UserModel>(admin.Data.ToString());
                SessionLib.Current.SessionID = dt.UserID;
                SessionLib.Current.UserID = dt.UserID;
                SessionLib.Current.FirstName = dt.FirstName;
                SessionLib.Current.LastName = dt.LastName;
                SessionLib.Current.Email = dt.Email;
                SessionLib.Current.DateJoin = dt.DateJoin;
                // Register/verify (legacy, stage-1) does not mint tokens. The user
                // lands without a Bearer pair; the dashboard's auth gate routes them
                // to login, where mint-after-otp issues the token pair.
                Session["QRTYHC"] = null;
                Session["OTPIDC"] = null;
                Response.Redirect("login");
            }
            else
            {
                enableButton();
                divfailed.Visible = true;
                lblFailed.Text = admin.Message;
            }

        }

        protected void btnfailed_ServerClick(object sender, EventArgs e)
        {
            divfailed.Visible = false;
        }

        protected void lbtResendOTP_Click(object sender, EventArgs e)
        {
            UserService ad = new UserService();
            UserAuthOTPModel z = new UserAuthOTPModel();
            z.ID = Session["OTPIDC"].ToString();

            var admin = ad.resendOTPRegister(z);
            if (admin.Status == "success")
            {
                Session["OTPIDC"] = admin.Data.ToString();
                lblSuccess.Text = "Success resend OTP to your email.";
                divsuccess.Visible = true;
            }
            else
            {
                divfailed.Visible = true;
                lblFailed.Text = admin.Message;
            }

        }

        protected void btnsuccess_ServerClick(object sender, EventArgs e)
        {
            divsuccess.Visible = false;
        }
    }
}