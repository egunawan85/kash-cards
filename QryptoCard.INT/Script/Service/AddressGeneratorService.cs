using System;
using System.Globalization;

namespace QryptoCard.INT.Script.Service
{
    /// <summary>
    /// On-the-fly synthesizer of plausible US cardholder details (name, street address,
    /// city/state/ZIP, phone) for the frictionless card-buy flow. Replaces the old
    /// tblM_Address_Generator pool table (which is unseeded/empty) — values are generated
    /// per call from small embedded arrays so there is never a "no address available" stall.
    ///
    /// These are throwaway holder details fed to WasabiCard's createHolder; they are not the
    /// buyer's real identity. App-code only (the Date/random restrictions that apply to the
    /// workflow scripts do not apply here), so a fresh Random per call is fine.
    /// </summary>
    public static class AddressGeneratorService
    {
        // Real US city / state / ZIP triples, so the synthesized address is geographically valid.
        static readonly string[][] Cities = new[]
        {
            new[] { "New York", "NY", "10001" },
            new[] { "Los Angeles", "CA", "90001" },
            new[] { "Chicago", "IL", "60601" },
            new[] { "Houston", "TX", "77001" },
            new[] { "Phoenix", "AZ", "85001" },
            new[] { "Philadelphia", "PA", "19101" },
            new[] { "San Antonio", "TX", "78201" },
            new[] { "San Diego", "CA", "92101" },
            new[] { "Dallas", "TX", "75201" },
            new[] { "San Jose", "CA", "95101" },
            new[] { "Austin", "TX", "73301" },
            new[] { "Jacksonville", "FL", "32099" },
            new[] { "Columbus", "OH", "43085" },
            new[] { "Charlotte", "NC", "28201" },
            new[] { "Indianapolis", "IN", "46201" },
            new[] { "Seattle", "WA", "98101" },
            new[] { "Denver", "CO", "80201" },
            new[] { "Boston", "MA", "02108" },
            new[] { "Nashville", "TN", "37201" },
            new[] { "Atlanta", "GA", "30301" },
        };

        static readonly string[] StreetNames = new[]
        {
            "Main St", "Oak Ave", "Maple Dr", "Cedar Ln", "Pine St", "Elm St",
            "Washington Ave", "Lake View Dr", "Sunset Blvd", "Park Ave",
            "Hillcrest Rd", "Madison Ave", "Franklin St", "Highland Ave",
            "Riverside Dr", "Lincoln St", "Church St", "Spring St",
        };

        // US area codes that pair with the embedded cities (digits-only phone format).
        static readonly string[] AreaCodes = new[]
        {
            "212", "213", "312", "713", "602", "215", "210", "619",
            "214", "408", "512", "904", "614", "704", "317", "206",
            "303", "617", "615", "404",
        };

        static readonly string[] FirstNames = new[]
        {
            "James", "Mary", "John", "Patricia", "Robert", "Jennifer", "Michael",
            "Linda", "William", "Elizabeth", "David", "Barbara", "Richard", "Susan",
            "Joseph", "Jessica", "Thomas", "Sarah", "Charles", "Karen",
        };

        static readonly string[] LastNames = new[]
        {
            "Smith", "Johnson", "Williams", "Brown", "Jones", "Garcia", "Miller",
            "Davis", "Rodriguez", "Martinez", "Hernandez", "Lopez", "Wilson",
            "Anderson", "Thomas", "Taylor", "Moore", "Jackson", "Martin", "Lee",
        };

        /// <summary>A synthesized, geographically-valid US address + phone (digits only).</summary>
        public static (string Street, string City, string State, string PostCode, string Phone) NextUsAddress()
        {
            var rnd = NewRandom();
            int cityIdx = rnd.Next(Cities.Length);
            var city = Cities[cityIdx];

            string street = rnd.Next(100, 9999).ToString(CultureInfo.InvariantCulture)
                + " " + StreetNames[rnd.Next(StreetNames.Length)];

            // Area code paired with the chosen city when the lists line up, else any.
            string area = cityIdx < AreaCodes.Length ? AreaCodes[cityIdx] : AreaCodes[rnd.Next(AreaCodes.Length)];
            string phone = area
                + rnd.Next(200, 999).ToString(CultureInfo.InvariantCulture)
                + rnd.Next(0, 9999).ToString("0000", CultureInfo.InvariantCulture);

            return (street, city[0], city[1], city[2], phone);
        }

        /// <summary>A random first/last name pair.</summary>
        public static (string First, string Last) RandomName()
        {
            var rnd = NewRandom();
            return (FirstNames[rnd.Next(FirstNames.Length)], LastNames[rnd.Next(LastNames.Length)]);
        }

        // Guid-seeded so back-to-back calls (same tick) don't collapse onto the same value.
        static Random NewRandom() => new Random(Guid.NewGuid().GetHashCode());
    }
}
