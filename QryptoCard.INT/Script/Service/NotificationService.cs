using MimeKit;
using QryptoCard.INT.Model;
using System.IO;
using System.Web;
using System.Text;
using System;
using System.Net.Mail;

namespace QryptoCard.INT.Script.Service
{
    public class NotificationService
    {
        public static void sendEmailPassword(string em, string user, string id)
        {
            string to = em; //To address    
            string from = "no-reply@qrypto.trade"; //From address   
            MailMessage message = new MailMessage(from, to);

            message.Subject = "Forgot Password Request";
            message.Body = sendEmailPassword(user, id);
            message.BodyEncoding = Encoding.UTF8;
            message.IsBodyHtml = true;
            SmtpClient client = new SmtpClient("smtp.gmail.com", 587); //Gmail smtp    
            System.Net.NetworkCredential basicCredential1 = new
            System.Net.NetworkCredential("no-reply@qrypto.trade", "olkebbjshtdbzcae");
            //System.Net.NetworkCredential("syaprilstudio@gmail.com", "jrwxdabkuhuyurhs");
            client.EnableSsl = true;
            client.UseDefaultCredentials = false;
            client.Credentials = basicCredential1;
            try
            {
                client.Send(message);
            }

            catch (Exception ex)
            {
                throw ex;
            }
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
            string from = "no-reply@qrypto.trade"; //From address   
            MailMessage message = new MailMessage(from, to);

            message.Subject = "Forgot Password Request";
            message.Body = sendEmailPasswordAdmin(user, id);
            message.BodyEncoding = Encoding.UTF8;
            message.IsBodyHtml = true;
            SmtpClient client = new SmtpClient("smtp.gmail.com", 587); //Gmail smtp    
            System.Net.NetworkCredential basicCredential1 = new
            System.Net.NetworkCredential("no-reply@qrypto.trade", "olkebbjshtdbzcae");
            //System.Net.NetworkCredential("syaprilstudio@gmail.com", "jrwxdabkuhuyurhs");
            client.EnableSsl = true;
            client.UseDefaultCredentials = false;
            client.Credentials = basicCredential1;
            try
            {
                client.Send(message);
            }

            catch (Exception ex)
            {
                throw ex;
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
            string from = "no-reply@qrypto.trade"; //From address   
            MailMessage message = new MailMessage(from, to);

            message.Subject = "Security Code";
            message.Body = sendEmailOTP(user, otp);
            message.BodyEncoding = Encoding.UTF8;
            message.IsBodyHtml = true;
            SmtpClient client = new SmtpClient("smtp.gmail.com", 587); //Gmail smtp    
            System.Net.NetworkCredential basicCredential1 = new
            System.Net.NetworkCredential("no-reply@qrypto.trade", "olkebbjshtdbzcae");
            //System.Net.NetworkCredential("syaprilstudio@gmail.com", "jrwxdabkuhuyurhs");
            client.EnableSsl = true;
            client.UseDefaultCredentials = false;
            client.Credentials = basicCredential1;
            try
            {
                client.Send(message);
            }

            catch (Exception ex)
            {
                throw ex;
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
            string from = "no-reply@qrypto.trade"; //From address   
            MailMessage message = new MailMessage(from, to);

            message.Subject = invitator + " invited you to join " + company + " in Qrypto";
            message.Body = sendEmailUserInvitation(company, invitator, user, url);
            message.BodyEncoding = Encoding.UTF8;
            message.IsBodyHtml = true;
            SmtpClient client = new SmtpClient("smtp.gmail.com", 587); //Gmail smtp    
            System.Net.NetworkCredential basicCredential1 = new
            System.Net.NetworkCredential("no-reply@qrypto.trade", "olkebbjshtdbzcae");
            //System.Net.NetworkCredential("syaprilstudio@gmail.com", "jrwxdabkuhuyurhs");
            client.EnableSsl = true;
            client.UseDefaultCredentials = false;
            client.Credentials = basicCredential1;
            try
            {
                client.Send(message);
            }

            catch (Exception ex)
            {
                throw ex;
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
            string from = "no-reply@qrypto.trade"; //From address   
            MailMessage message = new MailMessage(from, to);

            message.Subject = invitator + " invited you to join Qrypto Card Administrator!";
            message.Body = sendEmailAdminInvitation(invitator, user, url);
            message.BodyEncoding = Encoding.UTF8;
            message.IsBodyHtml = true;
            SmtpClient client = new SmtpClient("smtp.gmail.com", 587); //Gmail smtp    
            System.Net.NetworkCredential basicCredential1 = new
            System.Net.NetworkCredential("no-reply@qrypto.trade", "olkebbjshtdbzcae");
            //System.Net.NetworkCredential("syaprilstudio@gmail.com", "jrwxdabkuhuyurhs");
            client.EnableSsl = true;
            client.UseDefaultCredentials = false;
            client.Credentials = basicCredential1;
            try
            {
                client.Send(message);
            }

            catch (Exception ex)
            {
                throw ex;
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
            string from = "no-reply@qrypto.trade"; //From address   
            MailMessage message = new MailMessage(from, to);

            message.Subject = "Welcome to Kash.Now!";
            message.Body = sendEmailOTPRegister(user, otp);
            message.BodyEncoding = Encoding.UTF8;
            message.IsBodyHtml = true;
            SmtpClient client = new SmtpClient("smtp.gmail.com", 587); //Gmail smtp    
            System.Net.NetworkCredential basicCredential1 = new
            System.Net.NetworkCredential("no-reply@qrypto.trade", "olkebbjshtdbzcae");
            //System.Net.NetworkCredential("syaprilstudio@gmail.com", "jrwxdabkuhuyurhs");
            client.EnableSsl = true;
            client.UseDefaultCredentials = false;
            client.Credentials = basicCredential1;
            try
            {
                client.Send(message);
            }

            catch (Exception ex)
            {
                throw ex;
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
    }
}