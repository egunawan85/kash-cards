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
    public partial class otplogin : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                if (Session["OTPIDC"] == null)
                    Response.Redirect("Login");
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
                lblFailed.Text = "All textbox should be filled";
                divfailed.Visible = true;
                return;
            }

            // Replace loginVerify(z) with AuthClient.MintAfterOtpVerify. Login.aspx
            // already POSTed (email, password) to /v1/auth/login and stashed OTPIDC;
            // here we collect the 6-digit OTP and call /v1/auth/mint-after-otp, which
            // verifies the OTP and mints the (access, refresh) token pair. The mint
            // response carries a Profile field — the JSON-serialized user record
            // (Password + PIN nulled) — matching what loginVerify returned, so the
            // SessionLib population shape is unchanged.
            var otpId = Session["OTPIDC"].ToString();
            var otpCode = icode1.Value + icode2.Value + icode3.Value + icode4.Value + icode5.Value + icode6.Value;

            var mintOp = AuthClient.MintAfterOtpVerify(otpId, otpCode);
            if (mintOp.Status == "success")
            {
                enableButton();
                var resp = JsonConvert.DeserializeObject<AuthMintResponse>(mintOp.Data.ToString());
                if (resp == null || string.IsNullOrEmpty(resp.Profile))
                {
                    divfailed.Visible = true;
                    lblFailed.Text = "Login completed but profile data missing";
                    return;
                }
                // AuthClient.MintAfterOtpVerify already populated SessionLib's token
                // fields. Now populate the profile fields from resp.Profile.
                var dt = JsonConvert.DeserializeObject<UserModel>(resp.Profile);
                SessionLib.Current.SessionID = dt.UserID;
                SessionLib.Current.UserID = dt.UserID;
                SessionLib.Current.FirstName = dt.FirstName;
                SessionLib.Current.LastName = dt.LastName;
                SessionLib.Current.Email = dt.Email;
                SessionLib.Current.DateJoin = dt.DateJoin;
                Session["QRTYHC"] = null;
                Session["OTPIDC"] = null;
                Response.Redirect("Dashboard");
            }
            else
            {
                enableButton();
                divfailed.Visible = true;
                lblFailed.Text = mintOp.Message;
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

            var admin = ad.resendOTP(z);
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