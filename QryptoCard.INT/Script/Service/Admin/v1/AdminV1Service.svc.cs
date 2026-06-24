using Newtonsoft.Json;
using QryptoCard.INT.Model;
using QryptoCard.INT.Model.Service;
using QryptoCard.Sec;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Text;

namespace QryptoCard.INT.Script.Service.Admin.v1
{
    // NOTE: You can use the "Rename" command on the "Refactor" menu to change the class name "AdminV1Service" in code, svc and config file together.
    // NOTE: In order to launch WCF Test Client for testing this service, please select AdminV1Service.svc or AdminV1Service.svc.cs at the Solution Explorer and start debugging.
    public class AdminV1Service : IAdminV1Service
    {
        DBEntities db = new DBEntities();
        OutputModel op = new OutputModel();

        string getAdminId(string em)
        {
            var a = db.tblM_Admin.Where(p => p.Email == em).FirstOrDefault();
            return a.AdminID;
        }

        string getRole(string em)
        {
            var a = db.vw_Admin.Where(p => p.Email == em).FirstOrDefault();
            return a == null ? null : a.Role;
        }

        // Allowlist: ONLY Owner/Admin may read/act on other admins' records.
        // Deny-by-default (case/whitespace-insensitive) so an unknown, null, or variant
        // role string can't slip through.
        bool isDeniedAdminManage(string em)
        {
            var role = (getRole(em) ?? "").Trim();
            return !(role.Equals(RoleModel.Owner, StringComparison.OrdinalIgnoreCase)
                  || role.Equals(RoleModel.Admin, StringComparison.OrdinalIgnoreCase));
        }

