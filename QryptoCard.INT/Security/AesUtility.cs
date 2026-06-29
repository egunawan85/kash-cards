// BouncyCastle.Cryptography 2.5.0 is aliased in the csproj because the legacy
// BouncyCastle.Crypto 1.8.1 (still used by Common.cs) defines the same
// Org.BouncyCastle.Crypto.* GCM types. Bind this file to the 2.5.0 assembly.
extern alias BCCryptography;
using System;
using System.Security.Cryptography;
using System.Text;
using BCCryptography::Org.BouncyCastle.Crypto.Engines;
using BCCryptography::Org.BouncyCastle.Crypto.Modes;
using BCCryptography::Org.BouncyCastle.Crypto.Parameters;
using QryptoCard.Sec;

namespace QryptoCard.INT.Security
{
    // Authenticated reversible encryption for secrets that must be recoverable at
    // rest (TOTP/2FA seeds). Replaces QryptoCard.Sec.Secure.EncryptAPP /
    // DecryptAPP / EncryptDB / DecryptDB, whose Rijndael-CBC primitive set its IV
    // equal to its key — making the encryption deterministic (same plaintext →
    // same ciphertext) and providing no integrity check.
    //
    // Primitive: AES-256-GCM via BouncyCastle. A fresh 96-bit random IV per call,
    // a 128-bit authentication tag, master key sourced from KASH_DATA_KEY
    // (64 hex chars = 32 bytes) via SecretsConfig. Ciphertext format:
    //
    //     base64( version(1) || iv(12) || ciphertext(n) || tag(16) )
    //
    // The version byte is 0x01, reserved for forward-compat. There is no legacy
    // ciphertext to dual-read (the migration re-encrypts or invalidates old rows),
    // so a version mismatch on decrypt always means corrupted input and fails
    // closed. To migrate the scheme later (rotated key, different primitive), bump
    // the version byte and branch in Decrypt.
    public static class AesUtility
    {
        public const string MasterKeyEnvVarName = "KASH_DATA_KEY";

        private const byte Version1 = 0x01;
        private const int IvLengthBytes = 12;   // 96 bits — the GCM-recommended nonce size
        private const int TagLengthBits = 128;
        private const int TagLengthBytes = TagLengthBits / 8;
        private const int KeyLengthBytes = 32;  // AES-256

        // Loaded once per process via SecretsConfig (validated 32-byte hex).
        // Rotation = app-pool recycle, matching SecretsConfig semantics.
        private static Lazy<byte[]> _keyBytes =
            new Lazy<byte[]>(() => SecretsConfig.RequireHexBytes(MasterKeyEnvVarName, KeyLengthBytes));

        public static string Encrypt(string plaintext)
        {
            if (plaintext == null)
                throw new ArgumentNullException(nameof(plaintext));

            var iv = new byte[IvLengthBytes];
            using (var rng = RandomNumberGenerator.Create())
                rng.GetBytes(iv);

            var plaintextBytes = Encoding.UTF8.GetBytes(plaintext);

            var cipher = new GcmBlockCipher(new AesEngine());
            cipher.Init(true, new AeadParameters(new KeyParameter(_keyBytes.Value), TagLengthBits, iv));

            var output = new byte[cipher.GetOutputSize(plaintextBytes.Length)];
            int len = cipher.ProcessBytes(plaintextBytes, 0, plaintextBytes.Length, output, 0);
            cipher.DoFinal(output, len);
            // output now contains ciphertext || tag (GCM mode appends the tag).

            // envelope = version(1) || iv(12) || ciphertext+tag
            var envelope = new byte[1 + IvLengthBytes + output.Length];
            envelope[0] = Version1;
            Buffer.BlockCopy(iv, 0, envelope, 1, IvLengthBytes);
            Buffer.BlockCopy(output, 0, envelope, 1 + IvLengthBytes, output.Length);

            return Convert.ToBase64String(envelope);
        }

        public static string Decrypt(string ciphertext)
        {
            if (string.IsNullOrEmpty(ciphertext))
                throw new ArgumentException("ciphertext must not be empty", nameof(ciphertext));

            byte[] envelope;
            try { envelope = Convert.FromBase64String(ciphertext); }
            catch (FormatException ex)
            {
                throw new CryptographicException("ciphertext is not valid base64", ex);
            }

            if (envelope.Length < 1 + IvLengthBytes + TagLengthBytes)
                throw new CryptographicException("ciphertext envelope is too short");

            if (envelope[0] != Version1)
                throw new CryptographicException(
                    "unsupported ciphertext version 0x" + envelope[0].ToString("X2"));

            var iv = new byte[IvLengthBytes];
            Buffer.BlockCopy(envelope, 1, iv, 0, IvLengthBytes);

            int cipherAndTagLength = envelope.Length - 1 - IvLengthBytes;
            var cipherAndTag = new byte[cipherAndTagLength];
            Buffer.BlockCopy(envelope, 1 + IvLengthBytes, cipherAndTag, 0, cipherAndTagLength);

            var cipher = new GcmBlockCipher(new AesEngine());
            cipher.Init(false, new AeadParameters(new KeyParameter(_keyBytes.Value), TagLengthBits, iv));

            var output = new byte[cipher.GetOutputSize(cipherAndTag.Length)];
            try
            {
                int len = cipher.ProcessBytes(cipherAndTag, 0, cipherAndTag.Length, output, 0);
                len += cipher.DoFinal(output, len);
                return Encoding.UTF8.GetString(output, 0, len);
            }
            catch (BCCryptography::Org.BouncyCastle.Crypto.InvalidCipherTextException ex)
            {
                // Tag verification failed — tampered or truncated ciphertext, or
                // wrong key. Always fail closed with a generic message; do not leak
                // which failure mode to callers.
                throw new CryptographicException("ciphertext authentication failed", ex);
            }
        }

        // Test-only escape hatch. Production code must never reload the master key
        // mid-process (rotation = app-pool recycle).
        internal static void ResetKeyCacheForTests()
        {
            _keyBytes = new Lazy<byte[]>(() => SecretsConfig.RequireHexBytes(MasterKeyEnvVarName, KeyLengthBytes));
        }
    }
}
