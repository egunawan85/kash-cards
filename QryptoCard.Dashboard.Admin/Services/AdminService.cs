using Newtonsoft.Json;
using QryptoCard.Dashboard.Admin.Models.Service;
using QryptoCard.Dashboard.Admin.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Web;

namespace QryptoCard.Dashboard.Admin.Services
{
    public class AdminService
    {
        OutputModel op = new OutputModel();

        public OutputModel login(AdminModel adm)
        {
            try
            {
                Common.trustConnection();
                string path = "/v1/auth/login";
                WebClient client = new WebClient();
                client.Headers["Content-type"] = "application/json";
                string inputJson = JsonConvert.SerializeObject(adm);
                //client.Headers[HttpRequestHeader.Authorization] = string.Format("Basic {0}", Security.credentials(SessionLib.Current.user_email, SessionLib.Current.user_password));
                //client.Headers[HttpRequestHeader.Authorization] = string.Format("Basic {0}", Security.credentialsNoAuthUser());
                client.Encoding = Encoding.UTF8;
                ServicePointManager.ServerCertificateValidationCallback += (sender, certificate, chain, sslPolicyErrors) => true;
                return op = JsonConvert.DeserializeObject<OutputModel>(client.UploadString(KeyModel.API_URL + path, "POST", inputJson));
            }
            catch (Exception ex)
            {
                op.Message = ex.ToString();
                op.Status = "error";
                return op;
            }
        }

        public OutputModel loginVerify(AdminAuthOTPModel adm)
        {
            try
            {
                Common.trustConnection();
                string path = "/v1/auth/login/verify";
                WebClient client = new WebClient();
                client.Headers["Content-type"] = "application/json";
                string inputJson = JsonConvert.SerializeObject(adm);
                //client.Headers[HttpRequestHeader.Authorization] = string.Format("Basic {0}", Security.credentials(SessionLib.Current.user_email, SessionLib.Current.user_password));
                //client.Headers[HttpRequestHeader.Authorization] = string.Format("Basic {0}", Security.credentialsNoAuthUser());
                client.Encoding = Encoding.UTF8;
                ServicePointManager.ServerCertificateValidationCallback += (sender, certificate, chain, sslPolicyErrors) => true;
                return op = JsonConvert.DeserializeObject<OutputModel>(client.UploadString(KeyModel.API_URL + path, "POST", inputJson));
            }
            catch (Exception ex)
            {
                op.Message = ex.ToString();
                op.Status = "error";
                return op;
            }
        }

        public OutputModel resendOTP(AdminAuthOTPModel adm)
        {
            try
            {
                Common.trustConnection();
                string path = "/v1/auth/login/resend";
                WebClient client = new WebClient();
                client.Headers["Content-type"] = "application/json";
                string inputJson = JsonConvert.SerializeObject(adm);
                //client.Headers[HttpRequestHeader.Authorization] = string.Format("Basic {0}", Security.credentials(SessionLib.Current.user_email, SessionLib.Current.user_password));
                //client.Headers[HttpRequestHeader.Authorization] = string.Format("Basic {0}", Security.credentialsNoAuthUser());
                client.Encoding = Encoding.UTF8;
                ServicePointManager.ServerCertificateValidationCallback += (sender, certificate, chain, sslPolicyErrors) => true;
                return op = JsonConvert.DeserializeObject<OutputModel>(client.UploadString(KeyModel.API_URL + path, "POST", inputJson));
            }
            catch (Exception ex)
            {
                op.Message = ex.ToString();
                op.Status = "error";
                return op;
            }
        }

        public OutputModel forgotPassword(AdminModel adm)
        {
            try
            {
                Common.trustConnection();
                string path = "/v1/auth/password/forgot";
                WebClient client = new WebClient();
                client.Headers["Content-type"] = "application/json";
                string inputJson = JsonConvert.SerializeObject(adm);
                //client.Headers[HttpRequestHeader.Authorization] = string.Format("Basic {0}", Security.credentials(SessionLib.Current.user_email, SessionLib.Current.user_password));
                //client.Headers[HttpRequestHeader.Authorization] = string.Format("Basic {0}", Security.credentialsNoAuthUser());
                client.Encoding = Encoding.UTF8;
                ServicePointManager.ServerCertificateValidationCallback += (sender, certificate, chain, sslPolicyErrors) => true;
                return op = JsonConvert.DeserializeObject<OutputModel>(client.UploadString(KeyModel.API_URL + path, "POST", inputJson));
            }
            catch (Exception ex)
            {
                op.Message = ex.ToString();
                op.Status = "error";
                return op;
            }
        }

