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
                string path = "/v1/auth/login";
                return op = AuthClient.ExecuteJsonPost(path, adm);
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
                string path = "/v1/auth/login/verify";
                return op = AuthClient.ExecuteJsonPost(path, adm);
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
                string path = "/v1/auth/login/resend";
                return op = AuthClient.ExecuteJsonPost(path, adm);
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
                string path = "/v1/auth/password/forgot";
                return op = AuthClient.ExecuteJsonPost(path, adm);
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
                string path = "/v1/auth/password/forgot/check";
                return op = AuthClient.ExecuteJsonPost(path, adm);
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
                string path = "/v1/auth/password/forgot/change";
                return op = AuthClient.ExecuteJsonPost(path, adm);
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
                string path = "/v1/auth/invited/check";
                return op = AuthClient.ExecuteJsonPost(path, adm);
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
                string path = "/v1/auth/invited/onboarding";
                return op = AuthClient.ExecuteJsonPut(path, adm);
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
                string path = "/v1/admin/list";
                return op = AuthClient.ExecuteJsonPost(path, adm);
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
                string path = "/v1/admin/detail";
                return op = AuthClient.ExecuteJsonPost(path, adm);
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
                string path = "/v1/admin/invite";
                return op = AuthClient.ExecuteJsonPost(path, adm);
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
                string path = "/v1/admin/ban";
                return op = AuthClient.ExecuteJsonDelete(path, adm);
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
                string path = "/v1/admin/data/" + id;
                return op = AuthClient.ExecuteJsonGet(path);
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
                string path = "/v1/admin/data";
                return op = AuthClient.ExecuteJsonPut(path, adm);
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
                string path = "/v1/admin/password";
                return op = AuthClient.ExecuteJsonPut(path, adm);
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
                string path = "/v1/admin/email/otp";
                return op = AuthClient.ExecuteJsonPost(path, adm);
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
                string path = "/v1/admin/email";
                return op = AuthClient.ExecuteJsonPut(path, adm);
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