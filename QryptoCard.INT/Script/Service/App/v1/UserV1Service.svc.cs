using Newtonsoft.Json;
using QryptoCard.INT.Model.Service;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Text;
using QryptoCard.Sec;
using QryptoCard.INT.Script.Gateway.PGCrypto;
using QryptoCard.INT.Model.PGCrypto;
using QryptoCard.INT.Model;

namespace QryptoCard.INT.Script.Service.App.v1
{
    // NOTE: You can use the "Rename" command on the "Refactor" menu to change the class name "UserV1Service" in code, svc and config file together.
    // NOTE: In order to launch WCF Test Client for testing this service, please select UserV1Service.svc or UserV1Service.svc.cs at the Solution Explorer and start debugging.
    public class UserV1Service : IUserV1Service
    {
        DBEntities db = new DBEntities();
        OutputModel op = new OutputModel();

        string getUserId(string em)
        {
            var a = db.tblM_User.Where(p => p.Email == em).FirstOrDefault();
            return a.UserID;
        }
        string getCompanyId(string em)
        {
            var a = db.tblM_User.Where(p => p.Email == em).FirstOrDefault();
            return a.CompanyID;
        }
        //string getRole(string em)
        //{
        //    var a = db.vw_User.Where(p => p.Email == em).FirstOrDefault();
        //    return a.Role;
        //}

        public OutputModel getDashboardData(string em, DashboardModel a)
        {
            try
            {
                var uid = getUserId(em);
                //if (a == null)
                //{
                //    op.Status = "failed";
                //    op.Message = "No valid parameters found";
                //    return op;
                //}

                DashboardModel x = new DashboardModel();
                x.TotalCards = 0;
                x.TotalCommission = 0;
                x.CommissionRate = 0;
                x.TotalTopupTransaction = 0;
                x.AmountSpentThisMonth = 0;

                var crd = db.tblT_Card.Where(p => p.Status == "success" && p.UserID == uid).ToList();
                if (crd.Count > 0)
                    x.TotalCards = crd.Count;

                var comm = db.tblT_Commission.Where(p => p.UserID == uid).ToList();
                if (comm.Count > 0)
                    x.TotalCommission = comm.Sum(p => p.Commission).Value;

                var rate = db.tblM_User_Commission.Where(p => p.UserID == uid).FirstOrDefault();
                if (rate != null)
                    x.CommissionRate = rate.Commission.Value;
                else
                    x.CommissionRate = -1;

                var tp = db.tblT_Card_Deposit.Where(p => p.UserID == uid && p.Status == "success").Count();
                //if (tp.Count() > 0)
                //    x.TotalTopupTransaction = tp.Sum(p => p.ReceivedAmount).Value;

                op.Status = "success";
                op.Message = "Success get dashboard data";
                op.Data = JsonConvert.SerializeObject(x, Formatting.None);
            }
            catch (Exception ex)
            {
                op.Message = ex.Message;
                op.Status = "error";
            }
            return op;
        }

        public OutputModel Register(tblM_User x)
        {
            try
            {
                var data = db.tblM_User.Where(p => p.Email == x.Email && p.isActive == 1 && p.isVerified == 1 && p.isBanned == 0).FirstOrDefault();

                if (data != null)
                {
                    op.Status = "failed";
                    op.Message = "Email address was registered";
                    return op;
                }
                else
                {
                    //tblM_Company c = new tblM_Company();
                    //c.CompanyID = Guid.NewGuid().ToString();
                    //c.Name = x.Profession;
                    //c.isActive = 0;
                    //c.isVerified = 0;
                    //c.isBanned = 0;
                    //c.DateJoin = DateTime.Now;
                    //db.tblM_Company.Add(c);

                    tblM_User u = new tblM_User();
                    u.Password = Secure.APPtoDB(x.Password);
                    u.UserID = Guid.NewGuid().ToString();
                    //u.CompanyID = c.CompanyID;
                    //u.FirstName = x.FirstName;
                    //u.LastName = x.LastName;
                    u.Email = x.Email;
                    u.Phone = x.Phone;
                    u.RoleID = db.tblM_User_Role.Where(p => p.Role == "Owner").Select(p => p.RoleID).FirstOrDefault();
                    u.DateJoin = DateTime.Now;
                    u.isActive = 0;
                    u.isVerified = 0;
                    u.isBanned = 0;

                    // Only resolve a referral code when one was actually supplied. (The old
                    // `!= null || != ""` was a tautology — always true — so an empty code still ran
                    // a pointless lookup; the outcome was the same since a miss sets InvitedBy null.)
                    if (!string.IsNullOrEmpty(x.InvitedBy))
                    {
                        var ck = db.tblM_User_Referral.Where(p => p.Code == x.InvitedBy).FirstOrDefault();
                        if (ck != null)
                            u.InvitedBy = ck.UserID;
                        else
                            u.InvitedBy = null;
                    }

                    db.tblM_User.Add(u);
                    db.SaveChanges();

                    tblH_User_Register a = new tblH_User_Register();
                    a.ID = Guid.NewGuid().ToString();
                    a.UserID = u.UserID;

                    //Random r = new Random();
                    //var z = r.Next(0, 1000000);
                    //string s = String.Empty;
                    //if (KeyModel.QRYPTO_ENVIRONMENT == "dev")
                    //    s = "000000";
                    //else
                    //    s = z.ToString("000000");

                    var code = Common.getOTPCode();
                    a.Code = QryptoCard.Sec.OtpCodes.Hash(code);   // store the hash; the plaintext only goes in the email
                    a.DateCreated = DateTime.Now;
                    a.DateExpired = a.DateCreated.Value.AddMinutes(15);
                    a.isVerify = 0;
                    db.tblH_User_Register.Add(a);
                    db.SaveChanges();

                    NotificationMailkitService.sendEmailOTPRegister(u.Email, u.FirstName + " " + u.LastName, code);

                    op.Status = "success";
                    op.Message = "Success generate OTP";
                    op.Data = a.ID;
                }
            }
            catch (Exception ex)
            {
                op.Message = ex.Message;
                op.Status = "error";
            }
            return op;
        }

