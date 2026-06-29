using System.Collections.Generic;
using QryptoCard.INT.Callback.Model.WasabiCard;
using QryptoCard.INT.Callback.Service;
using Xunit;

namespace QryptoCard.Tests.Unit
{
    /// <summary>
    /// The money-out pre-flight: before an auto-fund transfer is sent, the configured WasabiCard
    /// deposit address must be confirmed present on our merchant account's live address list. These
    /// pin the fail-CLOSED decision logic — the destination of real funds, so the easiest thing to
    /// get dangerously wrong: a definitively-absent address AND an unverifiable (API down/garbled)
    /// read must BOTH block; only an exact, case-sensitive match may proceed.
    /// </summary>
    public class WasabiCardAddressGuardTests
    {
        // A real-shaped TRON address (the prod USDT-TRC20 destination format).
        private const string Addr = "TC75gyAywXztsQAmwLhkAvcYm6yEjjBEQP";

        private static WCAddressListResponseModel.Datum Entry(string coinKey, string address)
        {
            return new WCAddressListResponseModel.Datum
            {
                coinKey = coinKey,
                chain = "TRC20",
                coinName = "USDT",
                address = address
            };
        }

        private static WCAddressListResponseModel Ok(params WCAddressListResponseModel.Datum[] entries)
        {
            return new WCAddressListResponseModel
            {
                success = true,
                code = 200,
                data = new List<WCAddressListResponseModel.Datum>(entries)
            };
        }

        [Fact]
        public void Listed_WhenAddressPresent_ReturnsMatchedCoinKey()
        {
            string coinKey;
            var r = WasabiCardAddressGuard.Evaluate(
                Ok(Entry("USDT_BSC", "Tdifferent000000000000000000000000"), Entry("USDT_TRC20", Addr)),
                Addr, out coinKey);
            Assert.Equal(AddressVerifyResult.Listed, r);
            Assert.Equal("USDT_TRC20", coinKey); // surfaced for logging only, not a gate
        }

        [Fact]
        public void NotListed_WhenAddressAbsent()
        {
            string coinKey;
            var r = WasabiCardAddressGuard.Evaluate(
                Ok(Entry("USDT_TRC20", "Tdifferent000000000000000000000000")), Addr, out coinKey);
            Assert.Equal(AddressVerifyResult.NotListed, r);
            Assert.Null(coinKey);
        }

        [Fact]
        public void NotListed_WhenCaseDiffers() // TRON base58 is case-sensitive: a case change is a different address
        {
            string coinKey;
            var r = WasabiCardAddressGuard.Evaluate(
                Ok(Entry("USDT_TRC20", Addr.ToLowerInvariant())), Addr, out coinKey);
            Assert.Equal(AddressVerifyResult.NotListed, r);
        }

        [Fact]
        public void Listed_WhenProviderAddressHasSurroundingWhitespace() // incidental whitespace must not false-block
        {
            string coinKey;
            var r = WasabiCardAddressGuard.Evaluate(
                Ok(Entry("USDT_TRC20", "  " + Addr + "\n")), Addr, out coinKey);
            Assert.Equal(AddressVerifyResult.Listed, r);
        }

        [Fact]
        public void NotListed_WhenEmptyData()
        {
            string coinKey;
            Assert.Equal(AddressVerifyResult.NotListed,
                WasabiCardAddressGuard.Evaluate(Ok(), Addr, out coinKey));
        }

        [Fact]
        public void Unverifiable_WhenNull()
        {
            string coinKey;
            Assert.Equal(AddressVerifyResult.Unverifiable,
                WasabiCardAddressGuard.Evaluate(null, Addr, out coinKey));
        }

        [Fact]
        public void Unverifiable_WhenNotSuccess()
        {
            string coinKey;
            var m = Ok(Entry("USDT_TRC20", Addr));
            m.success = false; // e.g. provider returned a 200 with an error envelope
            Assert.Equal(AddressVerifyResult.Unverifiable,
                WasabiCardAddressGuard.Evaluate(m, Addr, out coinKey));
        }

        [Fact]
        public void Unverifiable_WhenCodeNot200()
        {
            string coinKey;
            var m = Ok(Entry("USDT_TRC20", Addr));
            m.code = 500;
            Assert.Equal(AddressVerifyResult.Unverifiable,
                WasabiCardAddressGuard.Evaluate(m, Addr, out coinKey));
        }

        [Fact]
        public void Unverifiable_WhenDataNull() // a garbled body deserializes to success=false / null data
        {
            string coinKey;
            var m = new WCAddressListResponseModel { success = true, code = 200, data = null };
            Assert.Equal(AddressVerifyResult.Unverifiable,
                WasabiCardAddressGuard.Evaluate(m, Addr, out coinKey));
        }

        [Theory]
        [InlineData("")]
        [InlineData(null)]
        public void NotListed_WhenTargetAddressBlank(string blank)
        {
            string coinKey;
            Assert.Equal(AddressVerifyResult.NotListed,
                WasabiCardAddressGuard.Evaluate(Ok(Entry("USDT_TRC20", Addr)), blank, out coinKey));
        }
    }
}
