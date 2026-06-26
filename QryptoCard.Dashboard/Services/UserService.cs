using QryptoCard.Dashboard.Models.Service;
using QryptoCard.Dashboard.Models;
using System;

namespace QryptoCard.Dashboard.Services
{
    // Every upstream call routes through AuthClient, which attaches the
    // Authorization: Bearer header (from SessionLib), silently refreshes an
    // expired access token, and retries once on a 401. Public/pre-login endpoints
    // (register, login, forgot-password, invited) go through the same path with
    // an empty bearer — harmless, since those endpoints don't require auth.
    public class UserService
    {
        OutputModel op = new OutputModel();

        public OutputModel register(UserModel adm)
        {
            try
            {
                string path = "/v1/auth/register";
                return op = AuthClient.ExecuteJsonPost(path, adm);
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
                string path = "/v1/auth/register/verify";
                return op = AuthClient.ExecuteJsonPost(path, adm);
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
                string path = "/v1/auth/register/resend";
                return op = AuthClient.ExecuteJsonPost(path, adm);
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

        public OutputModel loginVerify(UserAuthOTPModel adm)
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

        public OutputModel resendOTP(UserAuthOTPModel adm)
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

        public OutputModel forgotPassword(UserModel adm)
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

        public OutputModel checkForgotPassword(UserForgotPasswordModel adm)
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

        public OutputModel changeForgotPassword(UserForgotPasswordModel adm)
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

        public OutputModel getUserList(UserFilterModel adm)
        {
            try
            {
                string path = "/v1/user/list";
                return op = AuthClient.ExecuteJsonPost(path, adm);
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
                string path = "/v1/user/detail";
                return op = AuthClient.ExecuteJsonPost(path, adm);
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
                string path = "/v1/user/invite";
                return op = AuthClient.ExecuteJsonPost(path, adm);
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
                string path = "/v1/user/ban";
                return op = AuthClient.ExecuteJsonDelete(path, adm);
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

        public OutputModel onboardingUser(UserModel adm)
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

        public OutputModel getUserData(string id)
        {
            try
            {
                string path = "/v1/user/data/" + id;
                return op = AuthClient.ExecuteJsonGet(path);
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
                string path = "/v1/user/data";
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
                string path = "/v1/user/password";
                return op = AuthClient.ExecuteJsonPut(path, adm);
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
                string path = "/v1/user/email/otp";
                return op = AuthClient.ExecuteJsonPost(path, adm);
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
                string path = "/v1/user/email";
                return op = AuthClient.ExecuteJsonPut(path, adm);
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
                string path = "/v1/user/referral/code";
                return op = AuthClient.ExecuteJsonPost(path, adm);
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
                string path = "/v1/user/balance";
                return op = AuthClient.ExecuteJsonPost(path, adm);
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
                string path = "/v1/user/otp/generate";
                return op = AuthClient.ExecuteJsonGet(path);
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
                string path = "/v1/user/otp/validate";
                return op = AuthClient.ExecuteJsonPost(path, adm);
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
                string path = "/v1/user/dashboard/data";
                return op = AuthClient.ExecuteJsonGet(path);
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
                string path = "/v1/user/referral/joined";
                return op = AuthClient.ExecuteJsonGet(path);
            }
            catch (Exception ex)
            {
                op.Message = ex.ToString();
                op.Status = "error";
                return op;
            }
        }

        // Wallet read surface (Plan 07 prepaid balance). The caller's identity is taken from
        // the bearer token server-side, so no user id is sent — a user can only read their own
        // deposit address / ledger. Deserialize op.Data into DepositAddressModel / LedgerModel.
        public OutputModel getDepositAddress()
        {
            try
            {
                string path = "/v1/user/deposit/address";
                return op = AuthClient.ExecuteJsonGet(path);
            }
            catch (Exception ex)
            {
                op.Message = ex.ToString();
                op.Status = "error";
                return op;
            }
        }

        public OutputModel getLedger(int page = 1, int pageSize = 20)
        {
            try
            {
                string path = "/v1/user/ledger?page=" + page + "&pageSize=" + pageSize;
                return op = AuthClient.ExecuteJsonGet(path);
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