        public OutputModel RegisterVerify(tblH_User_Register x)
        {
            try
            {
                var data = db.tblH_User_Register.Where(p => p.ID == x.ID && p.isVerify == 0).FirstOrDefault();

                if (data == null || !QryptoCard.Sec.OtpCodes.Verify(x.Code, data.Code) || QryptoCard.Sec.OtpCodes.IsExpired(data.DateExpired, DateTime.Now))
                {
                    // Count a wrong-code guess against this live session and lock it (isVerify=-1)
                    // at the threshold, so a locked session reads as "not found" on the next try.
                    if (data != null && !QryptoCard.Sec.OtpCodes.Verify(x.Code, data.Code))
                        QryptoCard.INT.Security.OtpLockout.RecordFailure(db.Database, "tblH_User_Register", "ID", data.ID);
                    op.Status = "failed";
                    op.Message = "Your session is ended";
                    return op;
                }
                else
                {
                    data.isVerify = 1;
                    data.Param1 = DateTime.Now.ToString();
                    db.SaveChanges();

                    var u = db.tblM_User.Where(p => p.UserID == data.UserID).FirstOrDefault();
                    u.isActive = 1;
                    u.isVerified = 1;
                    u.isBanned = 0;
                    u.DateVerified = DateTime.Now;
                    
                    db.SaveChanges();

                    // Provisioning (wallet, deposit address, referral, commission) is
                    // decoupled from the verify gate: it runs idempotently and best-effort
                    // here, and self-repairs on later access or a backfill if anything (e.g.
                    // the deposit-address gateway) is transiently unavailable. Verify itself
                    // must always succeed once the OTP is valid — a provisioning hiccup can
                    // never leave a half-provisioned account or report a spurious error.
                    UserProvisioningService.EnsureUserProvisioned(u.UserID);

                    
                    //var c = db.tblM_Company.Where(p => p.CompanyID == u.CompanyID).FirstOrDefault();
                    //c.isVerified = 1;
                    //c.isActive = 1;
                    //c.DateVerified = u.DateVerified;
                    //db.SaveChanges();

                    var user = db.tblM_User.Where(p => p.UserID == data.UserID).FirstOrDefault();
                    op.Status = "success";
                    op.Message = "Success Get User";
                    op.Data = JsonConvert.SerializeObject(user, Formatting.None);
                }
            }
            catch (Exception ex)
            {
                op.Message = ex.Message;
                op.Status = "error";
            }
            return op;
        }

        public OutputModel regenerateOTPRegister(tblH_User_Register x)
        {
            try
            {
                var data = db.tblH_User_Register.Where(p => p.ID == x.ID).FirstOrDefault();

                if (data == null)
                {
                    op.Status = "failed";
                    op.Message = "Your session is ended";
                    return op;
                }
                else
                {
                    // Invalidate any still-pending register-OTP rows for this user before issuing a
                    // new one, so a resend supersedes rather than accumulates live codes.
                    foreach (var stale in db.tblH_User_Register.Where(p => p.UserID == data.UserID && p.isVerify == 0).ToList())
                        stale.isVerify = 1;

                    tblH_User_Register a = new tblH_User_Register();
                    a.ID = Guid.NewGuid().ToString();
                    a.UserID = data.UserID;

                    //Random r = new Random();
                    //var z = r.Next(0, 1000000);
                    //string s = String.Empty;
                    //if (KeyModel.QRYPTO_ENVIRONMENT == "dev")
                    //    s = "000000";
                    //else
                    //    s = z.ToString("000000");

                    var code = Common.getOTPCode();
                    a.Code = QryptoCard.Sec.OtpCodes.Hash(code);
                    a.DateCreated = DateTime.Now;
                    a.DateExpired = a.DateCreated.Value.AddMinutes(15);
                    a.isVerify = 0;
                    db.tblH_User_Register.Add(a);
                    db.SaveChanges();

                    var u = db.tblM_User.Where(p => p.UserID == a.UserID).FirstOrDefault();

                    NotificationMailkitService.sendEmailOTPRegister(u.Email, u.FirstName + " " + u.LastName, code);

                    op.Status = "success";
                    op.Message = "Success generate OTP";
                    op.Data = a.ID;
                }
            }
            catch (Exception ex)
            {
                op.Message = ex.Message;
                op.Status = "error";
            }
            return op;
        }

