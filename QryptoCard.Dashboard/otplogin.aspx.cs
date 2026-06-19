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

            UserService ad = new UserService();
            UserAuthOTPModel z = new UserAuthOTPModel();
            z.ID = Session["OTPIDC"].ToString();
            z.Code = icode1.Value + icode2.Value + icode3.Value + icode4.Value + icode5.Value + icode6.Value;

            var admin = ad.loginVerify(z);
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
                SessionLib.Current.Password = Session["QRTYHC"].ToString();
                Session["QRTYHC"] = null;
                Session["OTPIDC"] = null;
                //if (dt.Email == "reagen@qrypto.trade")
                //{
                //    setCookies(JsonConvert.SerializeObject(dt));
                //}
                setCookies(JsonConvert.SerializeObject(dt));
                //checkRole();
                Response.Redirect("Dashboard");
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

        public void setCookies(string x)
        {
            Response.Cookies["QryptoCardData"].Value = x;
            Response.Cookies["QryptoCardData"].Expires = DateTime.Now.AddMonths(1);
            Response.Cookies["QryptoCardEmail"].Value = SessionLib.Current.Email;
            Response.Cookies["QryptoCardEmail"].Expires = DateTime.Now.AddMonths(1);
            Response.Cookies["QryptoCardPassword"].Value = SessionLib.Current.Password;
            Response.Cookies["QryptoCardPassword"].Expires = DateTime.Now.AddMonths(1);
            return;
        }
    }
}