using System;
using System.Diagnostics;
using System.Security.Cryptography;
using QryptoCard.INT.Security;
using Xunit;

namespace QryptoCard.Tests.Unit
{
    // Covers the crypto-at-rest primitives that replaced the old reversible
    // Rijndael (IV == key) scheme: one-way bcrypt for passwords/API secrets
    // (PasswordHasher) and authenticated AES-256-GCM for recoverable secrets
    // such as TOTP/2FA seeds (AesUtility).
    public class PasswordHasherTests
    {
        [Fact]
        public void HashThenVerify_RoundTrips()
        {
            var hash = PasswordHasher.Hash("correct horse battery staple");
            Assert.True(PasswordHasher.Verify("correct horse battery staple", hash));
        }

        [Fact]
        public void Verify_WrongPassword_ReturnsFalse()
        {
            var hash = PasswordHasher.Hash("the-right-one");
            Assert.False(PasswordHasher.Verify("the-wrong-one", hash));
        }

        [Fact]
        public void Hash_IsSaltedSoSamePlaintextProducesDistinctHashes()
        {
            var h1 = PasswordHasher.Hash("same-password");
            var h2 = PasswordHasher.Hash("same-password");
            Assert.NotEqual(h1, h2);              // per-hash random salt
            Assert.True(PasswordHasher.Verify("same-password", h1));
            Assert.True(PasswordHasher.Verify("same-password", h2));
        }

        [Fact]
        public void Hash_ProducesBcryptShapedString()
        {
            var hash = PasswordHasher.Hash("anything");
            Assert.StartsWith("$2", hash);        // bcrypt identifier
            Assert.Equal(60, hash.Length);        // bcrypt hashes are 60 chars
        }

        [Fact]
        public void Verify_NullOrEmpty_ReturnsFalse()
        {
            var hash = PasswordHasher.Hash("anything");
            Assert.False(PasswordHasher.Verify(null, hash));
            Assert.False(PasswordHasher.Verify("", hash));
            Assert.False(PasswordHasher.Verify("anything", null));
            Assert.False(PasswordHasher.Verify("anything", ""));
        }

        [Fact]
        public void Verify_NonBcryptStoredValue_ReturnsFalseNotThrows()
        {
            // Legacy ciphertext, a forced-reset sentinel, or a corrupted row: must fail
            // closed rather than throw (SaltParseException is swallowed).
            Assert.False(PasswordHasher.Verify("anything", "not-a-bcrypt-hash"));
            Assert.False(PasswordHasher.Verify("anything", "RESET-REQUIRED"));
        }

        [Fact]
        public void Hash_EmptyPlaintext_Throws()
        {
            Assert.Throws<ArgumentException>(() => PasswordHasher.Hash(""));
            Assert.Throws<ArgumentException>(() => PasswordHasher.Hash(null));
        }

        [Fact]
        public void VerifyWithUniformTiming_NullStoredHash_ReturnsFalseAndRunsBcrypt()
        {
            // On an account miss it must still run a real bcrypt verify (against the
            // internal dummy hash) so latency doesn't reveal account existence. bcrypt at
            // work factor 12 is tens-to-hundreds of ms; assert a generous lower bound so a
            // short-circuit (which would be near-instant) is caught without timing flakiness.
            var sw = Stopwatch.StartNew();
            var result = PasswordHasher.VerifyWithUniformTiming("any-password", null);
            sw.Stop();
            Assert.False(result);
            Assert.True(sw.ElapsedMilliseconds >= 10,
                $"expected a real bcrypt run (>=10ms); took {sw.ElapsedMilliseconds}ms");
        }

        [Fact]
        public void VerifyWithUniformTiming_RealHash_VerifiesNormally()
        {
            var hash = PasswordHasher.Hash("pw");
            Assert.True(PasswordHasher.VerifyWithUniformTiming("pw", hash));
            Assert.False(PasswordHasher.VerifyWithUniformTiming("nope", hash));
        }

