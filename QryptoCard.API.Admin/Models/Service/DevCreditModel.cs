using System;

namespace QryptoCard.API.Admin.Models.Service
{
    /// <summary>
    /// Request body for the dev-only wallet test-credit endpoint
    /// (POST v1/admin/dev/credit). The acting admin is taken from the bearer token,
    /// never from the body. <see cref="Reference"/> is an optional idempotency key:
    /// repeating a call with the same reference dedupes instead of double-crediting.
    /// </summary>
    public class DevCreditModel
    {
        public string UserId { get; set; }
        public decimal Amount { get; set; }
        public string Reference { get; set; }
    }
}
