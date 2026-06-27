using Newtonsoft.Json;
using QryptoCard.Dashboard.Models;
using QryptoCard.Dashboard.Models.Service;
using QryptoCard.Dashboard.Services;
using QryptoCard.Sec;
using System;
using System.Web.UI;

namespace QryptoCard.Dashboard
{
    // Account settings. Every action is scoped to the signed-in user: the REST tier resolves
    // identity from the bearer token, so nothing here sends a user id and a caller can only ever
    // read/change their own data. Only backend-supported sections are present (profile name,
    // password, email-via-OTP, referral); 2FA, sessions, KYC, notifications and account deletion
    // are intentionally omitted until they have endpoints.
    public partial class settings : Page
    {
        UserService us = new UserService();

        protected void Page_Load(object sender, EventArgs e)
        {
            // Same gate the other authenticated pages use: a valid session, or a cookie we can
            // resume from, otherwise bounce to login.
            if (Common.checkID())
            {
                if (!IsPostBack) loadInitial();
            }
            else if (Master.checkCookies())
            {
                if (!IsPostBack) loadInitial();
            }
            else
            {
                Master.forceLogin();
            }
        }

        void loadInitial()
        {
            // Prefill the profile from the cached session identity (set at login) — no extra
            // round-trip, and no id is ever sent to fetch "someone's" profile.
            txtFirstName.Text = SessionLib.Current.FirstName;
            txtLastName.Text = SessionLib.Current.LastName;
            txtCurrentEmail.Text = SessionLib.Current.Email;
            loadReferral();
        }

        void loadReferral()
        {
            var op = us.getReferralCode(new UserReferralModel());
            if (op.Status == "success" && op.Data != null)
            {
                var dt = JsonConvert.DeserializeObject<UserReferralModel>(op.Data.ToString());
                txtReferralCode.Text = dt.Code;
                txtReferralLink.Text = KeyModel.REFERRAL_URL + dt.Code;
            }
        }

        protected void btnSaveProfile_Click(object sender, EventArgs e)
        {
            try
            {
                var m = new UserModel
                {
                    FirstName = (txtFirstName.Text ?? "").Trim(),
                    LastName = (txtLastName.Text ?? "").Trim()
                };
                var op = us.updateUserData(m);
                if (op.Status == "success")
                    showMsg(true, "Your profile has been updated.");
                else
                    showMsg(false, op.Message);
            }
            catch (Exception ex)
            {
                showMsg(false, ex.Message);
            }
        }

        protected void btnSendEmailOtp_Click(object sender, EventArgs e)
        {
            try
            {
                var newEmail = (txtNewEmail.Text ?? "").Trim();
                if (newEmail == "")
                {
                    showMsg(false, "Enter the new email address.");
                    return;
                }

                var op = us.updateEmailOTP(new UserModel { Email = newEmail });
                if (op.Status == "success")
                {
                    // Hold the OTP session id and the proposed address across the confirm postback.
                    hfEmailOtpId.Value = op.Data == null ? "" : op.Data.ToString();
                    hfNewEmail.Value = newEmail;
                    pnlEmailOtp.Visible = true;
                    showMsg(true, "We sent a verification code to " + newEmail + ".");
                }
                else
                {
                    showMsg(false, op.Message);
                }
            }
            catch (Exception ex)
            {
                showMsg(false, ex.Message);
            }
        }

        protected void btnConfirmEmail_Click(object sender, EventArgs e)
        {
            try
            {
                var code = (txtEmailOtp.Text ?? "").Trim();
                if (hfEmailOtpId.Value == "" || hfNewEmail.Value == "")
                {
                    showMsg(false, "Request a verification code first.");
                    return;
                }
                if (code == "")
                {
                    showMsg(false, "Enter the verification code.");
                    return;
                }

                // The new address is bound to the OTP server-side at request time; the confirm
                // only needs the session id + code. hfNewEmail is used solely for the UI message.
                var op = us.updateEmail(new UserOTPModel
                {
                    OTPID = hfEmailOtpId.Value,
                    Code = code
                });

                if (op.Status == "success")
                {
                    txtCurrentEmail.Text = hfNewEmail.Value;
                    txtNewEmail.Text = "";
                    txtEmailOtp.Text = "";
                    hfEmailOtpId.Value = "";
                    hfNewEmail.Value = "";
                    pnlEmailOtp.Visible = false;
                    showMsg(true, "Your email address has been updated.");
                }
                else
                {
                    pnlEmailOtp.Visible = true;
                    showMsg(false, op.Message);
                }
            }
            catch (Exception ex)
            {
                showMsg(false, ex.Message);
            }
        }

        protected void btnChangePw_Click(object sender, EventArgs e)
        {
            try
            {
                var cur = txtCurrentPw.Text ?? "";
                var pw = txtNewPw.Text ?? "";
                var conf = txtConfirmPw.Text ?? "";

                if (cur == "" || pw == "" || conf == "")
                {
                    showMsg(false, "Fill in all password fields.");
                    return;
                }
                if (pw != conf)
                {
                    showMsg(false, "The new passwords do not match.");
                    return;
                }
                string pwMsg;
                if (!PasswordPolicy.Validate(pw.Trim(), out pwMsg))
                {
                    showMsg(false, pwMsg);
                    return;
                }

                // Encrypt to the APP form before sending, exactly like the login flow — the INT
                // tier compares against the stored credential in this form.
                var op = us.updatePassword(new PasswordChangeModel
                {
                    CurrentPassword = Secure.EncryptAPP(cur),
                    Password = Secure.EncryptAPP(pw)
                });

                if (op.Status == "success")
                {
                    txtCurrentPw.Text = "";
                    txtNewPw.Text = "";
                    txtConfirmPw.Text = "";
                    showMsg(true, "Your password has been updated.");
                }
                else
                {
                    showMsg(false, op.Message);
                }
            }
            catch (Exception ex)
            {
                showMsg(false, ex.Message);
            }
        }

        void showMsg(bool ok, string message)
        {
            pnlMsg.Visible = true;
            pnlMsg.CssClass = ok ? "set-alert ok" : "set-alert err";
            lblMsg.Text = Server.HtmlEncode(string.IsNullOrEmpty(message)
                ? (ok ? "Done." : "Something went wrong.")
                : message);
        }
    }
}