        public OutputModel Login(tblM_User x)
        {
            try
            {
                var pwd = Secure.APPtoDB(x.Password);
                var data = db.tblM_User.Where(p => p.Email == x.Email && p.isActive == 1 && p.isVerified == 1 && p.isBanned == 0).FirstOrDefault();

                if (data == null)
                {
                    op.Status = "failed";
                    op.Message = "Your email is not registered";
                    return op;
                }
                else
                {
                    DateTime now = DateTime.Now;
                    if (QryptoCard.INT.Security.PasswordLockout.IsLockedOut(db.Database, "tblM_User", "UserID", data.UserID, now))
                    {
                        // Option B: a locked account is indistinguishable from an ordinary wrong password.
                        op.Status = "failed";
                        op.Message = "Your password is incorrect";
                        return op;
                    }

                    if (data.Password != pwd)
                    {
                        QryptoCard.INT.Security.PasswordLockout.RecordFailure(db.Database, "tblM_User", "UserID", data.UserID, now);
                        op.Status = "failed";
                        op.Message = "Your password is incorrect";
                        return op;
                    }

                    QryptoCard.INT.Security.PasswordLockout.RecordSuccess(db.Database, "tblM_User", "UserID", data.UserID);

                    if (data.isActive == 0)
                    {
                        op.Status = "failed";
                        op.Message = "Your account is nonactive. Please call admin for more";
                        return op;
                    }

                    if (data.isBanned == 1)
                    {
                        op.Status = "failed";
                        op.Message = "Your account has been banned for some reason";
                        return op;
                    }

                    tblH_User_Login a = new tblH_User_Login();
                    a.ID = Guid.NewGuid().ToString();
                    a.UserID = data.UserID;

                    //Random r = new Random();
                    //var z = r.Next(0, 1000000);
                    //string s = String.Empty;
                    //if (KeyModel.QRYPTO_ENVIRONMENT == "dev")
                    //    s = "000000";
                    //else
                    //    s = z.ToString("000000");

                    var code = Common.getOTPCode();
                    a.Code = QryptoCard.Sec.OtpCodes.Hash(code);   // store the hash; the plaintext only goes in the email
                    a.DateCreated = DateTime.Now;
                    a.DateExpired = a.DateCreated.Value.AddMinutes(15);
                    a.isVerify = 0;
                    db.tblH_User_Login.Add(a);
                    db.SaveChanges();

                    NotificationMailkitService.sendEmailOTP(data.Email, data.FirstName + " " + data.LastName, code);

                    op.Status = "success";
                    op.Message = "Success generate OTP";
                    op.Data = a.ID;
                }
            }
            catch (Exception ex)
            {
                op.Message = ex.Message;
                op.Status = "error";
            }
            return op;
        }

        public void viewUser(tblM_User x)
        { return; }


        public OutputModel LoginVerify(tblH_User_Login x)
        {
            try
            {
                var data = db.tblH_User_Login.Where(p => p.ID == x.ID && p.isVerify == 0).FirstOrDefault();

                if (data == null || !QryptoCard.Sec.OtpCodes.Verify(x.Code, data.Code) || QryptoCard.Sec.OtpCodes.IsExpired(data.DateExpired, DateTime.Now))
                {
                    // Count a wrong-code guess against this live session and lock it (isVerify=-1)
                    // at the threshold, so a locked session reads as "not found" on the next try.
                    if (data != null && !QryptoCard.Sec.OtpCodes.Verify(x.Code, data.Code))
                        QryptoCard.INT.Security.OtpLockout.RecordFailure(db.Database, "tblH_User_Login", "ID", data.ID);
                    op.Status = "failed";
                    op.Message = "Your session is ended";
                    return op;
                }
                else
                {
                    data.isVerify = 1;
                    data.Param1 = DateTime.Now.ToString();
                    db.SaveChanges();
                    var user = db.tblM_User.Where(p => p.UserID == data.UserID).FirstOrDefault();
                    op.Status = "success";
                    op.Message = "Success Get User";
                    op.Data = JsonConvert.SerializeObject(user, Formatting.None);
                }
            }
            catch (Exception ex)
            {
                op.Message = ex.Message;
                op.Status = "error";
            }
            return op;
        }

