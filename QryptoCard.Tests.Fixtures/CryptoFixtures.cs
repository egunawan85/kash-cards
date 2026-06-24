using System;
using System.Security.Cryptography;
using System.Text;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.X509;

namespace QryptoCard.Tests.Fixtures
{
    /// <summary>Shared crypto helpers for producing valid provider-style signatures in tests.</summary>
    public static class CryptoFixtures
    {
        public static byte[] Utf8(string s) => Encoding.UTF8.GetBytes(s);

        // --- Runegate webhook: "t=<unix>,v1=<hex(HMAC-SHA256(secret, ts + "." + body))>" ---
        public static string RunegateHeader(string secret, long ts, byte[] body)
        {
            byte[] prefix = Encoding.UTF8.GetBytes(ts + ".");
            byte[] input = new byte[prefix.Length + body.Length];
            Buffer.BlockCopy(prefix, 0, input, 0, prefix.Length);
            Buffer.BlockCopy(body, 0, input, prefix.Length, body.Length);
            using (var h = new HMACSHA256(Encoding.UTF8.GetBytes(secret)))
            {
                string hex = BitConverter.ToString(h.ComputeHash(input)).Replace("-", "").ToLowerInvariant();
                return "t=" + ts + ",v1=" + hex;
            }
        }

        // --- WasabiCard: SHA256withRSA over the raw body; key generated/exported via BouncyCastle
        //     (so the public key is X.509 SubjectPublicKeyInfo base64, matching the verifier). ---
        public sealed class RsaPair
        {
            public AsymmetricKeyParameter PrivateKey;
            public string PublicKeySpkiBase64;
        }

        public static RsaPair NewRsaPair(int bits = 2048)
        {
            var gen = new RsaKeyPairGenerator();
            gen.Init(new KeyGenerationParameters(new SecureRandom(), bits));
            AsymmetricCipherKeyPair kp = gen.GenerateKeyPair();
            var spki = SubjectPublicKeyInfoFactory.CreateSubjectPublicKeyInfo(kp.Public);
            return new RsaPair
            {
                PrivateKey = kp.Private,
                PublicKeySpkiBase64 = Convert.ToBase64String(spki.GetDerEncoded())
            };
        }

        public static string WasabiSign(AsymmetricKeyParameter privateKey, byte[] body)
        {
            ISigner signer = SignerUtilities.GetSigner("SHA-256withRSA");
            signer.Init(true, privateKey);
            signer.BlockUpdate(body, 0, body.Length);
            return Convert.ToBase64String(signer.GenerateSignature());
        }
    }
}