        public OutputModel checkForgotPassword(AdminForgotPasswordModel adm)
        {
            try
            {
                Common.trustConnection();
                string path = "/v1/auth/password/forgot/check";
                WebClient client = new WebClient();
                client.Headers["Content-type"] = "application/json";
                string inputJson = JsonConvert.SerializeObject(adm);
                //client.Headers[HttpRequestHeader.Authorization] = string.Format("Basic {0}", Security.credentials(SessionLib.Current.user_email, SessionLib.Current.user_password));
                //client.Headers[HttpRequestHeader.Authorization] = string.Format("Basic {0}", Security.credentialsNoAuthUser());
                client.Encoding = Encoding.UTF8;
                ServicePointManager.ServerCertificateValidationCallback += (sender, certificate, chain, sslPolicyErrors) => true;
                return op = JsonConvert.DeserializeObject<OutputModel>(client.UploadString(KeyModel.API_URL + path, "POST", inputJson));
            }
            catch (Exception ex)
            {
                op.Message = ex.ToString();
                op.Status = "error";
                return op;
            }
        }

        public OutputModel changeForgotPassword(AdminForgotPasswordModel adm)
        {
            try
            {
                Common.trustConnection();
                string path = "/v1/auth/password/forgot/change";
                WebClient client = new WebClient();
                client.Headers["Content-type"] = "application/json";
                string inputJson = JsonConvert.SerializeObject(adm);
                //client.Headers[HttpRequestHeader.Authorization] = string.Format("Basic {0}", Security.credentials(SessionLib.Current.user_email, SessionLib.Current.user_password));
                //client.Headers[HttpRequestHeader.Authorization] = string.Format("Basic {0}", Security.credentialsNoAuthUser());
                client.Encoding = Encoding.UTF8;
                ServicePointManager.ServerCertificateValidationCallback += (sender, certificate, chain, sslPolicyErrors) => true;
                return op = JsonConvert.DeserializeObject<OutputModel>(client.UploadString(KeyModel.API_URL + path, "POST", inputJson));
            }
            catch (Exception ex)
            {
                op.Message = ex.ToString();
                op.Status = "error";
                return op;
            }
        }

        public OutputModel getInvitedAdmin(AdminModel adm)
        {
            try
            {
                Common.trustConnection();
                string path = "/v1/auth/invited/check";
                WebClient client = new WebClient();
                client.Headers["Content-type"] = "application/json";
                string inputJson = JsonConvert.SerializeObject(adm);
                //client.Headers[HttpRequestHeader.Authorization] = string.Format("Basic {0}", Security.credentials(SessionLib.Current.user_email, SessionLib.Current.user_password));
                //client.Headers[HttpRequestHeader.Authorization] = string.Format("Basic {0}", Security.credentialsNoAuthUser());
                client.Encoding = Encoding.UTF8;
                ServicePointManager.ServerCertificateValidationCallback += (sender, certificate, chain, sslPolicyErrors) => true;
                return op = JsonConvert.DeserializeObject<OutputModel>(client.UploadString(KeyModel.API_URL + path, "POST", inputJson));
            }
            catch (Exception ex)
            {
                op.Message = ex.ToString();
                op.Status = "error";
                return op;
            }
        }
        public OutputModel onboardingAdmin(AdminModel adm)
        {
            try
            {
                Common.trustConnection();
                string path = "/v1/auth/invited/onboarding";
                WebClient client = new WebClient();
                client.Headers["Content-type"] = "application/json";
                string inputJson = JsonConvert.SerializeObject(adm);
                //client.Headers[HttpRequestHeader.Authorization] = string.Format("Basic {0}", Security.credentials(SessionLib.Current.user_email, SessionLib.Current.user_password));
                //client.Headers[HttpRequestHeader.Authorization] = string.Format("Basic {0}", Security.credentialsNoAuthUser());
                client.Encoding = Encoding.UTF8;
                ServicePointManager.ServerCertificateValidationCallback += (sender, certificate, chain, sslPolicyErrors) => true;
                return op = JsonConvert.DeserializeObject<OutputModel>(client.UploadString(KeyModel.API_URL + path, "PUT", inputJson));
            }
            catch (Exception ex)
            {
                op.Message = ex.ToString();
                op.Status = "error";
                return op;
            }
        }