        public OutputModel regenerateOTP(tblH_User_Login x)
        {
            try
            {
                var data = db.tblH_User_Login.Where(p => p.ID == x.ID).FirstOrDefault();

                if (data == null)
                {
                    op.Status = "failed";
                    op.Message = "Your session is ended";
                    return op;
                }
                else
                {
                    // Invalidate any still-pending OTP rows for this user before issuing a new one,
                    // so an old un-consumed code cannot remain valid alongside the fresh code — a
                    // resend must supersede, not accumulate, live codes.
                    foreach (var stale in db.tblH_User_Login.Where(p => p.UserID == data.UserID && p.isVerify == 0).ToList())
                        stale.isVerify = 1;

                    tblH_User_Login a = new tblH_User_Login();
                    a.ID = Guid.NewGuid().ToString();
                    a.UserID = data.UserID;

                    //Random r = new Random();
                    //var z = r.Next(0, 1000000);
                    //string s = String.Empty;
                    //if (KeyModel.QRYPTO_ENVIRONMENT == "dev")
                    //    s = "000000";
                    //else
                    //    s = z.ToString("000000");

                    var code = Common.getOTPCode();
                    a.Code = QryptoCard.Sec.OtpCodes.Hash(code);
                    a.DateCreated = DateTime.Now;
                    a.DateExpired = a.DateCreated.Value.AddMinutes(15);
                    a.isVerify = 0;
                    db.tblH_User_Login.Add(a);
                    db.SaveChanges();

                    var u = db.tblM_User.Where(p => p.UserID == a.UserID).FirstOrDefault();
                    NotificationMailkitService.sendEmailOTP(u.Email, u.FirstName + " " + u.LastName, code);

                    op.Status = "success";
                    op.Message = "Success generate OTP";
                    op.Data = a.ID;
                }
            }
            catch (Exception ex)
            {
                op.Message = ex.Message;
                op.Status = "error";
            }
            return op;
        }

        public OutputModel forgotPassword(tblM_User x)
        {
            try
            {
                var data = db.tblM_User.Where(p => p.Email == x.Email).FirstOrDefault();

                if (data == null)
                {
                    op.Status = "failed";
                    op.Message = "Your email is not registered";
                    return op;
                }
                else
                {
                    if (data.isActive == 0)
                    {
                        op.Status = "failed";
                        op.Message = "Your account is nonactive. Please call admin for more";
                        return op;
                    }

                    if (data.isBanned == 1)
                    {
                        op.Status = "failed";
                        op.Message = "Your account has been banned for some reason";
                        return op;
                    }

                    var q = new tblT_User_ForgotPassword();
                    q.UserID = data.UserID;
                    q.CompanyID = data.CompanyID;
                    q.Hash = Guid.NewGuid().ToString();
                    q.isVerified = 0;
                    q.isActive = 1;
                    q.DateCreated = DateTime.Now;
                    db.tblT_User_ForgotPassword.Add(q);
                    db.SaveChanges();

                    var hash = Secure.Base64Encode(q.Hash);

                    NotificationMailkitService.sendEmailPassword(data.Email, data.FirstName + " " + data.LastName, hash);

                    op.Status = "success";
                    op.Message = "Success generate OTP";
                }
            }
            catch (Exception ex)
            {
                op.Message = ex.Message;
                op.Status = "error";
            }
            return op;
        }

        public OutputModel checkForgotPassword(tblT_User_ForgotPassword x)
        {
            try
            {
                var data = db.tblT_User_ForgotPassword.Where(p => p.Hash == x.Hash).FirstOrDefault();

                if (data == null)
                {
                    op.Status = "failed";
                    op.Message = "Your email is not registered";
                    return op;
                }
                else
                {
                    if (data.isVerified == 1)
                    {
                        op.Status = "failed";
                        op.Message = "Your session has been completed";
                        return op;
                    }

                    //var b = db.tblM_User.Where(p => p.UserID == data.UserID).FirstOrDefault();

                    op.Status = "success";
                    op.Message = "Success validate session";
                    //op.Data = JsonConvert.SerializeObject(b, Formatting.None);
                }
            }
            catch (Exception ex)
            {
                op.Message = ex.Message;
                op.Status = "error";
            }
            return op;
        }

        public OutputModel changePassword(tblT_User_ForgotPassword x)
        {
            try
            {
                var data = db.tblT_User_ForgotPassword.Where(p => p.Hash == x.Hash).FirstOrDefault();

                if (data == null)
                {
                    op.Status = "failed";
                    op.Message = "Your email is not registered";
                    return op;
                }
                else
                {
                    if (data.isVerified == 1)
                    {
                        op.Status = "failed";
                        op.Message = "Your session has been completed";
                        return op;
                    }

                    var b = db.tblM_User.Where(p => p.UserID == data.UserID).FirstOrDefault();
                    b.Password = Secure.APPtoDB(x.Param1);
                    data.isVerified = 1;

                    db.SaveChanges();

                    op.Status = "success";
                    op.Message = "Success change password";
                    //op.Data = JsonConvert.SerializeObject(b, Formatting.None);
                }
            }
            catch (Exception ex)
            {
                op.Message = ex.Message;
                op.Status = "error";
            }
            return op;
        }

