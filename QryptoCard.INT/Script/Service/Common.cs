using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Security;
using QryptoCard.INT.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Web;
using Org.BouncyCastle.OpenSsl;

namespace QryptoCard.INT.Script.Service
{
    public static class Common
    {

        public static string RandomString(int length)
        {
            Random random = new Random();
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            return new string(Enumerable.Repeat(chars, length)
                .Select(s => s[random.Next(s.Length)]).ToArray());
        }

        public static string getOTPCode()
        {
            return "000000";
            //Random r = new Random();
            //var z = r.Next(0, 1000000);
            //if (KeyModel.QRYPTO_ENVIRONMENT == "dev")
            //    return "000000";
            //else
            //    return z.ToString("000000");
        }

        //public static RSACryptoServiceProvider ImportPrivateKey(string pem)
        //{
        //    PemReader pr = new PemReader(new StringReader(pem));
        //    AsymmetricCipherKeyPair KeyPair = (AsymmetricCipherKeyPair)pr.ReadObject();
        //    RSAParameters rsaParams = DotNetUtilities.ToRSAParameters((RsaPrivateCrtKeyParameters)KeyPair.Private);

        //    RSACryptoServiceProvider csp = new RSACryptoServiceProvider();// cspParams);
        //    csp.ImportParameters(rsaParams);
        //    return csp;
        //}

        //public static RSACryptoServiceProvider ImportPublicKey(string pem)
        //{
        //    PemReader pr = new PemReader(new StringReader(pem));
        //    AsymmetricKeyParameter publicKey = (AsymmetricKeyParameter)pr.ReadObject();
        //    RSAParameters rsaParams = DotNetUtilities.ToRSAParameters((RsaKeyParameters)publicKey);

        //    RSACryptoServiceProvider csp = new RSACryptoServiceProvider();// cspParams);
        //    csp.ImportParameters(rsaParams);
        //    return csp;
        //}
    }
}