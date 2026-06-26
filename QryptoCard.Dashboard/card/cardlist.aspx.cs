using Microsoft.Ajax.Utilities;
using Newtonsoft.Json;
using QryptoCard.Dashboard.Models;
using QryptoCard.Dashboard.Models.Service;
using QryptoCard.Dashboard.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace QryptoCard.Dashboard.card
{
    public partial class cardlist : System.Web.UI.Page
    {
        CardService cs = new CardService();
        protected void Page_Load(object sender, EventArgs e)
        {
            getData();
        }


        void getData()
        {
            List<CardTypeModel> dt;
            OutputModel op = new OutputModel();
            op = cs.getCardTypes();
            if (op.Status == "success")
            {
                dt = JsonConvert.DeserializeObject<List<CardTypeModel>>(op.Data.ToString());
                if (dt.Count > 0)
                {
                    for (int i = 0; i < dt.Count; i++)
                    {
                        if (dt[i].Organization == "Visa")
                            dt[i].LogoURL = "https://www.svgrepo.com/show/362035/visa-3.svg";
                        else if (dt[i].Organization == "MasterCard")
                            dt[i].LogoURL = "https://www.svgrepo.com/show/508703/mastercard.svg";
                        else
                            dt[i].LogoURL = "https://www.svgrepo.com/show/328132/discover.svg";

                        dt[i].ArtURL = ResolveCardArt(dt[i].Organization, dt[i].ArtURL);

                        dt[i].CardPrice = dt[i].CardPrice + " " + dt[i].CardPriceCurrency;
                        if (dt[i].NeedDepositForActiveCard == 1)
                            dt[i].NeedDeposit = "Yes";
                        else
                            dt[i].NeedDeposit = "No";

                        if (dt[i].NeedCardHolder == 1)
                        {
                            dt[i].TypeStr = "<img src=\"https://www.svgrepo.com/show/508402/apple-pay.svg\" alt=\"Chip\" width=\"40\">";
                            dt[i].Status = "<img src=\"https://www.svgrepo.com/show/508690/google-pay.svg\" alt=\"Chip\" width=\"40\">";
                        }
                        else
                        {
                            dt[i].TypeStr = "";
                            dt[i].Status = "";
                        }

                        dt[i].DetailURL = KeyModel.DETAIL_URL + dt[i].CardTypeId;

                        dt[i].RechargeFeeRate = dt[i].RechargeFeeRate + "%";
                        dt[i].BankCardBin = String.Format("{0:0000 0000 0000 0000}", (Int64.Parse(dt[i].BankCardBin + "0000000000")));
                    }
                    rptCard.DataSource = dt;
                    rptCard.DataBind();
                }
                else
                {

                    rptCard.DataSource = null;
                    rptCard.DataBind();
                }
            }
            else
            {
                // Load failed upstream — bind empty AND tell the user, rather than
                // silently presenting an empty card list as if none existed.
                rptCard.DataSource = null;
                rptCard.DataBind();
                ShowAlert(op.Message);
            }

        }

        // Per-card-type artwork (DD-7). Prefer an upstream-supplied ArtURL when the
        // card-type service provides one (a server-sourced value from our own API, not
        // client input); otherwise pick a vendored image by card scheme, with the
        // static brand card as the final fallback for an unmapped scheme.
        string ResolveCardArt(string organization, string upstream)
        {
            if (!string.IsNullOrWhiteSpace(upstream))
                return upstream;

            if (organization == "Visa")
                return ResolveUrl("~/Content/media/cards/visa.svg");
            if (organization == "MasterCard")
                return ResolveUrl("~/Content/media/cards/mastercard.svg");
            if (organization == "Discover")
                return ResolveUrl("~/Content/media/cards/discover.svg");

            return ResolveUrl("~/Content/media/card-bg.png");
        }

        // Surfaces a backend error message to the user. This screen has no styled
        // alert modal yet (added when it's re-skinned), so fall back to a client alert.
        void ShowAlert(string message)
        {
            if (string.IsNullOrEmpty(message))
                message = "Unable to load cards. Please try again.";
            string js = "alert('" + HttpUtility.JavaScriptStringEncode(message) + "');";
            ClientScript.RegisterStartupScript(GetType(), "cardListError", js, true);
        }
    }
}