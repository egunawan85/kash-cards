using QryptoCard.Dashboard.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Web;

namespace QryptoCard.Dashboard.Services
{
    public class Security
    {
        public static string credentialsNoAuthAdmin()
        {
            return Convert.ToBase64String(Encoding.ASCII.GetBytes(KeyModel.USER_EMAIL + ":" + KeyModel.USER_PASSWORD));
        }
        public static string credentialsNoAuthUser()
        {
            return Convert.ToBase64String(Encoding.ASCII.GetBytes(KeyModel.USER_EMAIL + ":" + KeyModel.USER_PASSWORD));
        }
        public static string credentials(string phone, string password)
        {
            return Convert.ToBase64String(Encoding.ASCII.GetBytes(phone + ":" + password));
        }

        public static string EncryptAPP(string data)
        {
            RijndaelManaged rijndaelCipher = new RijndaelManaged();
            rijndaelCipher.Mode = CipherMode.CBC; //remember this parameter
            rijndaelCipher.Padding = PaddingMode.PKCS7; //remember this parameter

            rijndaelCipher.KeySize = 0x80;
            rijndaelCipher.BlockSize = 0x80;
            byte[] pwdBytes = Encoding.UTF8.GetBytes(KeyModel.APPKEY);
            byte[] keyBytes = new byte[0x10];
            int len = pwdBytes.Length;

            if (len > keyBytes.Length)
            {
                len = keyBytes.Length;
            }

            Array.Copy(pwdBytes, keyBytes, len);
            rijndaelCipher.Key = keyBytes;
            rijndaelCipher.IV = keyBytes;
            ICryptoTransform transform = rijndaelCipher.CreateEncryptor();
            byte[] plainText = Encoding.UTF8.GetBytes(data);

            return Convert.ToBase64String
            (transform.TransformFinalBlock(plainText, 0, plainText.Length));
        }
    }
}