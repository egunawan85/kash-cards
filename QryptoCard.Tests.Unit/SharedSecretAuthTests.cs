using System;
using QryptoCard.Sec;
using Xunit;

namespace QryptoCard.Tests.Unit
{
    public class SharedSecretAuthTests
    {
        private const string SecretName = "KC_TEST_AUTH_SECRET";
        private const string Secret = "s3cr3t-shared-value-0123456789abcdef";

        public SharedSecretAuthTests()
        {
            Environment.SetEnvironmentVariable(SecretName, Secret);
        }

        [Fact]
        public void CorrectSecret_IsAuthorized()
        {
            Assert.True(SharedSecretAuth.IsAuthorized(Secret, SecretName));
        }

        [Theory]
        [InlineData("wrong-value")]
        [InlineData("")]
        [InlineData(null)]
        [InlineData("s3cr3t")] // correct prefix, still rejected
        public void BadInput_IsRejected(string provided)
        {
            Assert.False(SharedSecretAuth.IsAuthorized(provided, SecretName));
        }

        [Fact]
        public void MissingConfiguredSecret_FailsClosed()
        {
            // No silent allow: an unconfigured secret throws rather than returning true.
            Assert.ThrowsAny<Exception>(
                () => SharedSecretAuth.IsAuthorized("anything", "KC_TEST_UNSET_" + Guid.NewGuid().ToString("N")));
        }

        [Fact]
        public void FixedTimeEquals_IsCorrect()
        {
            Assert.True(SharedSecretAuth.FixedTimeEquals("abc", "abc"));
            Assert.False(SharedSecretAuth.FixedTimeEquals("abc", "abd")); // content diff
            Assert.False(SharedSecretAuth.FixedTimeEquals("abc", "ab"));  // length diff
            Assert.False(SharedSecretAuth.FixedTimeEquals("abc", null));  // null
        }
    }
}
