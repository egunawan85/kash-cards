using MailKit.Net.Smtp;
using MimeKit;
using QryptoCard.INT.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Policy;
using System.Text;
using System.Web;
using static System.Net.WebRequestMethods;

namespace QryptoCard.INT.Script.Service
{
    public class NotificationMailkitService
    {
        //public static void sendEmailPassword(string em, string user, string id)
        //{
        //    string to = em; //To address    
        //    string from = "noreply@kash.now"; //From address   
        //    MimeMessage message = new MimeMessage();
        //    message.From.Add(new MailboxAddress("noreply", from));
        //    message.To.Add(new MailboxAddress("Kash.Now Users", to));

        //    var bodyBuilder = new BodyBuilder();
        //    bodyBuilder.HtmlBody = sendEmailPassword(user, id);

        //    message.Subject = "Forgot Password Request";
        //    message.Body = bodyBuilder.ToMessageBody();

        //    using (var client = new MailKit.Net.Smtp.SmtpClient())
        //    {
        //        client.Connect("mail.spacemail.com", 465);


        //        // Note: since we don't have an OAuth2 token, disable
        //        // the XOAUTH2 authentication mechanism.
        //        client.AuthenticationMechanisms.Remove("XOAUTH2");

        //        // Note: only needed if the SMTP server requires authentication
        //        client.Authenticate("noreply@kash.now", "<moved-to-Postmark-in-hardening>");

        //        client.Send(message);
        //        client.Disconnect(true);
        //    }
        //    //try
        //    //{
        //    //    client.Send(message);
        //    //}

        //    //catch (Exception ex)
        //    //{
        //    //    throw ex;
        //    //}
        //}

        //private static string sendEmailPassword(string user, string id)
        //{
        //    string body = string.Empty;
        //    using (StreamReader reader = new StreamReader(HttpContext.Current.Server.MapPath("~/Content/Email/ForgotPassword.html")))
        //    {
        //        body = reader.ReadToEnd();
        //    }
        //    id = KeyModel.QRYPTO_URL_FORGOT_PASSWORD + id;
        //    body = body.Replace("{Fullname}", user);
        //    body = body.Replace("{id}", id);
        //    return body;
        //}

        //public static void sendEmailPasswordAdmin(string em, string user, string id)
        //{
        //    string to = em; //To address    
        //    string from = "noreply@kash.now"; //From address   

        //    MimeMessage message = new MimeMessage();
        //    message.From.Add(new MailboxAddress("noreply", from));
        //    message.To.Add(new MailboxAddress("Kash.Now Users", to));

        //    var bodyBuilder = new BodyBuilder();
        //    bodyBuilder.HtmlBody = sendEmailPasswordAdmin(user, id);

        //    message.Subject = "Forgot Password Request";
        //    message.Body = bodyBuilder.ToMessageBody();

        //    using (var client = new MailKit.Net.Smtp.SmtpClient())
        //    {
        //        client.Connect("mail.spacemail.com", 465);


        //        // Note: since we don't have an OAuth2 token, disable
        //        // the XOAUTH2 authentication mechanism.
        //        client.AuthenticationMechanisms.Remove("XOAUTH2");

        //        // Note: only needed if the SMTP server requires authentication
        //        client.Authenticate("noreply@kash.now", "<moved-to-Postmark-in-hardening>");

        //        client.Send(message);
        //        client.Disconnect(true);
        //    }

        //}

        //private static string sendEmailPasswordAdmin(string user, string id)
        //{
        //    string body = string.Empty;
        //    using (StreamReader reader = new StreamReader(HttpContext.Current.Server.MapPath("~/Content/Email/Admin/ForgotPassword.html")))
        //    {
        //        body = reader.ReadToEnd();
        //    }
        //    body = body.Replace("{Fullname}", user);
        //    body = body.Replace("{id}", id);
        //    return body;
        //}

        //public static void sendEmailOTP(string em, string user, string otp)
        //{
        //    string to = em; //To address    
        //    string from = "noreply@kash.now"; //From address   

        //    MimeMessage message = new MimeMessage();
        //    message.From.Add(new MailboxAddress("noreply", from));
        //    message.To.Add(new MailboxAddress("Kash.Now Users", to));

