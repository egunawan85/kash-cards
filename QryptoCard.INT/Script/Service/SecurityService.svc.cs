using System;
using System.Linq;
using QryptoCard.Sec;

namespace QryptoCard.INT.Script.Service
{
    // Credential validation for the API tiers. The former public crypto-oracle
    // operations (base64*/encrypt*/decrypt*/dbtoapp/apptodb/signRSA/decryptRSA/
    // getwb/getcity) had no callers and exposed our DB/RSA keys to anyone who
    // could reach the WCF endpoint; they were removed along with their private
    // RSA helpers. Only the three validators remain.
    public class SecurityService : ISecurityService
    {
        DBEntities db = new DBEntities();

        public bool validateUser(string email, string passw)
        {
            try
            {
                passw = Secure.APPtoDB(passw);
                var data = db.tblM_User.Where(p => p.Email == email && p.Password == passw && p.isActive == 1 && p.isVerified == 1 && p.isBanned == 0).FirstOrDefault();
                return data != null;
            }
            catch (Exception) { return false; }
        }

        public bool validateAPI(string api, string sec)
        {
            try
            {
                sec = Secure.APPtoDB(sec);
                var data = db.tblM_User_API.Where(p => p.APIKey == api && p.SecretKey == sec).FirstOrDefault();
                return data != null;
            }
            catch (Exception) { return false; }
        }

        public bool validateAdmin(string email, string passw)
        {
            try
            {
                passw = Secure.APPtoDB(passw);
                var data = db.tblM_Admin.Where(p => p.Email == email && p.Password == passw && p.isActive == 1 && p.isVerified == 1 && p.isBanned == 0).FirstOrDefault();
                return data != null;
            }
            catch (Exception) { return false; }
        }
    }
}