        public OutputModel getAdminList(AdminFilterModel adm)
        {
            try
            {
                Common.trustConnection();
                string path = "/v1/admin/list";
                WebClient client = new WebClient();
                client.Headers["Content-type"] = "application/json";
                string inputJson = JsonConvert.SerializeObject(adm);
                client.Headers[HttpRequestHeader.Authorization] = string.Format("Basic {0}", Security.credentials(SessionLib.Current.Email, SessionLib.Current.Password));
                //client.Headers[HttpRequestHeader.Authorization] = string.Format("Basic {0}", Security.credentialsNoAuthAdmin());
                client.Encoding = Encoding.UTF8;
                ServicePointManager.ServerCertificateValidationCallback += (sender, certificate, chain, sslPolicyErrors) => true;
                return op = JsonConvert.DeserializeObject<OutputModel>(client.UploadString(KeyModel.API_URL + path, "POST", inputJson));
            }
            catch (Exception ex)
            {
                op.Message = ex.ToString();
                op.Status = "error";
                return op;
            }
        }

        public OutputModel getAdminDetail(AdminModel adm)
        {
            try
            {
                Common.trustConnection();
                string path = "/v1/admin/detail";
                WebClient client = new WebClient();
                client.Headers["Content-type"] = "application/json";
                string inputJson = JsonConvert.SerializeObject(adm);
                client.Headers[HttpRequestHeader.Authorization] = string.Format("Basic {0}", Security.credentials(SessionLib.Current.Email, SessionLib.Current.Password));
                //client.Headers[HttpRequestHeader.Authorization] = string.Format("Basic {0}", Security.credentialsNoAuthAdmin());
                client.Encoding = Encoding.UTF8;
                ServicePointManager.ServerCertificateValidationCallback += (sender, certificate, chain, sslPolicyErrors) => true;
                return op = JsonConvert.DeserializeObject<OutputModel>(client.UploadString(KeyModel.API_URL + path, "POST", inputJson));
            }
            catch (Exception ex)
            {
                op.Message = ex.ToString();
                op.Status = "error";
                return op;
            }
        }

        public OutputModel inviteAdmin(AdminModel adm)
        {
            try
            {
                Common.trustConnection();
                string path = "/v1/admin/invite";
                WebClient client = new WebClient();
                client.Headers["Content-type"] = "application/json";
                string inputJson = JsonConvert.SerializeObject(adm);
                client.Headers[HttpRequestHeader.Authorization] = string.Format("Basic {0}", Security.credentials(SessionLib.Current.Email, SessionLib.Current.Password));
                //client.Headers[HttpRequestHeader.Authorization] = string.Format("Basic {0}", Security.credentialsNoAuthUser());
                client.Encoding = Encoding.UTF8;
                ServicePointManager.ServerCertificateValidationCallback += (sender, certificate, chain, sslPolicyErrors) => true;
                return op = JsonConvert.DeserializeObject<OutputModel>(client.UploadString(KeyModel.API_URL + path, "POST", inputJson));
            }
            catch (Exception ex)
            {
                op.Message = ex.ToString();
                op.Status = "error";
                return op;
            }
        }

        public OutputModel banAdmin(AdminModel adm)
        {
            try
            {
                Common.trustConnection();
                string path = "/v1/admin/ban";
                WebClient client = new WebClient();
                client.Headers["Content-type"] = "application/json";
                string inputJson = JsonConvert.SerializeObject(adm);
                client.Headers[HttpRequestHeader.Authorization] = string.Format("Basic {0}", Security.credentials(SessionLib.Current.Email, SessionLib.Current.Password));
                //client.Headers[HttpRequestHeader.Authorization] = string.Format("Basic {0}", Security.credentialsNoAuthUser());
                client.Encoding = Encoding.UTF8;
                ServicePointManager.ServerCertificateValidationCallback += (sender, certificate, chain, sslPolicyErrors) => true;
                return op = JsonConvert.DeserializeObject<OutputModel>(client.UploadString(KeyModel.API_URL + path, "DELETE", inputJson));
            }
            catch (Exception ex)
            {
                op.Message = ex.ToString();
                op.Status = "error";
                return op;
            }
        }

        public OutputModel getAdminData(string id)
        {
            try
            {
                Common.trustConnection();
                string path = "/v1/admin/data/" + id;
                WebClient client = new WebClient();
                client.Headers["Content-type"] = "application/json";
                client.Headers[HttpRequestHeader.Authorization] = string.Format("Basic {0}", Security.credentials(SessionLib.Current.Email, SessionLib.Current.Password));
                //client.Headers[HttpRequestHeader.Authorization] = string.Format("Basic {0}", Security.credentialsNoAuthUser());
                client.Encoding = Encoding.UTF8;
                ServicePointManager.ServerCertificateValidationCallback += (sender, certificate, chain, sslPolicyErrors) => true;
                return op = JsonConvert.DeserializeObject<OutputModel>(client.DownloadString(KeyModel.API_URL + path));
            }
            catch (Exception ex)
            {
                op.Message = ex.ToString();
                op.Status = "error";
                return op;
            }
        }

