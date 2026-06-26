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
            // Use a CSPRNG, not System.Random. A new time-seeded Random() per call returns the
            // SAME string for calls within one ~15ms tick, which collides callers that must be
            // unique (e.g. the deposit Address has a unique index — colliding values fail the
            // insert and, under that path, can leave provisioning empty). The OS CSPRNG is
            // re-seeded from entropy each call, so successive results don't collide.
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            var result = new char[length];
            var bytes = new byte[length * 4];
            using (var rng = RandomNumberGenerator.Create())
                rng.GetBytes(bytes);
            for (int i = 0; i < length; i++)
            {
                uint val = BitConverter.ToUInt32(bytes, i * 4);
                result[i] = chars[(int)(val % (uint)chars.Length)];
            }
            return new string(result);
        }

        public static string getOTPCode()
        {
            // Real CSPRNG one-time code (was hardcoded "000000", with a dev bypass also to "000000").
            return QryptoCard.Sec.OtpCodes.Generate(6);
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