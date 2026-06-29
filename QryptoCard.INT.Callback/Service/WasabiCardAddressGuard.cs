using System;
using System.Linq;
using QryptoCard.INT.Callback.Model.WasabiCard;

namespace QryptoCard.INT.Callback.Service
{
    /// <summary>
    /// The verdict of the money-out pre-flight: is the configured WasabiCard deposit address still
    /// on our merchant account's live address list?
    ///   Listed       — present; the transfer may proceed.
    ///   NotListed    — definitively absent; the provider answered and our address is not there.
    ///   Unverifiable — we could NOT get a usable answer (null / unsuccessful / garbled response).
    /// Both NotListed and Unverifiable must BLOCK the transfer (fail-closed): we never send to an
    /// address we could not confirm is ours.
    /// </summary>
    public enum AddressVerifyResult { Listed, NotListed, Unverifiable }

    /// <summary>
    /// Pure (no DB / no gateway) decision logic for the WasabiCard deposit-address pre-flight,
    /// factored out so it is unit-testable without an HTTP call — same isolation rationale as
    /// WasabiCardFundingMath. The HTTP read lives in WasabiCardService.addressList(); this class
    /// only decides what its result means.
    /// </summary>
    public static class WasabiCardAddressGuard
    {
        /// <summary>
        /// Decide whether <paramref name="address"/> appears in the merchant's WasabiCard wallet
        /// address list. Fail-closed: a null / unsuccessful / non-200 / null-data response is
        /// Unverifiable (the caller must NOT send). The match is EXACT and case-sensitive — TRON
        /// base58 addresses are case-sensitive, so a case difference is a different address, not ours.
        /// The matched entry's coinKey is returned for logging/observability ONLY; it is never used
        /// as a gate (per the locked "exact address only" rule — an unverified coin-label string must
        /// not be able to false-block a money-out).
        /// </summary>
        public static AddressVerifyResult Evaluate(WCAddressListResponseModel resp, string address, out string matchedCoinKey)
        {
            matchedCoinKey = null;

            // Defensive: the caller's format check (LooksLikeTronAddress) already rejected an empty
            // address, so this is unreachable in the live path — but a blank target can never match a
            // real listed address, so treat it as NotListed rather than risk a send.
            if (string.IsNullOrEmpty(address)) return AddressVerifyResult.NotListed;

            // No usable answer -> fail closed.
            if (resp == null || !resp.success || resp.code != 200 || resp.data == null)
                return AddressVerifyResult.Unverifiable;

            // Trim the provider value too (we already trimmed ours): incidental surrounding
            // whitespace must not cause a false-block — that would needlessly pause funding. This
            // normalizes whitespace only; the address identity comparison stays exact/case-sensitive.
            var match = resp.data.FirstOrDefault(d =>
                d != null && string.Equals((d.address ?? "").Trim(), address, StringComparison.Ordinal));
            if (match == null) return AddressVerifyResult.NotListed;

            matchedCoinKey = match.coinKey;
            return AddressVerifyResult.Listed;
        }
    }
}