        public OutputModel getUserData(string em, string x)
        {
            try
            {
                // IDOR fix: the row returned is always the authenticated caller's own
                // record (UserID derived from em); the client-supplied id is ignored so a
                // caller cannot read another user's profile.
                var uid = getUserId(em);
                var data = db.tblM_User.Where(p => p.UserID == uid).FirstOrDefault();

                if (data == null)
                {
                    op.Status = "failed";
                    op.Message = "Your email is not registered";
                    return op;
                }
                else
                {
                    op.Data = JsonConvert.SerializeObject(data, Formatting.None);
                    op.Status = "success";
                    op.Message = "Success get data";
                }
            }
            catch (Exception ex)
            {
                op.Message = ex.Message;
                op.Status = "error";
            }
            return op;
        }

        public OutputModel updateUserData(string em, tblM_User x)
        {
            try
            {
                // IDOR fix: the record updated is the authenticated caller's own (UserID
                // from em), never the client-supplied x.UserID.
                var uid = getUserId(em);
                var data = db.tblM_User.Where(p => p.UserID == uid).FirstOrDefault();

                if (data == null)
                {
                    op.Status = "failed";
                    op.Message = "Your email is not registered";
                    return op;
                }
                else
                {
                    if (data.isActive == 0)
                    {
                        op.Status = "failed";
                        op.Message = "Your account is nonactive. Please call admin for more";
                        return op;
                    }

                    if (data.isBanned == 1)
                    {
                        op.Status = "failed";
                        op.Message = "Your account has been banned for some reason";
                        return op;
                    }

                    data.FirstName = x.FirstName;
                    data.LastName = x.LastName;
                    data.Phone = x.Phone;
                    db.SaveChanges();

                    op.Status = "success";
                    op.Message = "Success update data";
                }
            }
            catch (Exception ex)
            {
                op.Message = ex.Message;
                op.Status = "error";
            }
            return op;
        }

        public OutputModel updatePassword(string em, PasswordChangeModel x)
        {
            try
            {
                // IDOR fix: the password changed is the authenticated caller's own (UserID
                // from em), never the client-supplied x.UserID.
                var uid = getUserId(em);
                var data = db.tblM_User.Where(p => p.UserID == uid).FirstOrDefault();

                if (data == null)
                {
                    op.Status = "failed";
                    op.Message = "Your email is not registered";
                    return op;
                }
                else
                {
                    if (Secure.DBtoAPP(data.Password) != x.CurrentPassword)
                    {
                        op.Status = "failed";
                        op.Message = "Your current password is wrong";
                        return op;
                    }
                    if (data.isActive == 0)
                    {
                        op.Status = "failed";
                        op.Message = "Your account is nonactive. Please call admin for more";
                        return op;
                    }

                    if (data.isBanned == 1)
                    {
                        op.Status = "failed";
                        op.Message = "Your account has been banned for some reason";
                        return op;
                    }

                    data.Password = Secure.APPtoDB(x.Password);
                    db.SaveChanges();

                    op.Status = "success";
                    op.Message = "Success update data";
                }
            }
            catch (Exception ex)
            {
                op.Message = ex.Message;
                op.Status = "error";
            }
            return op;
        }

        public OutputModel updateEmailOTP(string em, tblM_User x)
        {
            try
            {
                // IDOR fix: the change-email OTP is always issued for the authenticated
                // caller (UserID from em), never the client-supplied x.UserID, so a caller
                // cannot start an email change on another account.
                var uid = getUserId(em);

                var q = db.tblM_User.Where(p => p.Email == x.Email && p.isActive == 1 && p.isVerified == 1 && p.isBanned == 0).FirstOrDefault();

                if (q != null)
                {
                    op.Status = "failed";
                    op.Message = "Email address was registered. Please input another email address.";
                    return op;
                }
                else
                {
                    var data = db.tblM_User.Where(p => p.UserID == uid).FirstOrDefault();
                    if (data.isActive == 0)
                    {
                        op.Status = "failed";
                        op.Message = "Your account is nonactive. Please call admin for more";
                        return op;
                    }

                    if (data.isBanned == 1)
                    {
                        op.Status = "failed";
                        op.Message = "Your account has been banned for some reason";
                        return op;
                    }

                    tblH_User_OTP a = new tblH_User_OTP();
                    a.OTPID = Guid.NewGuid().ToString();
                    a.UserID = uid;
                    a.Name = "Change Email";
                    // Bind the intended new address to the OTP record at request time. The confirm
                    // step applies THIS server-stored value, never a client-supplied one, so the
                    // verified code can only ever commit the address the code was issued for.
                    a.MerchantID = x.Email;

                    //Random r = new Random();
                    //var z = r.Next(0, 1000000);
                    //string s = String.Empty;
                    //if (KeyModel.QRYPTO_ENVIRONMENT == "dev")
                    //    s = "000000";
                    //else
                    //    s = z.ToString("000000");

                    var code = Common.getOTPCode();
                    a.Code = QryptoCard.Sec.OtpCodes.Hash(code);
                    a.DateCreated = DateTime.Now;
                    a.DateExpired = a.DateCreated.Value.AddMinutes(15);
                    a.isVerify = 0;
                    db.tblH_User_OTP.Add(a);
                    db.SaveChanges();

                    var u = db.tblM_User.Where(p => p.UserID == a.UserID).FirstOrDefault();
                    NotificationMailkitService.sendEmailOTP(x.Email, u.FirstName + " " + u.LastName, code);

                    op.Status = "success";
                    op.Message = "Success generate OTP";
                    op.Data = a.OTPID;

                }
            }
            catch (Exception ex)
            {
                op.Message = ex.Message;
                op.Status = "error";
            }
            return op;
        }