        //    var bodyBuilder = new BodyBuilder();
        //    bodyBuilder.HtmlBody = sendEmailOTP(user, otp);

        //    message.Subject = "Security Code";
        //    message.Body = bodyBuilder.ToMessageBody();

        //    using (var client = new MailKit.Net.Smtp.SmtpClient())
        //    {
        //        client.Connect("mail.spacemail.com", 465);


        //        // Note: since we don't have an OAuth2 token, disable
        //        // the XOAUTH2 authentication mechanism.
        //        client.AuthenticationMechanisms.Remove("XOAUTH2");

        //        // Note: only needed if the SMTP server requires authentication
        //        client.Authenticate("noreply@kash.now", "<moved-to-Postmark-in-hardening>");

        //        client.Send(message);
        //        client.Disconnect(true);
        //    }

        //}

        //private static string sendEmailOTP(string user, string otp)
        //{
        //    string body = string.Empty;
        //    using (StreamReader reader = new StreamReader(HttpContext.Current.Server.MapPath("~/Content/Email/SecurityCode.html")))
        //    {
        //        body = reader.ReadToEnd();
        //    }
        //    body = body.Replace("{Fullname}", user);
        //    body = body.Replace("{code}", otp);
        //    return body;
        //}

        //public static void sendEmailUserInvitation(string em, string company, string invitator, string user, string url)
        //{
        //    string to = em; //To address    
        //    string from = "noreply@kash.now"; //From address   

        //    MimeMessage message = new MimeMessage();
        //    message.From.Add(new MailboxAddress("noreply", from));
        //    message.To.Add(new MailboxAddress("Kash.Now Users", to));

        //    var bodyBuilder = new BodyBuilder();
        //    bodyBuilder.HtmlBody = sendEmailUserInvitation(company, invitator, user, url);

        //    message.Subject = invitator + " invited you to join " + company + " in Qrypto Card";
        //    message.Body = bodyBuilder.ToMessageBody();

        //    using (var client = new MailKit.Net.Smtp.SmtpClient())
        //    {
        //        client.Connect("mail.spacemail.com", 465);


        //        // Note: since we don't have an OAuth2 token, disable
        //        // the XOAUTH2 authentication mechanism.
        //        client.AuthenticationMechanisms.Remove("XOAUTH2");

        //        // Note: only needed if the SMTP server requires authentication
        //        client.Authenticate("noreply@kash.now", "<moved-to-Postmark-in-hardening>");

        //        client.Send(message);
        //        client.Disconnect(true);
        //    }
        //}

        //private static string sendEmailUserInvitation(string company, string invitator, string user, string url)
        //{
        //    string body = string.Empty;
        //    using (StreamReader reader = new StreamReader(HttpContext.Current.Server.MapPath("~/Content/Email/InvitedAccount.html")))
        //    {
        //        body = reader.ReadToEnd();
        //    }
        //    body = body.Replace("{Fullname}", user);
        //    body = body.Replace("{Invitator}", invitator);
        //    body = body.Replace("{Company}", company);
        //    body = body.Replace("{url}", url);
        //    return body;
        //}

        //public static void sendEmailAdminInvitation(string em, string invitator, string user, string url)
        //{
        //    string to = em; //To address    
        //    string from = "noreply@kash.now"; //From address   

        //    MimeMessage message = new MimeMessage();
        //    message.From.Add(new MailboxAddress("noreply", from));
        //    message.To.Add(new MailboxAddress("Kash.Now Users", to));

        //    var bodyBuilder = new BodyBuilder();
        //    bodyBuilder.HtmlBody = sendEmailAdminInvitation(invitator, user, url);

        //    message.Subject = invitator + " invited you to join Qrypto Card Administrator!";
        //    message.Body = bodyBuilder.ToMessageBody();

        //    using (var client = new MailKit.Net.Smtp.SmtpClient())
        //    {
        //        client.Connect("mail.spacemail.com", 465);


        //        // Note: since we don't have an OAuth2 token, disable
        //        // the XOAUTH2 authentication mechanism.
        //        client.AuthenticationMechanisms.Remove("XOAUTH2");

