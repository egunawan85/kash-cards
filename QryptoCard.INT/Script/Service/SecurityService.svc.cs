using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography;
using System.ServiceModel;
using System.Text;
using QryptoCard.INT.Model;
using System.IO;
using QryptoCard.Sec;
using Org.BouncyCastle.Asn1.X9;
using QryptoCard.INT.Model.Service;
using Org.BouncyCastle.Ocsp;
using QryptoCard.INT.Script.Gateway.WasabiCard;
using QryptoCard.INT.Model.WasabiCard;

namespace QryptoCard.INT.Script.Service
{
    // NOTE: You can use the "Rename" command on the "Refactor" menu to change the class name "SecurityService" in code, svc and config file together.
    // NOTE: In order to launch WCF Test Client for testing this service, please select SecurityService.svc or SecurityService.svc.cs at the Solution Explorer and start debugging.
    public class SecurityService : ISecurityService
    {

        DBEntities db = new DBEntities();
        OutputModel op = new OutputModel();

        public bool validateUser(string email, string passw)
        {
            try
            {
                passw = Secure.APPtoDB(passw);
                var data = db.tblM_User.Where(p => p.Email == email && p.Password == passw && p.isActive == 1 && p.isVerified == 1 && p.isBanned == 0).FirstOrDefault();
                if (data != null) return true;
                else return false;
            }
            catch (Exception ex) { return false; }
        }
        public bool validateAPI(string api, string sec)
        {
            try
            {
                sec = Secure.APPtoDB(sec);
                var data = db.tblM_User_API.Where(p => p.APIKey == api && p.SecretKey == sec).FirstOrDefault();
                if (data != null) return true;
                else return false;
            }
            catch (Exception ex) { return false; }
        }
        public bool validateAdmin(string email, string passw)
        {
            try
            {
                passw = Secure.APPtoDB(passw);
                var data = db.tblM_Admin.Where(p => p.Email == email && p.Password == passw && p.isActive == 1 && p.isVerified == 1 && p.isBanned == 0).FirstOrDefault();
                if (data != null) return true;
                else return false;
            }
            catch (Exception ex) { return false; }
        }
        //public bool validateAPI(string apikey, string seckey)
        //{
        //    try
        //    {
        //        var data = db.tblM_Company_API.Where(p => p.APIKey == apikey && p.SecretKey == seckey && p.isActive == 1).FirstOrDefault();
        //        if (data != null) return true;
        //        else return false;
        //    }
        //    catch (Exception ex) { return false; }
        //}

        public string base64Encode(string str)
        {
            return Secure.Base64Encode(str);
        }

        public string base64Decode(string str)
        {
            return Secure.Base64Decode(str);
        }

        public string encryptapp(string str)
        {
            return Secure.EncryptAPP(str);
        }

        public string decryptapp(string str)
        {
            return Secure.DecryptAPP(str);
        }

        public string encryptdb(string str)
        {
            return Secure.EncryptDB(str);
        }

        public string decryptdb(string str)
        {
            return Secure.DecryptDB(str);
        }

        public string dbtoapp(string str)
        {
            var x = Secure.DBtoAPP(str);
            return x;
        }

        public string apptodb(string str)
        {
            return Secure.APPtoDB(str);
        }
        public void signRSA()
        {
            string plainTextData = "{}";

            System.Diagnostics.Debug.WriteLine("plainTextData : " + plainTextData);
            string encryData = signData(plainTextData, loadRsaPrivateKeyPem());
            System.Diagnostics.Debug.WriteLine("encryData : " + encryData);
            string DecryptData = Decryption(encryData, loadRsaPrivateKeyPem());
            System.Diagnostics.Debug.WriteLine("DecryptData : " + DecryptData);

        }


        public void decryptRSA(string txt)
        {
            string dec = Decryption(txt, loadRsaPrivateKeyProdPem());
            System.Diagnostics.Debug.WriteLine("decrypt : " + dec);

        }

