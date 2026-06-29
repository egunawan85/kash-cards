using System;

namespace QryptoCard.API.Admin.Models.Service
{
    /// <summary>
    /// Request body for the admin card-refund endpoint (POST v1/admin/card/refund). The acting admin is
    /// taken from the bearer token, never from the body. <see cref="OrderId"/> is a card open-order id
    /// OR a top-up order id — both resolve to the same physical card, which is cancelled as a whole and
    /// its unused balance returned to the buyer (WasabiCard has no partial withdraw).
    /// </summary>
    public class RefundCardModel
    {
        public string OrderId { get; set; }
    }
}
