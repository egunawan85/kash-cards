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
                // Query by email alone — the password is never in the WHERE clause, so
                // there is no SQL-equality oracle, and bcrypt verification replaces the
                // old ciphertext-equality compare. VerifyWithUniformTiming runs a dummy
                // bcrypt on an account miss so latency doesn't reveal account existence.
                var data = db.tblM_User.Where(p => p.Email == email && p.isActive == 1 && p.isVerified == 1 && p.isBanned == 0).FirstOrDefault();
                return QryptoCard.INT.Security.PasswordHasher.VerifyWithUniformTiming(passw, data?.Password);
            }
            catch (Exception) { return false; }
        }

        public bool validateAPI(string api, string sec)
        {
            try
            {
                // APIKey is the lookup handle; the secret is stored as a bcrypt hash, so
                // look up by key then verify the secret (it can't be matched by equality).
                var data = db.tblM_User_API.Where(p => p.APIKey == api).FirstOrDefault();
                return QryptoCard.INT.Security.PasswordHasher.VerifyWithUniformTiming(sec, data?.SecretKey);
            }
            catch (Exception) { return false; }
        }

        public bool validateAdmin(string email, string passw)
        {
            try
            {
                var data = db.tblM_Admin.Where(p => p.Email == email && p.isActive == 1 && p.isVerified == 1 && p.isBanned == 0).FirstOrDefault();
                return QryptoCard.INT.Security.PasswordHasher.VerifyWithUniformTiming(passw, data?.Password);
            }
            catch (Exception) { return false; }
        }
    }
}
