using QryptoCard.INT.Model.WasabiCard;
using QryptoCard.INT.Script.Gateway.WasabiCard;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Web;
using System.Web.Caching;

namespace QryptoCard.INT.Script.Service
{
    /// <summary>
    /// Supplies a valid WasabiCard <c>town</c> value for the frictionless cardholder synthesis.
    ///
    /// WasabiCard's createHolder <c>town</c> field is NOT a free-text city name — it is a CITY CODE
    /// drawn from WasabiCard's own geo catalog (the getCityList API: states at the top level, each
    /// with child cities that carry the code). Sending a human name like "Atlanta" is rejected with
    /// code 40002 "town parameter error", which blocks every KYC card purchase.
    ///
    /// The historical source of these codes (tblM_Address_Generator / tblM_Country_City) is unseeded
    /// in production, so the codes are sourced LIVE from WasabiCard and cached — mirroring
    /// CardCatalogService — rather than depending on a seed table that may be empty.
    /// </summary>
    public static class CardholderGeoService
    {
        // Only the upstream getCityList round-trip is cached; the catalog changes very rarely, so a
        // long TTL is fine (and a transient WasabiCard hiccup is never cached — see below).
        const string CacheKey = "wasabi_us_city_codes_v1";
        static readonly TimeSpan CacheTtl = TimeSpan.FromHours(6);
        static readonly object _fetchLock = new object();

        // Per-call randomness so repeated holder synthesis doesn't always pick the same town.
        static Random NewRandom() => new Random(Guid.NewGuid().GetHashCode());

        /// <summary>
        /// A valid WasabiCard US city code for the holder <c>town</c> field, or null when the catalog
        /// cannot be loaded (caller should fail the buy with a retryable message rather than send a
        /// value WasabiCard will reject).
        /// </summary>
        public static string GetUsCityCode()
        {
            var codes = GetUsCityCodes();
            if (codes == null || codes.Count == 0) return null;
            return codes[NewRandom().Next(codes.Count)];
        }

        // ---- internals -------------------------------------------------------

        static List<string> GetUsCityCodes()
        {
            var cache = HttpRuntime.Cache;
            var hit = cache != null ? cache[CacheKey] as List<string> : null;
            if (hit != null) return hit;

            lock (_fetchLock)
            {
                var hit2 = cache != null ? cache[CacheKey] as List<string> : null;
                if (hit2 != null) return hit2;

                var codes = FetchUsCityCodes();
                // Only cache a non-empty result so a transient failure isn't pinned for the full TTL.
                if (cache != null && codes.Count > 0)
                    cache.Insert(CacheKey, codes, null, DateTime.UtcNow.Add(CacheTtl), Cache.NoSlidingExpiration);
                return codes;
            }
        }

        static List<string> FetchUsCityCodes()
        {
            WCCityListRequestModel resp;
            try { resp = WasabiCardService.getCityList(); }
            catch (Exception ex)
            {
                Trace.TraceError("CardholderGeo: getCityList threw — " + ex.Message);
                return new List<string>();
            }

            if (resp == null || resp.data == null)
            {
                Trace.TraceError("CardholderGeo: getCityList returned no data.");
                return new List<string>();
            }

            var codes = ExtractUsCityCodes(resp);
            if (codes.Count == 0)
                Trace.TraceError("CardholderGeo: getCityList had no US city codes (" + resp.data.Count + " top-level nodes).");
            return codes;
        }

        /// <summary>
        /// Pure extraction of US city codes from a getCityList response (no I/O — unit-testable).
        /// Catalog shape: top-level node = state/region, its children = cities (the town codes),
        /// matching tblM_Country_City (city Code with a RegionCode parent). Collects every city code
        /// under a US region (children inherit the region's country), with fallbacks for a flatter or
        /// child-tagged hierarchy. Returns distinct, trimmed codes; never null.
        /// </summary>
        public static List<string> ExtractUsCityCodes(WCCityListRequestModel resp)
        {
            var codes = new List<string>();
            if (resp == null || resp.data == null) return codes;

            foreach (var region in resp.data)
            {
                if (region == null || !IsUs(region.country, region.countryStandardCode)) continue;
                if (region.children == null) continue;
                foreach (var city in region.children)
                {
                    if (city != null && !string.IsNullOrWhiteSpace(city.code))
                        codes.Add(city.code.Trim());
                }
            }

            // Fallbacks for an unexpected hierarchy: US cities tagged directly at the child level
            // regardless of the parent's country, or a flat list where US nodes are top-level.
            if (codes.Count == 0)
            {
                foreach (var region in resp.data)
                {
                    if (region == null) continue;
                    if (region.children != null)
                        codes.AddRange(region.children
                            .Where(c => c != null && !string.IsNullOrWhiteSpace(c.code) && IsUs(c.country, c.countryStandardCode))
                            .Select(c => c.code.Trim()));
                    if (IsUs(region.country, region.countryStandardCode) && !string.IsNullOrWhiteSpace(region.code))
                        codes.Add(region.code.Trim());
                }
            }

            return codes.Distinct().ToList();
        }

        static bool IsUs(string country, string countryStandardCode)
        {
            return MatchesUs(country) || MatchesUs(countryStandardCode);
        }

        // Tolerant US match: WasabiCard may encode the country as the ISO alpha-2 "US", alpha-3 "USA",
        // the ISO numeric "840", or a display name — accept all so a representation change can't
        // silently empty the candidate list and block every card buy.
        static bool MatchesUs(string s)
        {
            s = (s ?? "").Trim();
            if (s.Length == 0) return false;
            return s.Equals("US", StringComparison.OrdinalIgnoreCase)
                || s.Equals("USA", StringComparison.OrdinalIgnoreCase)
                || s.Equals("840", StringComparison.Ordinal)
                || s.IndexOf("united states", StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }
}
