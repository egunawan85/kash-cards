using System;
using QryptoCard.Sec;
using Xunit;

namespace QryptoCard.Tests.Unit
{
    public class SecretsConfigTests
    {
        private static string Fresh(string p) => "KC_T_" + p + "_" + Guid.NewGuid().ToString("N");

        [Fact]
        public void Require_Throws_WhenMissing()
        {
            Assert.ThrowsAny<Exception>(() => SecretsConfig.Require(Fresh("MISS")));
        }

        [Fact]
        public void Require_Returns_WhenSet()
        {
            var name = Fresh("SET");
            Environment.SetEnvironmentVariable(name, "hello");
            Assert.Equal("hello", SecretsConfig.Require(name));
        }

        [Fact]
        public void Preload_Aggregates_AllMissing_InOneError()
        {
            var a = Fresh("PA");
            var b = Fresh("PB");
            var ex = Assert.ThrowsAny<Exception>(() => SecretsConfig.Preload(a, b));
            Assert.Contains(a, ex.Message);
            Assert.Contains(b, ex.Message); // both names listed, not just the first
        }

        [Fact]
        public void Preload_Passes_WhenAllPresent()
        {
            var a = Fresh("OK");
            Environment.SetEnvironmentVariable(a, "x");
            SecretsConfig.Preload(a); // must not throw
        }

        [Fact]
        public void RequireHexBytes_AcceptsExactLength_RejectsBadHex()
        {
            var ok = Fresh("HEXOK");
            Environment.SetEnvironmentVariable(ok, new string('a', 64));
            Assert.Equal(32, SecretsConfig.RequireHexBytes(ok, 32).Length);

            var bad = Fresh("HEXBAD");
            Environment.SetEnvironmentVariable(bad, "zz"); // wrong length and not hex
            Assert.ThrowsAny<Exception>(() => SecretsConfig.RequireHexBytes(bad, 32));
        }

        [Fact]
        public void GetOptional_FallsBackThenReadsLive()
        {
            var name = Fresh("OPT");
            Assert.Equal("def", SecretsConfig.GetOptional(name, "def"));
            Environment.SetEnvironmentVariable(name, "real");
            Assert.Equal("real", SecretsConfig.GetOptional(name, "def"));
        }
    }
}