        public static string signData(string strText, string privateKey)
        {
            var testData = Encoding.UTF8.GetBytes(strText);

            using (var rsa = new RSACryptoServiceProvider(2048))
            {
                try
                {
                    // client encrypting data with public key issued by server                    
                    rsa.FromXmlString(privateKey.ToString());

                    //var encryptedData = rsa.Encrypt(testData, true);
                    var encryptedData = rsa.SignData(testData, new System.Security.Cryptography.SHA256CryptoServiceProvider());

                    var base64Encrypted = Convert.ToBase64String(encryptedData);

                    return base64Encrypted;
                }
                finally
                {
                    rsa.PersistKeyInCsp = false;
                }
            }
        }

        public static string Decryption(string strText, string privateKey)
        {

            var testData = Encoding.UTF8.GetBytes(strText);

            using (var rsa = new RSACryptoServiceProvider(2048))
            {
                try
                {
                    var base64Encrypted = strText;

                    // server decrypting data with private key                    
                    rsa.FromXmlString(privateKey);

                    var resultBytes = Convert.FromBase64String(base64Encrypted);
                    var decryptedBytes = rsa.Decrypt(resultBytes, false);
                    var decryptedData = Encoding.UTF8.GetString(decryptedBytes);
                    return decryptedData.ToString();
                }
                finally
                {
                    rsa.PersistKeyInCsp = false;
                }
            }
        }

        public static string decryptWS(string strText)
        {
            var testData = Encoding.UTF8.GetBytes(strText);

            using (var rsa = new RSACryptoServiceProvider(2048))
            {
                try
                {
                    var base64Encrypted = strText;

                    // server decrypting data with private key                    
                    rsa.FromXmlString(KeyModel.WASABICARD_PRIVATE_KEY_XML);

                    var resultBytes = Convert.FromBase64String(base64Encrypted);
                    var decryptedBytes = rsa.Decrypt(resultBytes, RSAEncryptionPadding.Pkcs1);
                    var decryptedData = Encoding.UTF8.GetString(decryptedBytes);
                    return decryptedData.ToString();
                }
                finally
                {
                    rsa.PersistKeyInCsp = false;
                }
            }
        }

        public void getwb(string x)
        {
            WCCardInfoRequestModel reqq = new WCCardInfoRequestModel();
            reqq.cardNo = x;
            var xxx = WasabiCardService.getCardInfo(reqq);

            WCCardInfoSensitiveRequestModel req = new WCCardInfoSensitiveRequestModel();
            req.cardNo = x;
            var xx = WasabiCardService.getCardInfoSensitive(req);

            var cardno = decryptWS(xx.data.cardNumber);
            var cvv = decryptWS(xx.data.cvv);
            var exp = decryptWS(xx.data.expireDate);
            return;
        }

        public void getcity()
        {
            var xx = WasabiCardService.getCityList();
            return;
        }

        private static string loadRsaPublicKeyPem()
        {
            return "<RSAKeyValue><Modulus>vn+RrVd5OrRdHO+v9nxmL49QF1GBIwpR44HAcb6RtCpnZ8EIDulOJYzsx9VWWYKAAgz4GYi2uK9XgQJNfGBa+3Eell/fVVbgQ67o61sBKZzW+ri7PB7eCnSJSBuec5Y0W4vgAStxglgxDH1D5Am75EIgsxoen2vPNkxyvm4ykPM=</Modulus><Exponent>AQAB</Exponent></RSAKeyValue>";
        }

        private static string loadRsaPrivateKeyPem()
        {
            return QryptoCard.INT.Model.KeyModel.WASABICARD_PRIVATE_KEY_XML;
        }

        private static string loadRsaPrivateKeyProdPem()
        {
            return QryptoCard.INT.Model.KeyModel.WASABICARD_PRIVATE_KEY_XML;
        }
    }
}
