using System;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Security;

namespace QryptoCard.Sec
{
    /// <summary>
    /// Verifies inbound WasabiCard webhook signatures.
    ///
    /// Per the WasabiCard spec, the platform signs the EXACT raw HTTP request body with its RSA
    /// private key using SHA256withRSA, base64-encoded in the X-WSB-SIGNATURE header; the merchant
    /// verifies with the platform's RSA public key (WASABICARD_WSBPUBLIC_KEY, supplied as base64
    /// X.509 SubjectPublicKeyInfo). An empty body is signed as the literal "{}".
    ///
    /// Fail-closed: returns false on any malformed/forged input, never throws. Rejects a public key
    /// whose modulus is under 1024 bits (defends against a substituted weak/forgeable key). The floor
    /// is 1024 because WasabiCard's PLATFORM signing key is a 1024-bit RSA key — a stricter 2048 floor
    /// rejects their real key and 401s every legitimate webhook. 1024-bit is weaker than ideal but is
    /// the provider's actual key strength (not ours to choose); the floor still blocks a truly-weak
    /// (e.g. 512-bit) substituted key, and the downstream finalize/commission dedup + the deposit-refund
    /// provider cross-check bound the blast radius of any forged delivery.
    /// </summary>
    public static class WasabiSignatureVerifier
    {
        private const int MinModulusBits = 1024;

        public static bool Verify(string signatureBase64, byte[] rawBody, string platformPublicKeyBase64)
        {
            if (string.IsNullOrEmpty(signatureBase64) || rawBody == null || string.IsNullOrEmpty(platformPublicKeyBase64))
                return false;

            // Empty body is signed as "{}" per the spec.
            if (rawBody.Length == 0)
                rawBody = System.Text.Encoding.UTF8.GetBytes("{}");

            try
            {
                byte[] sig = Convert.FromBase64String(signatureBase64.Trim());
                AsymmetricKeyParameter key = PublicKeyFactory.CreateKey(Convert.FromBase64String(platformPublicKeyBase64.Trim()));

                RsaKeyParameters rsa = key as RsaKeyParameters;
                if (rsa == null || rsa.IsPrivate) return false;            // must be an RSA public key
                if (rsa.Modulus.BitLength < MinModulusBits) return false;  // reject weak/substituted key

                ISigner verifier = SignerUtilities.GetSigner("SHA-256withRSA");
                verifier.Init(false, key);
                verifier.BlockUpdate(rawBody, 0, rawBody.Length);
                return verifier.VerifySignature(sig);
            }
            catch
            {
                // Malformed base64 / key / signature -> reject (no exception detail leaked).
                return false;
            }
        }
    }
}
