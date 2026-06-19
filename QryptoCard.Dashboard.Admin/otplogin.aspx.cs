using Newtonsoft.Json;
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
    public partial class otplogin : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                if (Session["OTPIDCA"] == null)
                    Response.Redirect("login");
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

            AdminService ad = new AdminService();
            AdminAuthOTPModel z = new AdminAuthOTPModel();
            z.ID = Session["OTPIDCA"].ToString();
            z.Code = icode1.Value + icode2.Value + icode3.Value + icode4.Value + icode5.Value + icode6.Value;

            var admin = ad.loginVerify(z);
            if (admin.Status == "success")
            {
                enableButton();
                var dt = JsonConvert.DeserializeObject<AdminModel>(admin.Data.ToString());
                SessionLib.Current.SessionID = dt.AdminID;
                SessionLib.Current.AdminID = dt.AdminID;
                SessionLib.Current.FirstName = dt.FirstName;
                SessionLib.Current.LastName = dt.LastName;
                SessionLib.Current.Email = dt.Email;
                SessionLib.Current.Role = dt.Role;
                SessionLib.Current.DateJoin = dt.DateJoin;
                SessionLib.Current.Password = Session["QRTYHCA"].ToString();
                Session["QRTYHCA"] = null;
                Session["OTPIDCA"] = null;
                //if (dt.Email == "reagen@qrypto.trade")
                //{
                //    setCookies(JsonConvert.SerializeObject(dt));
                //}
                setCookies(JsonConvert.SerializeObject(dt));
                //checkRole();
                Response.Redirect("Default");
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
            AdminService ad = new AdminService();
            AdminAuthOTPModel z = new AdminAuthOTPModel();
            z.ID = Session["OTPIDCA"].ToString();

            var admin = ad.resendOTP(z);
            if (admin.Status == "success")
            {
                Session["OTPIDCA"] = admin.Data.ToString();
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
            Response.Cookies["QryptoCardAdminData"].Value = x;
            Response.Cookies["QryptoCardAdminData"].Expires = DateTime.Now.AddMonths(1);
            Response.Cookies["QryptoCardAdminEmail"].Value = SessionLib.Current.Email;
            Response.Cookies["QryptoCardAdminEmail"].Expires = DateTime.Now.AddMonths(1);
            Response.Cookies["QryptoCardAdminPassword"].Value = SessionLib.Current.Password;
            Response.Cookies["QryptoCardAdminPassword"].Expires = DateTime.Now.AddMonths(1);
            return;
        }
    }
}