        public OutputModel updateAdminData(AdminModel adm)
        {
            try
            {
                Common.trustConnection();
                string path = "/v1/admin/data";
                WebClient client = new WebClient();
                client.Headers["Content-type"] = "application/json";
                string inputJson = JsonConvert.SerializeObject(adm);
                client.Headers[HttpRequestHeader.Authorization] = string.Format("Basic {0}", Security.credentials(SessionLib.Current.Email, SessionLib.Current.Password));
                //client.Headers[HttpRequestHeader.Authorization] = string.Format("Basic {0}", Security.credentialsNoAuthUser());
                client.Encoding = Encoding.UTF8;
                ServicePointManager.ServerCertificateValidationCallback += (sender, certificate, chain, sslPolicyErrors) => true;
                return op = JsonConvert.DeserializeObject<OutputModel>(client.UploadString(KeyModel.API_URL + path, "PUT", inputJson));
            }
            catch (Exception ex)
            {
                op.Message = ex.ToString();
                op.Status = "error";
                return op;
            }
        }

        public OutputModel updatePassword(PasswordChangeModel adm)
        {
            try
            {
                Common.trustConnection();
                string path = "/v1/admin/password";
                WebClient client = new WebClient();
                client.Headers["Content-type"] = "application/json";
                string inputJson = JsonConvert.SerializeObject(adm);
                client.Headers[HttpRequestHeader.Authorization] = string.Format("Basic {0}", Security.credentials(SessionLib.Current.Email, SessionLib.Current.Password));
                //client.Headers[HttpRequestHeader.Authorization] = string.Format("Basic {0}", Security.credentialsNoAuthUser());
                client.Encoding = Encoding.UTF8;
                ServicePointManager.ServerCertificateValidationCallback += (sender, certificate, chain, sslPolicyErrors) => true;
                return op = JsonConvert.DeserializeObject<OutputModel>(client.UploadString(KeyModel.API_URL + path, "PUT", inputJson));
            }
            catch (Exception ex)
            {
                op.Message = ex.ToString();
                op.Status = "error";
                return op;
            }
        }

        public OutputModel updateEmailOTP(AdminModel adm)
        {
            try
            {
                Common.trustConnection();
                string path = "/v1/admin/email/otp";
                WebClient client = new WebClient();
                client.Headers["Content-type"] = "application/json";
                string inputJson = JsonConvert.SerializeObject(adm);
                client.Headers[HttpRequestHeader.Authorization] = string.Format("Basic {0}", Security.credentials(SessionLib.Current.Email, SessionLib.Current.Password));
                //client.Headers[HttpRequestHeader.Authorization] = string.Format("Basic {0}", Security.credentialsNoAuthUser());
                client.Encoding = Encoding.UTF8;
                ServicePointManager.ServerCertificateValidationCallback += (sender, certificate, chain, sslPolicyErrors) => true;
                return op = JsonConvert.DeserializeObject<OutputModel>(client.UploadString(KeyModel.API_URL + path, "POST", inputJson));
            }
            catch (Exception ex)
            {
                op.Message = ex.ToString();
                op.Status = "error";
                return op;
            }
        }

        public OutputModel updateEmail(AdminOTPModel adm)
        {
            try
            {
                Common.trustConnection();
                string path = "/v1/admin/email";
                WebClient client = new WebClient();
                client.Headers["Content-type"] = "application/json";
                string inputJson = JsonConvert.SerializeObject(adm);
                client.Headers[HttpRequestHeader.Authorization] = string.Format("Basic {0}", Security.credentials(SessionLib.Current.Email, SessionLib.Current.Password));
                //client.Headers[HttpRequestHeader.Authorization] = string.Format("Basic {0}", Security.credentialsNoAuthUser());
                client.Encoding = Encoding.UTF8;
                ServicePointManager.ServerCertificateValidationCallback += (sender, certificate, chain, sslPolicyErrors) => true;
                return op = JsonConvert.DeserializeObject<OutputModel>(client.UploadString(KeyModel.API_URL + path, "PUT", inputJson));
            }
            catch (Exception ex)
            {
                op.Message = ex.ToString();
                op.Status = "error";
                return op;
            }
        }
    }
}