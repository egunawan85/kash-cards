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
            string to = em; //To address    
            string from = "no-reply@qrypto.trade"; //From address   
            MailMessage message = new MailMessage(from, to);

            message.Subject = "Card Transaction Failed";
            message.Body = sendEmailFailed(card, merchant, amount, rx);
            message.BodyEncoding = Encoding.UTF8;
            message.IsBodyHtml = true;
            SmtpClient client = new SmtpClient("smtp.gmail.com", 587); //Gmail smtp    
            System.Net.NetworkCredential basicCredential1 = new
            System.Net.NetworkCredential("no-reply@qrypto.trade", QryptoCard.Sec.SecretsConfig.Require("EMAIL_PASSWORD"));
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