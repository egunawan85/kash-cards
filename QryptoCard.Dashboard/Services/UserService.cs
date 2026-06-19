using Newtonsoft.Json;
using QryptoCard.Dashboard.Models.Service;
using QryptoCard.Dashboard.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Web;

namespace QryptoCard.Dashboard.Services
{
    public class UserService
    {
        OutputModel op = new OutputModel();

        public OutputModel register(UserModel adm)
        {
            try
            {
                Common.trustConnection();
                string path = "/v1/auth/register";
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

        public OutputModel registerVerify(UserAuthOTPModel adm)
        {
            try
            {
                Common.trustConnection();
                string path = "/v1/auth/register/verify";
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

        public OutputModel resendOTPRegister(UserAuthOTPModel adm)
        {
            try
            {
                Common.trustConnection();
                string path = "/v1/auth/register/resend";
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

        public OutputModel login(UserModel adm)
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

        public OutputModel loginVerify(UserAuthOTPModel adm)
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

        public OutputModel resendOTP(UserAuthOTPModel adm)
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

        public OutputModel forgotPassword(UserModel adm)
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

        public OutputModel checkForgotPassword(UserForgotPasswordModel adm)
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

        public OutputModel changeForgotPassword(UserForgotPasswordModel adm)
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

        public OutputModel getUserList(UserFilterModel adm)
        {
            try
            {
                Common.trustConnection();
                string path = "/v1/user/list";
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

        public OutputModel getUserDetail(UserModel adm)
        {
            try
            {
                Common.trustConnection();
                string path = "/v1/user/detail";
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

        public OutputModel inviteUser(UserModel adm)
        {
            try
            {
                Common.trustConnection();
                string path = "/v1/user/invite";
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

        public OutputModel banUser(UserModel adm)
        {
            try
            {
                Common.trustConnection();
                string path = "/v1/user/ban";
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
        public OutputModel getInvitedUser(UserModel adm)
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
        public OutputModel onboardingUser(UserModel adm)
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

        public OutputModel getUserData(string id)
        {
            try
            {
                Common.trustConnection();
                string path = "/v1/user/data/" + id;
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

        public OutputModel updateUserData(UserModel adm)
        {
            try
            {
                Common.trustConnection();
                string path = "/v1/user/data";
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
                string path = "/v1/user/password";
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

        public OutputModel updateEmailOTP(UserModel adm)
        {
            try
            {
                Common.trustConnection();
                string path = "/v1/user/email/otp";
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

        public OutputModel updateEmail(UserOTPModel adm)
        {
            try
            {
                Common.trustConnection();
                string path = "/v1/user/email";
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

        public OutputModel getReferralCode(UserReferralModel adm)
        {
            try
            {
                Common.trustConnection();
                string path = "/v1/user/referral/code";
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

        public OutputModel getBalance(UserBalanceModel adm)
        {
            try
            {
                Common.trustConnection();
                string path = "/v1/user/balance";
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

        public OutputModel generateOTP()
        {
            try
            {
                Common.trustConnection();
                string path = "/v1/user/otp/generate";
                WebClient client = new WebClient();
                client.Headers["Content-type"] = "application/json";
                //string inputJson = JsonConvert.SerializeObject(adm);
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

        public OutputModel validateOTP(UserOTPModel adm)
        {
            try
            {
                Common.trustConnection();
                string path = "/v1/user/otp/validate";
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

        public OutputModel getDashboardData()
        {
            try
            {
                Common.trustConnection();
                string path = "/v1/user/dashboard/data";
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

        public OutputModel getReferralJoined()
        {
            try
            {
                Common.trustConnection();
                string path = "/v1/user/referral/joined";
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
    }
}