        [Theory]
        [InlineData("!RESET-REQUIRED-CRYPTO-MIGRATION!")]  // forced-reset sentinel
        [InlineData("bm90LWJjcnlwdA==")]                   // legacy/base64 ciphertext shape
        [InlineData("plain-garbage")]
        public void VerifyWithUniformTiming_NonBcryptStoredValue_ReturnsFalseAndRunsBcrypt(string stored)
        {
            // A non-bcrypt stored value (e.g. a post-scrub reset sentinel) must NOT take the
            // fast SaltParseException path — it must run the dummy bcrypt so latency matches an
            // account miss, otherwise it's an enumeration oracle during the reset window.
            var sw = Stopwatch.StartNew();
            var result = PasswordHasher.VerifyWithUniformTiming("any-password", stored);
            sw.Stop();
            Assert.False(result);
            Assert.True(sw.ElapsedMilliseconds >= 10,
                $"expected a real bcrypt run (>=10ms) for non-bcrypt stored value; took {sw.ElapsedMilliseconds}ms");
        }
    }

    public class AesUtilityTests
    {
        public AesUtilityTests()
        {
            // AesUtility loads KASH_DATA_KEY (32-byte hex) via SecretsConfig on first use.
            // Set a fixed test key before any Encrypt/Decrypt call. Not a production key.
            Environment.SetEnvironmentVariable("KASH_DATA_KEY",
                "00112233445566778899aabbccddeeff00112233445566778899aabbccddeeff");
        }

        [Fact]
        public void EncryptThenDecrypt_RoundTrips()
        {
            const string plain = "JBSWY3DPEHPK3PXP"; // a TOTP-seed-shaped value
            var ct = AesUtility.Encrypt(plain);
            Assert.Equal(plain, AesUtility.Decrypt(ct));
        }

        [Fact]
        public void Encrypt_IsNonDeterministic_RandomIvPerCall()
        {
            var a = AesUtility.Encrypt("same");
            var b = AesUtility.Encrypt("same");
            Assert.NotEqual(a, b);                       // random IV => distinct ciphertext
            Assert.Equal("same", AesUtility.Decrypt(a)); // both still decrypt
            Assert.Equal("same", AesUtility.Decrypt(b));
        }

        [Fact]
        public void EmptyString_RoundTrips()
        {
            var ct = AesUtility.Encrypt("");
            Assert.Equal("", AesUtility.Decrypt(ct));
        }

        [Fact]
        public void Decrypt_TamperedCiphertext_FailsClosed()
        {
            var ct = AesUtility.Encrypt("secret-seed");
            var bytes = Convert.FromBase64String(ct);
            bytes[bytes.Length - 1] ^= 0xFF;             // corrupt the GCM auth tag
            var tampered = Convert.ToBase64String(bytes);
            Assert.Throws<CryptographicException>(() => AesUtility.Decrypt(tampered));
        }

        [Fact]
        public void Decrypt_WrongVersionByte_FailsClosed()
        {
            var ct = AesUtility.Encrypt("secret-seed");
            var bytes = Convert.FromBase64String(ct);
            bytes[0] = 0x02;                             // only version 0x01 is supported
            Assert.Throws<CryptographicException>(() => AesUtility.Decrypt(Convert.ToBase64String(bytes)));
        }

        [Fact]
        public void Decrypt_NotBase64_FailsClosed()
        {
            Assert.Throws<CryptographicException>(() => AesUtility.Decrypt("@@@not base64@@@"));
        }

        [Fact]
        public void Decrypt_TooShortEnvelope_FailsClosed()
        {
            var tiny = Convert.ToBase64String(new byte[] { 0x01, 0x00 });
            Assert.Throws<CryptographicException>(() => AesUtility.Decrypt(tiny));
        }

        [Fact]
        public void Decrypt_NullOrEmpty_Throws()
        {
            Assert.Throws<ArgumentException>(() => AesUtility.Decrypt(null));
            Assert.Throws<ArgumentException>(() => AesUtility.Decrypt(""));
        }
    }
}
