using System;

namespace QryptoCard.Sec
{
    // Base64 text helpers only. The reversible symmetric cipher that formerly lived
    // here (EncryptAPP / DecryptAPP / EncryptDB / DecryptDB / APPtoDB / DBtoAPP) used
    // Rijndael-CBC with IV == key under master keys that had been exposed, making it
    // both deterministic and recoverable. It has been removed: passwords and API
    // secrets now use one-way bcrypt (QryptoCard.INT.Security.PasswordHasher), and
    // recoverable secrets (TOTP/2FA seeds) use authenticated AES-256-GCM
    // (QryptoCard.INT.Security.AesUtility). The DBKEY/APPKEY master keys this class
    // depended on are retired.
    public class Secure
    {
        public static string Base64Encode(string plainText)
        {
            var plainTextBytes = System.Text.Encoding.UTF8.GetBytes(plainText);
            return System.Convert.ToBase64String(plainTextBytes);
        }

        public static string Base64Decode(string base64EncodedData)
        {
            var base64EncodedBytes = System.Convert.FromBase64String(base64EncodedData);
            return System.Text.Encoding.UTF8.GetString(base64EncodedBytes);
        }
    }
}
