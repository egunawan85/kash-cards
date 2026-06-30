using QryptoCard.INT.Model.WasabiCard;
using QryptoCard.INT.Script.Gateway.WasabiCard;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Web;
using System.Web.Caching;

namespace QryptoCard.INT.Script.Service
{
    /// <summary>
    /// Single source of truth for the card catalog. Card types are sourced LIVE from
    /// WasabiCard (cached briefly), exposed for ONLY the programs WasabiCard marks
    /// status=online (orderable), and overlaid with our two global pricing settings
    /// — card price and deposit-fee % — so the display path and the money path
    /// (openCard / depositCard) can never disagree on price/fee.
    ///
    /// WasabiCard's wholesale price/fee are preserved in the Original* fields for
    /// reference; the customer-facing CardPrice/RechargeFeeRate are OUR settings.
    /// The settings are read on EVERY call (only the slow WasabiCard fetch is cached),
    /// so a settings change takes effect on the next transaction — matching the
    /// "live rate, history immutable" pricing model (each order already snapshots the
    /// price/fee it was charged onto its own row).
    /// </summary>
    public static class CardCatalogService
    {
        // Only the upstream WasabiCard fetch is cached (it is a signed HTTP round-trip);
        // the markup is re-applied from settings on every read.
        const string CacheKey = "wasabi_card_catalog_online_v1";
        static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(5);
        static readonly object _fetchLock = new object();

        // tblM_Setting.Name keys for the two global pricing knobs (admin-editable).
        public const string SettingCardPrice = "CardPrice";
        public const string SettingDepositFeeRate = "CardDepositFeeRate";
        // Defaults when the setting row is missing/blank.
        public const double DefaultCardPrice = 0d;
        public const double DefaultDepositFeeRate = 3d;

        // Env-driven card-price markup (deploy-controlled, read live each catalog build). The
        // customer CardPrice = WasabiCard's wholesale OriginalCardPrice marked up by this %, rounded
        // UP to the next whole dollar, so we are never below WasabiCard's wholesale cost. WasabiCard's
        // deposit fee is <=0.2% (covered by our 3% deposit fee), so ONLY the issuance price is marked
        // up here — the deposit fee is unchanged. A flat CARD_PRICE_GLOBAL override, if set, replaces
        // wholesale+markup for every card.
        public const string EnvCardPriceMarkup = "CARD_PRICE_MARKUP";   // global %, or per-card CARD_PRICE_MARKUP_<cardTypeId>
        public const string EnvCardPriceGlobal = "CARD_PRICE_GLOBAL";   // optional flat price for all cards
        public const double DefaultCardPriceMarkupPct = 0d;             // 0 => sell at wholesale (break-even, never below cost)

        const string PriceCurrency = "USD";

        /// <summary>
        /// The orderable catalog (status=online) with our pricing overlay applied.
        /// Never throws — returns an empty list if WasabiCard is unreachable so callers
        /// degrade to "no cards available" rather than a 500.
        /// </summary>
        public static List<tblM_Card_Type> GetCatalog()
        {
            // Card price is now per-card (wholesale + markup, computed in Map); only the deposit fee
            // remains a single global knob.
            double fee = GetSetting(SettingDepositFeeRate, DefaultDepositFeeRate);
            return GetOnlineDatums().Select(d => Map(d, fee)).ToList();
        }

        /// <summary>A single online card type by id, pricing overlay applied, or null if not orderable.</summary>
        public static tblM_Card_Type GetById(long cardTypeId)
        {
            return GetCatalog().FirstOrDefault(c => c.CardTypeId == cardTypeId);
        }

        /// <summary>
        /// The current global deposit-fee % (admin setting), read directly — NOT gated on the
        /// live catalog. Top-ups of an already-owned card use this so they keep working even when
        /// a card type is momentarily offline or WasabiCard is briefly unreachable.
        /// </summary>
        public static double GetDepositFeeRate()
        {
            return GetSetting(SettingDepositFeeRate, DefaultDepositFeeRate);
        }

        /// <summary>
        /// The customer-facing card price for an already-fetched catalog entry, parsed numerically
        /// (InvariantCulture — Map writes it that way, so a comma-decimal host can't misread it), or 0
        /// if unparseable. The buy paths use this instead of a single global price.
        /// </summary>
        public static double PriceOf(tblM_Card_Type c)
        {
            double v;
            return (c != null && double.TryParse(c.CardPrice, NumberStyles.Any, CultureInfo.InvariantCulture, out v)) ? v : 0d;
        }

        /// <summary>
        /// Pure markup math (unit-testable, no I/O): wholesale marked up by <paramref name="markupPct"/>%,
        /// rounded UP to the next whole dollar. Negative inputs are floored to 0.
        /// </summary>
        public static double MarkupPrice(double wholesale, double markupPct)
        {
            if (wholesale < 0d) wholesale = 0d;
            if (markupPct < 0d) markupPct = 0d;
            return Math.Ceiling(wholesale * (1d + markupPct / 100d));
        }

        // Customer card price for a card: a flat CARD_PRICE_GLOBAL override if set, else the WasabiCard
        // wholesale price marked up by the (per-card or global) % and rounded up (MarkupPrice).
        static double ComputeCardPrice(string wholesaleStr, long cardTypeId)
        {
            double flat;
            var ov = QryptoCard.Sec.SecretsConfig.GetOptional(EnvCardPriceGlobal, null);
            if (!string.IsNullOrWhiteSpace(ov) &&
                double.TryParse(ov, NumberStyles.Any, CultureInfo.InvariantCulture, out flat) && flat >= 0d)
                return Math.Ceiling(flat);

            double wholesale;
            if (!double.TryParse(wholesaleStr, NumberStyles.Any, CultureInfo.InvariantCulture, out wholesale))
                wholesale = 0d;
            return MarkupPrice(wholesale, GetMarkupPct(cardTypeId));
        }

