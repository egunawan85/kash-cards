using System.Collections.Generic;
using System.Linq;
using QryptoCard.INT.Model.WasabiCard;
using QryptoCard.INT.Script.Service;
using Xunit;

namespace QryptoCard.Tests.Integration
{
    // Pure-logic tests for the WasabiCard getCityList -> US town-code extraction. WasabiCard's
    // createHolder `town` field is a CITY CODE from this catalog (sending a name fails with 40002
    // "town parameter error"), so this parsing is the load-bearing part of the card-buy fix. No I/O.
    public class CardholderGeoTests
    {
        static WCCityListRequestModel.Datum Region(string code, string country, string stdCode, params WCCityListRequestModel.Child[] cities)
            => new WCCityListRequestModel.Datum
            {
                code = code,
                country = country,
                countryStandardCode = stdCode,
                children = cities.ToList()
            };

        static WCCityListRequestModel.Child City(string code, string country = null, string stdCode = null)
            => new WCCityListRequestModel.Child { code = code, country = country, countryStandardCode = stdCode };

        static WCCityListRequestModel Resp(params WCCityListRequestModel.Datum[] data)
            => new WCCityListRequestModel { success = true, code = 200, data = data.ToList() };

        [Fact]
        public void UsRegion_ReturnsItsCityCodes_AndExcludesNonUs()
        {
            var resp = Resp(
                Region("GA", "US", "US", City("ATL"), City("SAV")),
                Region("ON", "CA", "CA", City("TOR")),       // non-US region excluded
                Region("TX", "US", "US", City("DAL")));

            var codes = CardholderGeoService.ExtractUsCityCodes(resp);

            Assert.Equal(new[] { "ATL", "SAV", "DAL" }, codes);
            Assert.DoesNotContain("TOR", codes);
        }

        [Fact]
        public void RecognizesUs_ByCountryStandardCode_WhenCountryFieldDiffers()
        {
            var resp = Resp(Region("NY", "United States", "US", City("NYC")));
            Assert.Equal(new[] { "NYC" }, CardholderGeoService.ExtractUsCityCodes(resp));
        }

        [Theory]
        [InlineData("USA", null)]
        [InlineData(null, "USA")]
        [InlineData(null, "840")]
        [InlineData("United States of America", null)]
        public void RecognizesUs_AcrossEncodings(string country, string stdCode)
        {
            var resp = Resp(Region("R", country, stdCode, City("C1")));
            Assert.Equal(new[] { "C1" }, CardholderGeoService.ExtractUsCityCodes(resp));
        }

        [Fact]
        public void Fallback_ChildLevelUsTagging_WhenRegionNotTaggedUs()
        {
            // Region carries no US tag, but individual cities do -> fallback picks the US children.
            var resp = Resp(Region("X", null, null, City("LAX", "US", "US"), City("YVR", "CA", "CA")));
            var codes = CardholderGeoService.ExtractUsCityCodes(resp);
            Assert.Equal(new[] { "LAX" }, codes);
        }

        [Fact]
        public void DeDupesRepeatedCodes_AndIgnoresBlankCodes()
        {
            var resp = Resp(
                Region("GA", "US", "US", City("ATL"), City("  "), City("ATL")),
                Region("FL", "US", "US", City("ATL")));
            Assert.Equal(new[] { "ATL" }, CardholderGeoService.ExtractUsCityCodes(resp));
        }

        [Fact]
        public void NullOrEmpty_ReturnsEmpty_NeverNull()
        {
            Assert.Empty(CardholderGeoService.ExtractUsCityCodes(null));
            Assert.Empty(CardholderGeoService.ExtractUsCityCodes(new WCCityListRequestModel()));
            Assert.Empty(CardholderGeoService.ExtractUsCityCodes(Resp()));
        }

        [Fact]
        public void NoUsAnywhere_ReturnsEmpty()
        {
            var resp = Resp(Region("ON", "CA", "CA", City("TOR")), Region("BC", "CA", "CA", City("YVR")));
            Assert.Empty(CardholderGeoService.ExtractUsCityCodes(resp));
        }
    }
}