        //        // Note: only needed if the SMTP server requires authentication
        //        client.Authenticate("noreply@kash.now", "<moved-to-Postmark-in-hardening>");

        //        client.Send(message);
        //        client.Disconnect(true);
        //    }
        //}

        //private static string sendEmailAdminInvitation(string invitator, string user, string url)
        //{
        //    string body = string.Empty;
        //    using (StreamReader reader = new StreamReader(HttpContext.Current.Server.MapPath("~/Content/Email/Admin/InvitedAccount.html")))
        //    {
        //        body = reader.ReadToEnd();
        //    }
        //    body = body.Replace("{Fullname}", user);
        //    body = body.Replace("{Invitator}", invitator);
        //    body = body.Replace("{url}", url);
        //    return body;
        //}

        //public static void sendEmailOTPRegister(string em, string user, string otp)
        //{
        //    string to = em; //To address    
        //    string from = "noreply@kash.now"; //From address   

        //    MimeMessage message = new MimeMessage();
        //    message.From.Add(new MailboxAddress("noreply", from));
        //    message.To.Add(new MailboxAddress("Kash.Now Users", to));

        //    var bodyBuilder = new BodyBuilder();
        //    bodyBuilder.HtmlBody = sendEmailOTPRegister(user, otp);

        //    message.Subject = "Welcome to Kash.Now!";
        //    message.Body = bodyBuilder.ToMessageBody();

        //    using (var client = new MailKit.Net.Smtp.SmtpClient())
        //    {
        //        client.Connect("mail.spacemail.com", 465);


        //        // Note: since we don't have an OAuth2 token, disable
        //        // the XOAUTH2 authentication mechanism.
        //        client.AuthenticationMechanisms.Remove("XOAUTH2");

        //        // Note: only needed if the SMTP server requires authentication
        //        client.Authenticate("noreply@kash.now", "<moved-to-Postmark-in-hardening>");

        //        client.Send(message);
        //        client.Disconnect(true);
        //    }


        //}

        //private static string sendEmailOTPRegister(string user, string otp)
        //{
        //    string body = string.Empty;
        //    using (StreamReader reader = new StreamReader(HttpContext.Current.Server.MapPath("~/Content/Email/VerifyAccountCode.html")))
        //    {
        //        body = reader.ReadToEnd();
        //    }
        //    body = body.Replace("{Fullname}", user);
        //    body = body.Replace("{code}", otp);
        //    return body;
        //}




        public static void sendEmailPassword(string em, string user, string id)
        {
            string to = em; //To address    
            string from = KeyModel.EMAIL_ADDRESS; //From address   
            MimeMessage message = new MimeMessage();
            message.From.Add(new MailboxAddress("noreply", from));
            message.To.Add(new MailboxAddress("Kash.Now Users", to));

            var bodyBuilder = new BodyBuilder();
            bodyBuilder.HtmlBody = sendEmailPassword(user, id);

            message.Subject = "Forgot Password Request";
            message.Body = bodyBuilder.ToMessageBody();

            try
            {
                using (var client = new SmtpClient())
                {
                    client.Connect(KeyModel.EMAIL_SMTP_GATEWAY, KeyModel.EMAIL_SMTP_PORT);
                    client.AuthenticationMechanisms.Remove("XOAUTH2"); // no OAuth2 token
                    client.Authenticate(KeyModel.EMAIL_SMTP_USER, KeyModel.EMAIL_PASSWORD);
                    client.Send(message);
                    client.Disconnect(true);
                }
            }
            catch (Exception ex)
            {
                // Log the real cause server-side; surface a clean message (no SMTP internals to the client).
                System.Diagnostics.Trace.TraceError("Email send failed: " + ex);
                throw new Exception("Unable to send email. Please try again.");
            }
            //try
            //{
            //    client.Send(message);
            //}

            //catch (Exception ex)
            //{
            //    throw ex;
            //}
        }