        // Markup %: a per-card CARD_PRICE_MARKUP_<cardTypeId> override, else the global CARD_PRICE_MARKUP,
        // else the default. Blank/non-numeric/negative values fall through to the next source.
        static double GetMarkupPct(long cardTypeId)
        {
            double v;
            var perCard = QryptoCard.Sec.SecretsConfig.GetOptional(EnvCardPriceMarkup + "_" + cardTypeId, null);
            if (!string.IsNullOrWhiteSpace(perCard) &&
                double.TryParse(perCard, NumberStyles.Any, CultureInfo.InvariantCulture, out v) && v >= 0d)
                return v;
            var global = QryptoCard.Sec.SecretsConfig.GetOptional(EnvCardPriceMarkup, null);
            if (!string.IsNullOrWhiteSpace(global) &&
                double.TryParse(global, NumberStyles.Any, CultureInfo.InvariantCulture, out v) && v >= 0d)
                return v;
            return DefaultCardPriceMarkupPct;
        }

        // ---- internals -------------------------------------------------------

        // Cached list of the WasabiCard data rows that are status=online. Caching the
        // RAW datums (not our marked-up entities) keeps the pricing overlay live while
        // still amortising the upstream call. Double-checked lock collapses a cache-miss
        // stampede onto one fetch.
        static List<WCCardTypeResponseModel.Datum> GetOnlineDatums()
        {
            var cache = HttpRuntime.Cache;
            var hit = cache != null ? cache[CacheKey] as List<WCCardTypeResponseModel.Datum> : null;
            if (hit != null) return hit;

            lock (_fetchLock)
            {
                var hit2 = cache != null ? cache[CacheKey] as List<WCCardTypeResponseModel.Datum> : null;
                if (hit2 != null) return hit2;

                var online = FetchOnline();
                // Only cache a non-empty result, so a transient WasabiCard failure isn't
                // pinned for the full TTL — the next request retries.
                if (cache != null && online.Count > 0)
                    cache.Insert(CacheKey, online, null, DateTime.UtcNow.Add(CacheTtl), Cache.NoSlidingExpiration);
                return online;
            }
        }

        static List<WCCardTypeResponseModel.Datum> FetchOnline()
        {
            WCCardTypeResponseModel resp;
            try { resp = WasabiCardService.getCardType(); }
            catch { return new List<WCCardTypeResponseModel.Datum>(); }

            if (resp == null || !resp.success || resp.data == null)
                return new List<WCCardTypeResponseModel.Datum>();

            return resp.data
                .Where(d => d != null && string.Equals(d.status, "online", StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        // Map a WasabiCard datum -> the tblM_Card_Type shape every existing consumer
        // already understands, applying the pricing overlay. WasabiCard's wholesale
        // price/fee go to Original*; the customer-facing CardPrice/RechargeFeeRate are ours.
        static tblM_Card_Type Map(WCCardTypeResponseModel.Datum d, double fee)
        {
            // Recharge quota/fee live under extFieldVO.rechargeCurrencyInfos when present.
            var rc = d.extFieldVO?.rechargeCurrencyInfos?.FirstOrDefault();
            return new tblM_Card_Type
            {
                CardTypeId = d.cardTypeId,
                Organization = d.organization,
                Country = d.country,
                BankCardBin = d.bankCardBin,
                Type = d.type,
                TypeStr = d.typeStr,
                CardName = d.cardName,
                CardDesc = d.cardDesc,

                // --- our pricing overlay (customer-facing): per-card price = wholesale + markup ---
                CardPrice = ComputeCardPrice(d.cardPrice, d.cardTypeId).ToString(CultureInfo.InvariantCulture),
                CardPriceCurrency = PriceCurrency,
                RechargeFeeRate = fee.ToString(CultureInfo.InvariantCulture),

                // --- WasabiCard wholesale, preserved for reference ---
                OriginalCardPrice = d.cardPrice,
                OriginalCardPriceCurrency = d.cardPriceCurrency,
                OriginalRechargeFeeRate = rc?.rechargeFeeRate,
                OriginalRechargeFee = rc?.rechargeFee,

                RechargeMinQuota = rc?.rechargeMinQuota,
                RechargeMaxQuota = rc?.rechargeMaxQuota,

                // --- WasabiCard authoritative limits/flags (NOT marked up) ---
                DepositAmountMinQuotaForActiveCard = d.depositAmountMinQuotaForActiveCard,
                DepositAmountMaxQuotaForActiveCard = d.depositAmountMaxQuotaForActiveCard,
                FiatCurrency = d.fiatCurrency,
                NeedCardHolder = d.needCardHolder ? 1 : 0,
                NeedDepositForActiveCard = d.needDepositForActiveCard ? 1 : 0,
                MaxCount = d.maxCount,
                Status = d.status,
                isActive = 1,
            };
        }

        static double GetSetting(string name, double fallback)
        {
            try
            {
                using (var db = new DBEntities())
                {
                    var s = db.tblM_Setting.FirstOrDefault(p => p.Name == name);
                    return (s != null && s.Value.HasValue) ? s.Value.Value : fallback;
                }
            }
            catch
            {
                return fallback;
            }
        }
    }
}