        public OutputModel updateEmail(string em, tblH_User_OTP x)
        {
            try
            {
                string uid = getUserId(em);

                // Scope to change-email OTPs only. Other flows (e.g. generateKeyOTP / "View Card
                // Detail") also write tblH_User_OTP but leave MerchantID null; without this filter a
                // caller could confirm one of those here and null their own account email.
                var otp = db.tblH_User_OTP.Where(p => p.isVerify == 0 && p.UserID == uid && p.OTPID == x.OTPID && p.Name == "Change Email").OrderByDescending(p => p.DateCreated).FirstOrDefault();
                if (otp == null)
                {
                    op.Status = "failed";
                    op.Message = "OTP Session is not found. Please re-click 'generate key' button";
                    return op;
                }
                else
                {
                    if (!QryptoCard.Sec.OtpCodes.Verify(x.Code, otp.Code) || QryptoCard.Sec.OtpCodes.IsExpired(otp.DateExpired, DateTime.Now))
                    {
                        // Count a wrong-code guess and lock the session (isVerify=-1) at the threshold.
                        if (!QryptoCard.Sec.OtpCodes.Verify(x.Code, otp.Code))
                            QryptoCard.INT.Security.OtpLockout.RecordFailure(db.Database, "tblH_User_OTP", "OTPID", otp.OTPID);
                        op.Status = "failed";
                        op.Message = "Your OTP is wrong";
                        return op;
                    }
                    else
                    {
                        // The new address bound at request time. Guard against a malformed OTP
                        // record (defence in depth) so a missing target can never blank the email.
                        if (string.IsNullOrEmpty(otp.MerchantID))
                        {
                            op.Status = "failed";
                            op.Message = "This verification code is not valid for an email change.";
                            return op;
                        }

                        // Re-assert uniqueness at confirm time: the request-time check leaves a
                        // window (the OTP TTL) in which another user could claim the same address,
                        // which would create duplicate emails and break getUserId's FirstOrDefault.
                        if (db.tblM_User.Any(p => p.Email == otp.MerchantID && p.UserID != uid
                                && p.isActive == 1 && p.isVerified == 1 && p.isBanned == 0))
                        {
                            op.Status = "failed";
                            op.Message = "That email address is no longer available. Please try another.";
                            return op;
                        }

                        otp.isVerify = 1;
                        db.SaveChanges();

                        var data = db.tblM_User.Where(p => p.UserID == uid).FirstOrDefault();
                        // Use the address bound to the OTP at request time, not anything the client
                        // sends now — the code proves control of the address it was issued for.
                        data.Email = otp.MerchantID;
                        db.SaveChanges();

                        op.Status = "success";
                        op.Message = "Success update email address";
                    }
                }
            }
            catch (Exception ex)
            {
                op.Message = ex.Message;
                op.Status = "error";
            }
            return op;
        }

        public OutputModel enable2FA(string em, tblM_User_2FA x)
        {
            try
            {
                string uid = getUserId(em);

                var data = db.tblM_User_2FA.Where(p => p.UserID == uid && p.isActive == 1).FirstOrDefault();
                if (data != null)
                {
                    op.Status = "failed";
                    op.Message = "Two-Factor Authentication was enabled";
                    return op;
                }
                else
                {
                    var user = db.tblM_User.Where(p => p.UserID == uid).FirstOrDefault();
                    user.is2FA = 1;
                    user.Date2FA = DateTime.Now;
                    db.SaveChanges();

                    x.UserID = uid;
                    x.AccountKey = Secure.APPtoDB(x.AccountKey);
                    x.ManualEntryKey = Secure.APPtoDB(x.ManualEntryKey);
                    x.DateCreated = user.Date2FA;
                    x.isActive = 1;
                    db.tblM_User_2FA.Add(x);
                    db.SaveChanges();

                    op.Status = "success";
                    op.Message = "Two-Factor Authentication activated";
                }
            }
            catch (Exception ex)
            {
                op.Message = ex.Message;
                op.Status = "error";
            }
            return op;
        }