        private static string sendEmailPassword(string user, string id)
        {
            string body = string.Empty;
            using (StreamReader reader = new StreamReader(HttpContext.Current.Server.MapPath("~/Content/Email/ForgotPassword.html")))
            {
                body = reader.ReadToEnd();
            }
            id = KeyModel.QRYPTO_URL_FORGOT_PASSWORD + id;
            body = body.Replace("{Fullname}", user);
            body = body.Replace("{id}", id);
            return body;
        }

        public static void sendEmailPasswordAdmin(string em, string user, string id)
        {
            string to = em; //To address    
            string from = KeyModel.EMAIL_ADDRESS; //From address   

            MimeMessage message = new MimeMessage();
            message.From.Add(new MailboxAddress("noreply", from));
            message.To.Add(new MailboxAddress("Kash.Now Users", to));

            var bodyBuilder = new BodyBuilder();
            bodyBuilder.HtmlBody = sendEmailPasswordAdmin(user, id);

            message.Subject = "Forgot Password Request";
            message.Body = bodyBuilder.ToMessageBody();

            try
            {
                using (var client = new SmtpClient())
                {
                    client.Connect(KeyModel.EMAIL_SMTP_GATEWAY, KeyModel.EMAIL_SMTP_PORT);
                    client.AuthenticationMechanisms.Remove("XOAUTH2"); // no OAuth2 token
                    client.Authenticate(KeyModel.EMAIL_SMTP_USER, KeyModel.EMAIL_PASSWORD);
                    client.Send(message);
                    client.Disconnect(true);
                }
            }
            catch (Exception ex)
            {
                // Log the real cause server-side; surface a clean message (no SMTP internals to the client).
                System.Diagnostics.Trace.TraceError("Email send failed: " + ex);
                throw new Exception("Unable to send email. Please try again.");
            }

        }

        private static string sendEmailPasswordAdmin(string user, string id)
        {
            string body = string.Empty;
            using (StreamReader reader = new StreamReader(HttpContext.Current.Server.MapPath("~/Content/Email/Admin/ForgotPassword.html")))
            {
                body = reader.ReadToEnd();
            }
            body = body.Replace("{Fullname}", user);
            body = body.Replace("{id}", id);
            return body;
        }

        public static void sendEmailOTP(string em, string user, string otp)
        {
            string to = em; //To address    
            string from = KeyModel.EMAIL_ADDRESS; //From address   

            MimeMessage message = new MimeMessage();
            message.From.Add(new MailboxAddress("noreply", from));
            message.To.Add(new MailboxAddress("Kash.Now Users", to));

            var bodyBuilder = new BodyBuilder();
            bodyBuilder.HtmlBody = sendEmailOTP(user, otp);

            message.Subject = "Security Code";
            message.Body = bodyBuilder.ToMessageBody();

            try
            {
                using (var client = new SmtpClient())
                {
                    client.Connect(KeyModel.EMAIL_SMTP_GATEWAY, KeyModel.EMAIL_SMTP_PORT);
                    client.AuthenticationMechanisms.Remove("XOAUTH2"); // no OAuth2 token
                    client.Authenticate(KeyModel.EMAIL_SMTP_USER, KeyModel.EMAIL_PASSWORD);
                    client.Send(message);
                    client.Disconnect(true);
                }
            }
            catch (Exception ex)
            {
                // Log the real cause server-side; surface a clean message (no SMTP internals to the client).
                System.Diagnostics.Trace.TraceError("Email send failed: " + ex);
                throw new Exception("Unable to send email. Please try again.");
            }

        }

        private static string sendEmailOTP(string user, string otp)
        {
            string body = string.Empty;
            using (StreamReader reader = new StreamReader(HttpContext.Current.Server.MapPath("~/Content/Email/SecurityCode.html")))
            {
                body = reader.ReadToEnd();
            }
            body = body.Replace("{Fullname}", user);
            body = body.Replace("{code}", otp);
            return body;
        }