        public OutputModel Login(tblM_Admin x)
        {
            try
            {
                var pwd = Secure.APPtoDB(x.Password);
                var data = db.tblM_Admin.Where(p => p.Email == x.Email).FirstOrDefault();

                if (data == null)
                {
                    op.Status = "failed";
                    op.Message = "Your email is not registered";
                    return op;
                }
                else
                {
                    if (data.Password != pwd)
                    {
                        op.Status = "failed";
                        op.Message = "Your password is incorrect";
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

                    tblH_Admin_Login a = new tblH_Admin_Login();
                    a.ID = Guid.NewGuid().ToString();
                    a.AdminID = data.AdminID;

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
                    db.tblH_Admin_Login.Add(a);
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

        public void viewAdmin(vw_Admin x)
        { return; }


        public OutputModel LoginVerify(tblH_Admin_Login x)
        {
            try
            {
                var data = db.tblH_Admin_Login.Where(p => p.ID == x.ID && p.isVerify == 0).FirstOrDefault();

                if (data == null || !QryptoCard.Sec.OtpCodes.Verify(x.Code, data.Code) || QryptoCard.Sec.OtpCodes.IsExpired(data.DateExpired, DateTime.Now))
                {
                    op.Status = "failed";
                    op.Message = "Your session is ended";
                    return op;
                }
                else
                {
                    data.isVerify = 1;
                    data.Param1 = DateTime.Now.ToString();
                    db.SaveChanges();
                    var adm = db.vw_Admin.Where(p => p.AdminID == data.AdminID).FirstOrDefault();
                    op.Status = "success";
                    op.Message = "Success Get Admin";
                    op.Data = JsonConvert.SerializeObject(adm, Formatting.None);
                }
            }
            catch (Exception ex)
            {
                op.Message = ex.Message;
                op.Status = "error";
            }
            return op;
        }

        public OutputModel regenerateOTP(tblH_Admin_Login x)
        {
            try
            {
                var data = db.tblH_Admin_Login.Where(p => p.ID == x.ID).FirstOrDefault();

                if (data == null)
                {
                    op.Status = "failed";
                    op.Message = "Your session is ended";
                    return op;
                }
                else
                {
                    // Invalidate any still-pending OTP rows for this admin before issuing a new one,
                    // so a resend supersedes rather than accumulates live codes.
                    foreach (var stale in db.tblH_Admin_Login.Where(p => p.AdminID == data.AdminID && p.isVerify == 0).ToList())
                        stale.isVerify = 1;

                    tblH_Admin_Login a = new tblH_Admin_Login();
                    a.ID = Guid.NewGuid().ToString();
                    a.AdminID = data.AdminID;

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
                    db.tblH_Admin_Login.Add(a);
                    db.SaveChanges();
                    var adm = db.tblM_Admin.Where(p => p.AdminID == a.AdminID).FirstOrDefault();
                    NotificationMailkitService.sendEmailOTP(adm.Email, adm.FirstName + " " + adm.LastName, code);

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

        public OutputModel forgotPassword(tblM_Admin x)
        {
            try
            {
                var data = db.tblM_Admin.Where(p => p.Email == x.Email).FirstOrDefault();

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

                    var q = new tblT_Admin_ForgotPassword();
                    q.AdminID = data.AdminID;
                    q.Hash = Guid.NewGuid().ToString();
                    q.isVerified = 0;
                    q.isActive = 1;
                    q.DateCreated = DateTime.Now;
                    db.tblT_Admin_ForgotPassword.Add(q);
                    db.SaveChanges();

                    var hash = Secure.Base64Encode(q.Hash);

                    NotificationService.sendEmailPasswordAdmin(data.Email, data.FirstName + " " + data.LastName, hash);

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

        public OutputModel checkForgotPassword(tblT_Admin_ForgotPassword x)
        {
            try
            {
                var data = db.tblT_Admin_ForgotPassword.Where(p => p.Hash == x.Hash).FirstOrDefault();

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

                    //var b = db.tblM_Admin.Where(p => p.AdminID == data.AdminID).FirstOrDefault();

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

        public OutputModel changePassword(tblT_Admin_ForgotPassword x)
        {
            try
            {
                var data = db.tblT_Admin_ForgotPassword.Where(p => p.Hash == x.Hash).FirstOrDefault();

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

                    var b = db.tblM_Admin.Where(p => p.AdminID == data.AdminID).FirstOrDefault();
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

        public OutputModel getAdminFilter(string em, AdminFilterModel fil)
        {
            try
            {
                string uid = getAdminId(em);
                string role = getRole(em);

                if (isDeniedAdminManage(em))
                {
                    op.Status = "failed";
                    op.Message = "You are not authorize to run this endpoint";
                    return op;
                }

                var data = db.vw_Admin.Where(p => p.isActive == 1 && p.isBanned == 0).OrderBy(p => p.DateJoin).ToList();
                if (fil.isVerified)
                    data = data.Where(p => p.isVerified == 1).ToList();
                if (fil.Role != "all")
                {
                    data = data.Where(p => p.Role == fil.Role).ToList();
                }

                op.Status = "success";
                op.Message = "Success get admin list";
                op.Data = JsonConvert.SerializeObject(data, Formatting.None);
            }
            catch (Exception ex)
            {
                op.Message = ex.Message;
                op.Status = "error";
            }
            return op;
        }

        public OutputModel getAdminDetail(string em, tblM_Admin fil)
        {
            try
            {
                // Allow-list role gate: only Owner/Admin may view another admin's detail.
                if (isDeniedAdminManage(em))
                {
                    op.Status = "failed";
                    op.Message = "You are not authorize to run this endpoint";
                    return op;
                }

                var data = db.vw_Admin.Where(p => p.AdminID == fil.AdminID).FirstOrDefault();
                if (data != null)
                {
                    op.Status = "success";
                    op.Message = "Success get admin detail";
                    op.Data = JsonConvert.SerializeObject(data, Formatting.None);
                }
                else
                {
                    op.Status = "failed";
                    op.Message = "Failed get admin detail";
                }
            }
            catch (Exception ex)
            {
                op.Message = ex.Message;
                op.Status = "error";
            }
            return op;
        }

        public OutputModel addAdmin(string em, tblM_Admin x)
        {
            try
            {
                string uid = getAdminId(em);
                string role = getRole(em);

                if (isDeniedAdminManage(em))
                {
                    op.Status = "failed";
                    op.Message = "You are not authorize to run this endpoint";
                    return op;
                }

                var data = db.tblM_Admin.Where(p => p.Email == x.Email && p.isActive == 1).FirstOrDefault();
                if (data != null)
                {
                    op.Status = "failed";
                    op.Message = "Email address registered. Please choose another email address.";
                    return op;
                }

                var rl = db.tblM_Admin_Role.Where(p => p.Role == x.Password).FirstOrDefault();
                x.RoleID = rl.RoleID;

                x.DateInvited = DateTime.Now;
                x.InvitedBy = uid;
                x.AdminID = Guid.NewGuid().ToString();
                x.isBanned = 0;
                x.isVerified = 0;
                x.isActive = 1;

                db.tblM_Admin.Add(x);
                db.SaveChanges();

                var id = x.AdminID;
                var b = db.vw_Admin.Where(p => p.AdminID == id && p.isActive == 1).FirstOrDefault();

                var hash = Secure.Base64Encode(x.AdminID);
                var url = "https://admin-dev.qrypto.trade:88/InvitedAccount?id=" + hash;
                NotificationService.sendEmailAdminInvitation(b.Email, b.InvitedByFirstName + " " + b.InvitedByLastName, x.FirstName + " " + x.LastName, url);

                op.Status = "success";
                op.Message = "Success invite admin. Please ask admin to check email to verify his/her account.";
                //op.Data = JsonConvert.SerializeObject(data, Formatting.None);
            }
            //catch (DbEntityValidationException e)
            //{
            //    string str = "";
            //    foreach (var eve in e.EntityValidationErrors)
            //    {
            //        Console.WriteLine("Entity of type \"{0}\" in state \"{1}\" has the following validation errors:",
            //            eve.Entry.Entity.GetType().Name, eve.Entry.State);
            //        foreach (var ve in eve.ValidationErrors)
            //        {
            //            Console.WriteLine("- Property: \"{0}\", Error: \"{1}\"",
            //                ve.PropertyName, ve.ErrorMessage);
            //        }
            //    }
            //    op.Data = null;
            //    op.Status = "error";
            //    throw;
            //}
            catch (Exception ex)
            {
                op.Message = ex.Message;
                op.Status = "error";
            }
            return op;
        }

        public OutputModel getInvitedAdmin(tblM_Admin x)
        {
            try
            {
                var data = db.vw_Admin.Where(p => p.AdminID == x.AdminID).FirstOrDefault();

                if (data == null)
                {
                    op.Status = "failed";
                    op.Message = "Your session is not found";
                    return op;
                }
                else
                {
                    if (data.isVerified == 1)
                    {
                        op.Status = "failed";
                        op.Message = "Your admin has been completed. Please go to login page.";
                        return op;
                    }


                    op.Status = "success";
                    op.Message = "Success get admin detail";
                    op.Data = JsonConvert.SerializeObject(data, Formatting.None);
                }
            }
            catch (Exception ex)
            {
                op.Message = ex.Message;
                op.Status = "error";
            }
            return op;
        }

        public OutputModel updateInvitedAdmin(tblM_Admin x)
        {
            try
            {
                var data = db.tblM_Admin.Where(p => p.AdminID == x.AdminID).FirstOrDefault();

                if (data == null)
                {
                    op.Status = "failed";
                    op.Message = "Your session is not found";
                    return op;
                }
                else
                {
                    if (data.isVerified == 1)
                    {
                        op.Status = "failed";
                        op.Message = "Your admin has been completed. Please go to login page.";
                        return op;
                    }

                    data.Password = Secure.APPtoDB(x.Password);
                    data.Phone = x.Phone;
                    data.isActive = 1;
                    data.isVerified = 1;
                    data.isBanned = 0;
                    data.DateVerified = DateTime.Now;
                    data.DateJoin = data.DateVerified;
                    db.SaveChanges();


                    op.Status = "success";
                    op.Message = "Onboarding completed. Please login.";
                    //op.Data = JsonConvert.SerializeObject(data, Formatting.None);
                }
            }
            catch (Exception ex)
            {
                op.Message = ex.Message;
                op.Status = "error";
            }
            return op;
        }

        public OutputModel banAdmin(string em, tblM_Admin x)
        {
            try
            {
                string uid = getAdminId(em);
                string role = getRole(em);

                if (isDeniedAdminManage(em))
                {
                    op.Status = "failed";
                    op.Message = "You are not authorize to run this endpoint";
                    return op;
                }

                var data = db.tblM_Admin.Where(p => p.AdminID == x.AdminID).FirstOrDefault();
                if (data == null)
                {
                    op.Status = "failed";
                    op.Message = "Admin ID is not found.";
                    return op;
                }

                if (data.isBanned == 1)
                {
                    op.Status = "failed";
                    op.Message = "This admin already banned.";
                    return op;
                }

                // Bug fix: mutate the loaded entity (data), not the inbound wire object (x),
                // and actually ban (isBanned = 1) rather than clearing the flag.
                data.isBanned = 1;
                data.BannedBy = uid;
                data.DateBanned = DateTime.Now;
                db.SaveChanges();

                op.Status = "success";
                op.Message = "Success banned admin";
                //op.Data = JsonConvert.SerializeObject(data, Formatting.None);
            }
            catch (Exception ex)
            {
                op.Message = ex.Message;
                op.Status = "error";
            }
            return op;
        }

        public OutputModel getAdminData(string em, string x)
        {
            try
            {
                // IDOR fix: only ever return the authenticated caller's own admin record
                // (AdminID derived from em); the client-supplied id is ignored so a caller
                // cannot read another admin's data.
                string uid = getAdminId(em);
                var data = db.vw_Admin.Where(p => p.AdminID == uid).FirstOrDefault();

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

        public OutputModel updateAdminData(string em, tblM_Admin x)
        {
            try
            {
                // IDOR fix: the record updated is the authenticated caller's own (AdminID
                // from em), never the client-supplied x.AdminID.
                string uid = getAdminId(em);
                var data = db.tblM_Admin.Where(p => p.AdminID == uid).FirstOrDefault();

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
                // IDOR fix: the password changed is the authenticated caller's own (AdminID
                // from em), never the client-supplied x.AdminID.
                string uid = getAdminId(em);
                var data = db.tblM_Admin.Where(p => p.AdminID == uid).FirstOrDefault();

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

        public OutputModel updateEmailOTP(string em, tblM_Admin x)
        {
            try
            {
                // IDOR fix: the change-email OTP is always issued for the authenticated
                // caller (AdminID from em), never the client-supplied x.AdminID.
                string uid = getAdminId(em);

                var q = db.tblM_Admin.Where(p => p.Email == x.Email && p.isActive == 1 && p.isVerified == 1 && p.isBanned == 0).FirstOrDefault();

                if (q != null)
                {
                    op.Status = "failed";
                    op.Message = "Email address was registered. Please input another email address.";
                    return op;
                }
                else
                {
                    var data = db.tblM_Admin.Where(p => p.AdminID == uid).FirstOrDefault();
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

                    tblH_Admin_OTP a = new tblH_Admin_OTP();
                    a.OTPID = Guid.NewGuid().ToString();
                    a.AdminID = uid;
                    a.Name = "Change Email";

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
                    db.tblH_Admin_OTP.Add(a);
                    db.SaveChanges();

                    var u = db.tblM_Admin.Where(p => p.AdminID == a.AdminID).FirstOrDefault();
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

        public OutputModel updateEmail(string em, tblH_Admin_OTP x)
        {
            try
            {
                string uid = getAdminId(em);

                var otp = db.tblH_Admin_OTP.Where(p => p.isVerify == 0 && p.AdminID == uid && p.OTPID == x.OTPID).OrderByDescending(p => p.DateCreated).FirstOrDefault();
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
                        op.Status = "failed";
                        op.Message = "Your OTP is wrong";
                        return op;
                    }
                    else
                    {
                        otp.isVerify = 1;
                        db.SaveChanges();

                        var data = db.tblM_Admin.Where(p => p.AdminID == uid).FirstOrDefault();
                        data.Email = x.MerchantID;
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
    }
}
