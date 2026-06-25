using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Mail;
using System.Text;
using System.Web;

namespace QryptoCard.INT.Callback.Service
{
    public class NotificationService
    {
        public static void sendEmailFailed(string em, string card, string merchant, string amount, string rx)
        {
            // Postmark SMTP. The From must be a Postmark-verified sender signature; the SMTP
            // login is the Postmark Server API Token used as BOTH username and password (Postmark's
            // convention). Config is non-secret/env-overridable (defaults target Postmark); the
            // token is a Require()d secret. Read directly from SecretsConfig so this money-tier
            // callback stays self-contained (no dependency on the main INT tier / KeyModel).
            string from = QryptoCard.Sec.SecretsConfig.GetOptional("EMAIL_FROM", "no-reply@kash.cards");
            string gateway = QryptoCard.Sec.SecretsConfig.GetOptional("EMAIL_SMTP_GATEWAY", "smtp.postmarkapp.com");
            int port = Convert.ToInt32(QryptoCard.Sec.SecretsConfig.GetOptional("EMAIL_SMTP_PORT", "587"));
            string token = QryptoCard.Sec.SecretsConfig.Require("POSTMARK_SERVER_TOKEN");

            MailMessage message = new MailMessage(from, em);
            message.Subject = "Card Transaction Failed";
            message.Body = sendEmailFailed(card, merchant, amount, rx);
            message.BodyEncoding = Encoding.UTF8;
            message.IsBodyHtml = true;

            try
            {
                using (SmtpClient client = new SmtpClient(gateway, port))
                {
                    client.EnableSsl = true; // STARTTLS on 587
                    client.UseDefaultCredentials = false;
                    client.Credentials = new System.Net.NetworkCredential(token, token);
                    client.Send(message);
                }
            }
            catch (Exception ex)
            {
                // Best-effort notification: a failed-card email must never abort callback
                // processing (the transaction is already persisted) or surface SMTP internals.
                System.Diagnostics.Trace.TraceError("Transaction-failed email send failed: " + ex);
            }
        }

        private static string sendEmailFailed(string card, string merchant, string amount, string message)
        {
            string body = string.Empty;
            using (StreamReader reader = new StreamReader(HttpContext.Current.Server.MapPath("~/Content/Email/TransaactionFailed.html")))
            {
                body = reader.ReadToEnd();
            }
            body = body.Replace("{card}", card);
            body = body.Replace("{merchant}", merchant);
            body = body.Replace("{amount}", amount);
            body = body.Replace("{message}", message);
            return body;
        }
    }
}