        public static void sendEmailUserInvitation(string em, string company, string invitator, string user, string url)
        {
            string to = em; //To address    
            string from = KeyModel.EMAIL_ADDRESS; //From address   

            MimeMessage message = new MimeMessage();
            message.From.Add(new MailboxAddress("noreply", from));
            message.To.Add(new MailboxAddress("Kash.Now Users", to));

            var bodyBuilder = new BodyBuilder();
            bodyBuilder.HtmlBody = sendEmailUserInvitation(company, invitator, user, url);

            message.Subject = invitator + " invited you to join " + company + " in Qrypto Card";
            message.Body = bodyBuilder.ToMessageBody();

            try
            {
                using (var client = new SmtpClient())
                {
                    client.Connect(KeyModel.EMAIL_SMTP_GATEWAY, KeyModel.EMAIL_SMTP_PORT);
                    client.AuthenticationMechanisms.Remove("XOAUTH2"); // no OAuth2 token
                    client.Authenticate(KeyModel.EMAIL_SMTP_USER, KeyModel.EMAIL_PASSWORD);
                    client.Send(message);
                    client.Disconnect(true);
                }
            }
            catch (Exception ex)
            {
                // Log the real cause server-side; surface a clean message (no SMTP internals to the client).
                System.Diagnostics.Trace.TraceError("Email send failed: " + ex);
                throw new Exception("Unable to send email. Please try again.");
            }
        }

        private static string sendEmailUserInvitation(string company, string invitator, string user, string url)
        {
            string body = string.Empty;
            using (StreamReader reader = new StreamReader(HttpContext.Current.Server.MapPath("~/Content/Email/InvitedAccount.html")))
            {
                body = reader.ReadToEnd();
            }
            body = body.Replace("{Fullname}", user);
            body = body.Replace("{Invitator}", invitator);
            body = body.Replace("{Company}", company);
            body = body.Replace("{url}", url);
            return body;
        }

        public static void sendEmailAdminInvitation(string em, string invitator, string user, string url)
        {
            string to = em; //To address    
            string from = KeyModel.EMAIL_ADDRESS; //From address   

            MimeMessage message = new MimeMessage();
            message.From.Add(new MailboxAddress("noreply", from));
            message.To.Add(new MailboxAddress("Kash.Now Users", to));

            var bodyBuilder = new BodyBuilder();
            bodyBuilder.HtmlBody = sendEmailAdminInvitation(invitator, user, url);

            message.Subject = invitator + " invited you to join Qrypto Card Administrator!";
            message.Body = bodyBuilder.ToMessageBody();

            try
            {
                using (var client = new SmtpClient())
                {
                    client.Connect(KeyModel.EMAIL_SMTP_GATEWAY, KeyModel.EMAIL_SMTP_PORT);
                    client.AuthenticationMechanisms.Remove("XOAUTH2"); // no OAuth2 token
                    client.Authenticate(KeyModel.EMAIL_SMTP_USER, KeyModel.EMAIL_PASSWORD);
                    client.Send(message);
                    client.Disconnect(true);
                }
            }
            catch (Exception ex)
            {
                // Log the real cause server-side; surface a clean message (no SMTP internals to the client).
                System.Diagnostics.Trace.TraceError("Email send failed: " + ex);
                throw new Exception("Unable to send email. Please try again.");
            }
        }

        private static string sendEmailAdminInvitation(string invitator, string user, string url)
        {
            string body = string.Empty;
            using (StreamReader reader = new StreamReader(HttpContext.Current.Server.MapPath("~/Content/Email/Admin/InvitedAccount.html")))
            {
                body = reader.ReadToEnd();
            }
            body = body.Replace("{Fullname}", user);
            body = body.Replace("{Invitator}", invitator);
            body = body.Replace("{url}", url);
            return body;
        }

        public static void sendEmailOTPRegister(string em, string user, string otp)
        {
            string to = em; //To address    
            string from = KeyModel.EMAIL_ADDRESS; //From address   

            MimeMessage message = new MimeMessage();
            message.From.Add(new MailboxAddress("noreply", from));
            message.To.Add(new MailboxAddress("Kash.Now Users", to));

            var bodyBuilder = new BodyBuilder();
            bodyBuilder.HtmlBody = sendEmailOTPRegister(user, otp);

            message.Subject = "Welcome to Kash.Now!";
            message.Body = bodyBuilder.ToMessageBody();

            try
            {
                using (var client = new SmtpClient())
                {
                    client.Connect(KeyModel.EMAIL_SMTP_GATEWAY, KeyModel.EMAIL_SMTP_PORT);
                    client.AuthenticationMechanisms.Remove("XOAUTH2"); // no OAuth2 token
                    client.Authenticate(KeyModel.EMAIL_SMTP_USER, KeyModel.EMAIL_PASSWORD);
                    client.Send(message);
                    client.Disconnect(true);
                }
            }
            catch (Exception ex)
            {
                // Log the real cause server-side; surface a clean message (no SMTP internals to the client).
                System.Diagnostics.Trace.TraceError("Email send failed: " + ex);
                throw new Exception("Unable to send email. Please try again.");
            }


        }