        public OutputModel get2FA(string em)
        {
            try
            {
                string uid = getUserId(em);

                var data = db.tblM_User_2FA.Where(p => p.UserID == uid && p.isActive == 1).FirstOrDefault();
                if (data == null)
                {
                    op.Status = "failed";
                    op.Message = "Data not found";
                    return op;
                }
                else
                {
                    data.AccountKey = Secure.DBtoAPP(data.AccountKey);
                    data.ManualEntryKey = Secure.DBtoAPP(data.ManualEntryKey);
                    op.Data = JsonConvert.SerializeObject(data, Formatting.None);
                    op.Status = "success";
                    op.Message = "Success retrieve data";
                }
            }
            catch (Exception ex)
            {
                op.Message = ex.Message;
                op.Status = "error";
            }
            return op;
        }

        public OutputModel getReferralCode(string em, tblM_User_Referral x)
        {
            try
            {

                var uid = getUserId(em);
                var data = db.tblM_User_Referral.Where(p => p.UserID == uid).FirstOrDefault();

                if (data == null)
                {
                    op.Status = "failed";
                    op.Message = "No referral code";
                    return op;
                }
                else
                {
                    op.Data = JsonConvert.SerializeObject(data, Formatting.None);
                    op.Status = "success";
                    op.Message = "Success get data";
                }
            }
            catch (Exception ex)
            {
                op.Message = ex.Message;
                op.Status = "error";
            }
            return op;
        }

        public OutputModel getBalance(string em, tblM_User_Balance x)
        {
            try
            {

                var uid = getUserId(em);
                // Read the live wallet through the shared accessor, lazily provisioning a
                // row for users who predate the wallet feature or whose creation failed.
                var data = WalletService.EnsureWallet(uid);

                if (data == null)
                {
                    op.Status = "failed";
                    op.Message = "No user balance";
                    return op;
                }
                else
                {
                    op.Data = JsonConvert.SerializeObject(data, Formatting.None);
                    op.Status = "success";
                    op.Message = "Success get data";
                }
            }
            catch (Exception ex)
            {
                op.Message = ex.Message;
                op.Status = "error";
            }
            return op;
        }

        // Return the authenticated user's static deposit address (+ network/coin for a QR
        // payload), provisioning one on first view. Strictly scoped to the caller's own user
        // (derived from the bearer identity, never a body-supplied id) to prevent IDOR.
        public OutputModel getDepositAddress(string em)
        {
            try
            {
                var uid = getUserId(em);
                var addr = WalletService.EnsureDepositAddress(uid);
                if (addr == null)
                {
                    op.Status = "failed";
                    op.Message = "No deposit address available";
                    return op;
                }
                op.Data = JsonConvert.SerializeObject(new
                {
                    Address = addr.Address,
                    NetworkID = addr.NetworkID,
                    Network = "TRC20",
                    Coin = "USDT"
                }, Formatting.None);
                op.Status = "success";
                op.Message = "Success get deposit address";
            }
            catch (Exception ex)
            {
                op.Message = ex.Message;
                op.Status = "error";
            }
            return op;
        }

        // Paginated prepaid-balance ledger for the authenticated user. Scoped to the caller's
        // own user (IDOR-safe); the internal surrogate ID and BalanceID are not exposed.
        public OutputModel getLedger(string em, int page, int pageSize)
        {
            try
            {
                var uid = getUserId(em);
                if (page < 1) page = 1;
                if (pageSize < 1 || pageSize > 100) pageSize = 20;

                var q = db.tblH_User_Balance.Where(p => p.UserID == uid);
                var total = q.Count();
                var rows = q.OrderByDescending(p => p.ID)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(p => new
                    {
                        p.Type,
                        p.Amount,
                        p.Commision,
                        p.BalancePrevious,
                        p.Balance,
                        p.TransactionID,
                        p.Status,
                        p.CreatedDate
                    })
                    .ToList();

                op.Data = JsonConvert.SerializeObject(new
                {
                    Page = page,
                    PageSize = pageSize,
                    Total = total,
                    Items = rows
                }, Formatting.None);
                op.Status = "success";
                op.Message = "Success get ledger";
            }
            catch (Exception ex)
            {
                op.Message = ex.Message;
                op.Status = "error";
            }
            return op;
        }


