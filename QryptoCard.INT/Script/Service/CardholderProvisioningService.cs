using System;
using System.Linq;
using QryptoCard.INT.Model.WasabiCard;
using QryptoCard.INT.Script.Gateway.WasabiCard;

namespace QryptoCard.INT.Script.Service
{
    /// <summary>
    /// Resolves the WasabiCard KYC cardholder a card type requires (needCardHolder), reusing an
    /// existing verified holder for the buyer+card-type when present, else registering one
    /// frictionlessly (name/email from the buyer, address/phone synthesized). Factored out of the
    /// buy path so the deposit-into-card funding intent can create the holder up front
    /// (fail-fast, BEFORE the customer sends any crypto) — a KYC rejection then never strands a
    /// deposit.
    ///
    /// NOTE: CardV1Service.openCard still carries the equivalent logic inline (App+API v1); this
    /// helper is the single place new callers should use, and that inline copy can migrate here
    /// in a follow-up without behavior change.
    /// </summary>
    public static class CardholderProvisioningService
    {
        public class HolderResult
        {
            public bool Ok;
            public long? HolderId;
            public string Error;   // customer-safe message when !Ok
            public bool Retryable; // transient (catalog/provider) vs a hard KYC rejection

            public static HolderResult Success(long id) { return new HolderResult { Ok = true, HolderId = id }; }
            public static HolderResult Fail(string msg, bool retryable) { return new HolderResult { Ok = false, Error = msg, Retryable = retryable }; }
        }

        /// <summary>
        /// Ensure a verified holder exists for (userId, card type). Returns the holder id on success.
        /// No-op success (HolderId = null) when the card type does not need a holder. Never throws for
        /// an expected provider/KYC failure — returns a customer-safe message instead.
        /// </summary>
        public static HolderResult EnsureHolder(DBEntities db, string userId, string email, tblM_Card_Type data)
        {
            if (data == null || data.NeedCardHolder != 1)
                return new HolderResult { Ok = true, HolderId = null }; // no holder needed

            long typeId = data.CardTypeId ?? 0;

            // Reuse a previously-verified holder for this buyer + card type so a repeat purchase does
            // NOT register a duplicate holder at WasabiCard.
            var existing = db.tblM_Cardholder.FirstOrDefault(
                p => p.UserID == userId && p.CardTypeId == typeId
                  && p.isActive == 1 && p.Status == "pass_audit");
            if (existing != null && existing.HolderID.HasValue)
                return HolderResult.Success(existing.HolderID.Value);

            // Frictionless registration: real first/last from the buyer when BOTH present, else random.
            var buyer = db.tblM_User.FirstOrDefault(p => p.Email == email);
            string firstName, lastName;
            if (buyer != null && !string.IsNullOrWhiteSpace(buyer.FirstName) && !string.IsNullOrWhiteSpace(buyer.LastName))
            {
                firstName = buyer.FirstName.Trim();
                lastName = buyer.LastName.Trim();
            }
            else
            {
                var rn = AddressGeneratorService.RandomName();
                firstName = rn.First;
                lastName = rn.Last;
            }

            string holderEmail = (buyer != null && !string.IsNullOrWhiteSpace(buyer.Email)) ? buyer.Email.Trim() : email;
            var addr = AddressGeneratorService.NextUsAddress();

            // WasabiCard `town` is a CITY CODE from getCityList, not a free-text name (name -> 40002).
            string townCode = CardholderGeoService.GetUsCityCode();
            if (string.IsNullOrEmpty(townCode))
                return HolderResult.Fail("Card issuance is temporarily unavailable. Please try again shortly.", true);

            var chx = new WCCreateHolderRequestModel
            {
                cardTypeId = typeId,
                areaCode = "+1",
                mobile = addr.Phone,
                email = holderEmail,
                firstName = firstName,
                lastName = lastName,
                birthday = GenerateBirthdate(),
                country = "US",
                address = addr.Street,
                town = townCode,
                postCode = addr.PostCode,
            };

            var chdr = WasabiCardService.createHolder(chx);
            if (chdr != null && chdr.code == -1)
                return HolderResult.Fail(chdr.msg, false);

            if (chdr != null && chdr.data != null && chdr.data.status == "pass_audit")
            {
                var chu = new tblM_Cardholder
                {
                    ID = Guid.NewGuid().ToString(),
                    UserID = userId,
                    HolderID = chdr.data.holderId,
                    Address = chx.address,
                    Mobile = chx.mobile,
                    Email = chx.email,
                    FirstName = chx.firstName,
                    LastName = chx.lastName,
                    Birthday = chx.birthday,
                    Country = chx.country,
                    CardTypeId = chx.cardTypeId,
                    AreaCode = chx.areaCode,
                    Town = chx.town,
                    PostCode = chx.postCode,
                    DateCreated = DateTime.Now,
                    Status = chdr.data.status,
                    isActive = 1,
                };
                db.tblM_Cardholder.Add(chu);
                db.SaveChanges();
                return HolderResult.Success(chu.HolderID.Value);
            }

            string msg = (chdr != null && !string.IsNullOrEmpty(chdr.msg))
                ? chdr.msg
                : "Card holder registration could not be completed. Please try again.";
            return HolderResult.Fail(msg, false);
        }

        // Mirrors CardV1Service.generateBirthdate: a plausible adult DOB (1970..2000), yyyy-MM-dd.
        static string GenerateBirthdate()
        {
            var min = DateTime.Parse("1970/01/01").Ticks;
            var max = DateTime.Parse("2000/12/31").Ticks;
            var rnd = new Random();
            var newDate = new DateTime(min + (long)(rnd.NextDouble() * (max - min)));
            return newDate.ToString("yyyy-MM-dd");
        }
    }
}