        private static string sendEmailOTPRegister(string user, string otp)
        {
            string body = string.Empty;
            using (StreamReader reader = new StreamReader(HttpContext.Current.Server.MapPath("~/Content/Email/VerifyAccountCode.html")))
            {
                body = reader.ReadToEnd();
            }
            body = body.Replace("{Fullname}", user);
            body = body.Replace("{code}", otp);
            return body;
        }

        // Deposit-into-card terminal-state notification (E6). Called from the issuance tick's notify
        // sweep when a funding intent reaches Completed / Failed / Expired. Body is built INLINE (no
        // Server.MapPath / HttpContext dependency) so it is safe to send from the scheduled background
        // tick, which has no page context. Throws on send failure so the caller logs it (the sweep has
        // already claimed the row, so this is at-most-once — no retry storm).
        public static void sendCardFundingUpdate(string em, string user, bool success, bool topUp, string status)
        {
            string to = em;
            string from = KeyModel.EMAIL_ADDRESS;

            MimeMessage message = new MimeMessage();
            message.From.Add(new MailboxAddress("noreply", from));
            message.To.Add(new MailboxAddress("Kash Cards", to));
            message.Subject = success ? "Your card is ready" : "Your card funding didn't complete";

            var bodyBuilder = new BodyBuilder();
            bodyBuilder.HtmlBody = buildCardFundingBody(user, success, topUp, status);
            message.Body = bodyBuilder.ToMessageBody();

            try
            {
                using (var client = new SmtpClient())
                {
                    client.Connect(KeyModel.EMAIL_SMTP_GATEWAY, KeyModel.EMAIL_SMTP_PORT);
                    client.AuthenticationMechanisms.Remove("XOAUTH2");
                    client.Authenticate(KeyModel.EMAIL_SMTP_USER, KeyModel.EMAIL_PASSWORD);
                    client.Send(message);
                    client.Disconnect(true);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.TraceError("Card-funding email send failed: " + ex);
                throw;
            }
        }

        private static string buildCardFundingBody(string user, bool success, bool topUp, string status)
        {
            string greetingName = string.IsNullOrWhiteSpace(user) ? "there" : user;
            string headline, detail;
            if (success)
            {
                headline = topUp ? "Your top-up is complete" : "Your card is ready";
                detail = topUp
                    ? "Your funds have landed and your card balance is updated. You can view it in My Cards."
                    : "Your funds have landed and your new card is live. You can view it in My Cards.";
            }
            else
            {
                bool expired = string.Equals(status, "Expired", StringComparison.OrdinalIgnoreCase);
                headline = "Your card funding didn't complete";
                detail = (expired
                    ? "This request expired before your deposit was fully received. "
                    : "This funding could not be completed. ")
                    + "Any crypto already received stays in your available balance for your next purchase.";
            }

            return "<div style=\"font-family:Arial,Helvetica,sans-serif;color:#0c141c;font-size:15px;line-height:1.5\">"
                + "<p>Hi " + System.Web.HttpUtility.HtmlEncode(greetingName) + ",</p>"
                + "<h2 style=\"margin:0 0 8px\">" + System.Web.HttpUtility.HtmlEncode(headline) + "</h2>"
                + "<p>" + System.Web.HttpUtility.HtmlEncode(detail) + "</p>"
                + "<p style=\"color:#5b6b7a;font-size:13px\">You're receiving this because you started a card funding on Kash Cards.</p>"
                + "</div>";
        }
    }
}