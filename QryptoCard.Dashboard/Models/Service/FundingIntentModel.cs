using System;

namespace QryptoCard.Dashboard.Models.Service
{
    // Deposit-into-card funding intent, as returned by the app funding endpoints.
    //
    //   create/topup (/v1/card/funding/{intent,topup}) populate the pricing snapshot + the invoice's
    //   unique deposit address (the address-first funding screen renders these):
    //       IntentID, DepositAddress, Network, Coin, Face, Price, PercentageFee, FixedFee, ExpectedTotal, Status
    //
    //   status (/v1/card/funding/status) returns the live lifecycle for the tracker + poller:
    //       IntentID, Kind, Status, ExpectedTotal, ReceivedTotal, CardNo
    //
    // One model covers both shapes; a field the current call doesn't set stays null.
    public class FundingIntentModel
    {
        public string IntentID { get; set; }

        // Create/topup pricing snapshot + invoice address.
        public string DepositAddress { get; set; }
        public string Network { get; set; }
        public string Coin { get; set; }
        public Nullable<decimal> Face { get; set; }           // amount that lands on the card
        public Nullable<decimal> Price { get; set; }          // card price (new-card only)
        public Nullable<decimal> PercentageFee { get; set; }  // our % fee amount
        public Nullable<decimal> FixedFee { get; set; }       // flat network / fixed deposit fee
        public Nullable<decimal> ExpectedTotal { get; set; }  // exact amount to send

        // Status shape.
        public string Kind { get; set; }                      // "new" | "topup"
        public string Status { get; set; }                    // Pending|Funding|Confirming|Issuing|Completed|Expired|Cancelled|Failed
        public Nullable<decimal> ReceivedTotal { get; set; }  // credited-so-far (X of Y)
        public string CardNo { get; set; }                    // bound once Completed
    }
}