        public OutputModel generateKeyOTP(string em)
        {
            try
            {
                string uid = getUserId(em);


                tblH_User_OTP a = new tblH_User_OTP();
                a.OTPID = Guid.NewGuid().ToString();
                a.UserID = uid;
                a.Name = "View Card Detail";

                //Random r = new Random();
                //var z = r.Next(0, 1000000);
                //string s = String.Empty;
                //if (KeyModel.QRYPTO_ENVIRONMENT == "dev")
                //    s = "000000";
                //else
                //    s = z.ToString("000000");

                var code = Common.getOTPCode();
                a.Code = QryptoCard.Sec.OtpCodes.Hash(code);
                a.DateCreated = DateTime.Now;
                a.DateExpired = a.DateCreated.Value.AddMinutes(15);
                a.isVerify = 0;
                db.tblH_User_OTP.Add(a);
                db.SaveChanges();

                var u = db.tblM_User.Where(p => p.UserID == a.UserID).FirstOrDefault();
                NotificationMailkitService.sendEmailOTP(u.Email, u.FirstName + " " + u.LastName, code);

                op.Status = "success";
                op.Message = "Success generate OTP";
                op.Data = a.OTPID;
            }
            catch (Exception ex)
            {
                op.Message = ex.Message;
                op.Status = "error";
            }
            return op;
        }

        public OutputModel validateKeyOTP(string em, tblH_User_OTP x)
        {
            try
            {
                string uid = getUserId(em);

                var otp = db.tblH_User_OTP.Where(p => p.isVerify == 0 && p.OTPID == x.OTPID && p.UserID == uid).FirstOrDefault();
                if (otp == null)
                {
                    op.Status = "failed";
                    op.Message = "OTP Session is not found. Please re-click 'generate key' button";
                    return op;
                }
                else
                {
                    if (!QryptoCard.Sec.OtpCodes.Verify(x.Code, otp.Code) || QryptoCard.Sec.OtpCodes.IsExpired(otp.DateExpired, DateTime.Now))
                    {
                        // Count a wrong-code guess and lock the session (isVerify=-1) at the threshold.
                        if (!QryptoCard.Sec.OtpCodes.Verify(x.Code, otp.Code))
                            QryptoCard.INT.Security.OtpLockout.RecordFailure(db.Database, "tblH_User_OTP", "OTPID", otp.OTPID);
                        op.Status = "failed";
                        op.Message = "Your OTP is wrong";
                        return op;
                    }
                    else
                    {
                        otp.isVerify = 1;
                        db.SaveChanges();

                        op.Status = "success";
                        op.Message = "Success validate Key";
                    }
                }
            }
            catch (Exception ex)
            {
                op.Message = ex.Message;
                op.Status = "error";
            }
            return op;
        }


        // Per-referral breakdown for the authenticated caller: each user they referred, with the
        // total commission that referee has earned the caller, and whether they've converted (bought
        // a card). Read-only; scoped to the caller's own uid (getUserId(em)) — never a client-supplied
        // id, so a caller only ever sees their own referees. Earnings come from tblT_Commission,
        // joined back to the referee via the order id (tblT_Card / tblT_Card_Deposit). Kept on the
        // existing /referral/joined contract (the loosely-typed OutputModel.Data carries the richer
        // shape) so no WCF service-reference regen is needed; the referrals tab is the only caller.
        public OutputModel getReferralJoined(string em)
        {
            try
            {
                var uid = getUserId(em);

                // Referees (users this caller invited).
                var referees = db.tblM_User
                    .Where(p => p.InvitedBy == uid)
                    .Select(p => new { p.UserID, p.FirstName, p.LastName, p.DateJoin })
                    .ToList();

                // Commission this caller earned, attributed to the referee whose order generated it:
                // tblT_Commission.TransactionID -> the referee's card / deposit order -> that order's
                // UserID. Explicit joins (EF-translatable) rather than a correlated subquery.
                var earned = (from t in db.tblT_Commission
                              where t.UserID == uid
                              join c in db.tblT_Card on t.TransactionID equals c.ID
                              select new { RefereeId = c.UserID, t.Commission })
                             .Concat(from t in db.tblT_Commission
                                     where t.UserID == uid
                                     join d in db.tblT_Card_Deposit on t.TransactionID equals d.ID
                                     select new { RefereeId = d.UserID, t.Commission })
                             .GroupBy(x => x.RefereeId)
                             .Select(g => new { RefereeId = g.Key, Total = g.Sum(x => (double?)x.Commission) ?? 0.0 })
                             .ToList();

                // Referees who have bought at least one card (= converted).
                var refIds = referees.Select(r => r.UserID).ToList();
                var converted = db.tblT_Card
                    .Where(c => refIds.Contains(c.UserID))
                    .Select(c => c.UserID).Distinct().ToList();

                var earnedMap = earned.ToDictionary(e => e.RefereeId, e => e.Total);
                var convertedSet = new System.Collections.Generic.HashSet<string>(converted);

                var rows = referees.Select(r => new
                {
                    r.UserID,
                    r.FirstName,
                    r.LastName,
                    r.DateJoin,
                    Earned = earnedMap.ContainsKey(r.UserID) ? earnedMap[r.UserID] : 0.0,
                    Converted = convertedSet.Contains(r.UserID)
                }).ToList();

                op.Status = "success";
                op.Message = "Success get referral breakdown";
                op.Data = JsonConvert.SerializeObject(rows, Formatting.None);
            }
            catch (Exception ex)
            {
                op.Message = ex.Message;
                op.Status = "error";
            }
            return op;
        }
    }
}
