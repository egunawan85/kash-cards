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
using System.Windows.Controls.Primitives;

namespace QryptoCard.Dashboard.card
{
    public partial class carddetail : System.Web.UI.Page
    {
        CardService cs = new CardService();
        protected void Page_Load(object sender, EventArgs e)
        {
            // Per-page-load idempotency key for the buy: the same rendered Buy button — re-clicked,
            // double-posted, or resubmitted via Back — carries this same UserReferenceID, so the INT
            // tier collapses the duplicates onto one order (filtered unique index on
            // (UserID, UserReferenceID)). A fresh key is minted only on a new page load (or after a
            // confirmed spend error, see btnBuy), so a genuine re-purchase still gets its own order.
            if (!IsPostBack)
                hfBuyRef.Value = Guid.NewGuid().ToString("N");

            if (Common.checkID())
            {

                if (!IsPostBack)
                {
                    string id = Request.QueryString["id"];
                    if (id == null || id == "")
                    {
                        Response.Redirect("~/card/cardlist");
                    }
                    else
                    {
                        getData(id);
                    }
                }
                else
                {
                    //bindData("00000000000000000001");
                }
            }
            else
            {
                if (Master.checkCookies())
                {
                    if (!IsPostBack)
                    {

                        string id = Request.QueryString["id"];
                        if (id == null || id == "")
                        {
                            Response.Redirect("~/card/cardlist");
                        }
                        else
                        {
                            getData(id);
                        }
                    }
                    else
                    {
                        //bindData("00000000000000000001");
                    }
                }
                else
                    Master.forceLogin();
            }
            
        }


        void getData(string id)
        {
            CardTypeModel dt;

            var req = new CardTypeModel();
            req.CardTypeId = Convert.ToInt32(id);
            OutputModel op = new OutputModel();
            op = cs.getCardTypesByID(req);
            if (op.Status == "success")
            {
                dt = JsonConvert.DeserializeObject<CardTypeModel>(op.Data.ToString());
                hfCardData.Value = JsonConvert.SerializeObject(dt, Formatting.None);

                lblCardPrice.InnerHtml = dt.CardPrice + " " + dt.CardPriceCurrency;
                // BankCardBin may be null/blank or non-numeric — only reformat when it parses,
                // else show the raw value rather than throwing FormatException.
                long cardBinVal;
                lblCardBin.InnerHtml = Int64.TryParse((dt.BankCardBin ?? "") + "0000000000", out cardBinVal)
                    ? String.Format("{0:0000 0000 0000 0000}", cardBinVal)
                    : (dt.BankCardBin ?? "");
                lblDepositFeeRate.InnerHtml = dt.RechargeFeeRate + "%";
                lblDepositLimit.InnerHtml = (dt.DepositAmountMinQuotaForActiveCard ?? "") + " - " + dt.DepositAmountMaxQuotaForActiveCard + " " + dt.FiatCurrency;
                if (dt.Organization == "Visa")
                    imgOrg.Src = "https://www.svgrepo.com/show/362035/visa-3.svg";
                else if (dt.Organization == "MasterCard")
                    imgOrg.Src = "https://www.svgrepo.com/show/508703/mastercard.svg";
                else
                    imgOrg.Src = "https://www.svgrepo.com/show/328132/discover.svg";

                lblUsage.InnerHtml = (dt.CardDesc ?? "").Replace(",", ", ");
                lblDepositFee.InnerHtml = "Deposit Fee : " + dt.RechargeFeeRate + "%";
                lblCardFee.InnerHtml = "Card Fee : " + dt.CardPrice + " " + dt.CardPriceCurrency;
                

                lblMinDeposit.InnerHtml = "Minimum Deposit : " + dt.DepositAmountMinQuotaForActiveCard + " " + dt.FiatCurrency;
                lblMaxDeposit.InnerHtml = "Maximum Deposit : " + dt.DepositAmountMaxQuotaForActiveCard + " " + dt.FiatCurrency;
                lblMaxCardPurchase.InnerHtml = "Maximum Card Purchase Quantity : " + dt.MaxCount;
                lblInitialDepositRequired.InnerHtml = "Initial Deposit Required : Yes";
                lblMinInitialDepositAmount.InnerHtml = "Minimum Initial Deposit Amount : " + dt.DepositAmountMinQuotaForActiveCard + " " + dt.FiatCurrency;

                hfMinDeposit.Value = dt.DepositAmountMinQuotaForActiveCard;
                hfMaxDeposit.Value = dt.DepositAmountMaxQuotaForActiveCard;

                calculateDeposit();

                //txtDepositAmount.Text = dt.DepositAmountMaxQuotaForActiveCard;
                //lblCardFeeX.InnerHtml = dt.CardPrice + " " + dt.CardPriceCurrency;
                //lblMinDepositX.InnerHtml = dt.DepositAmountMinQuotaForActiveCard + " " + dt.FiatCurrency;
                //lblDepositFeeRateX.InnerHtml = dt.RechargeFeeRate + "%";
                //lblDepositFeeX.InnerHtml = ((Convert.ToDouble(dt.RechargeFeeRate) / 100) * Convert.ToDouble(dt.DepositAmountMinQuotaForActiveCard)).ToString() + " " + dt.FiatCurrency;
                //lblTotalX.InnerHtml = (((Convert.ToDouble(dt.RechargeFeeRate) / 100) * Convert.ToDouble(dt.DepositAmountMinQuotaForActiveCard)) + Convert.ToDouble(dt.CardPrice) + Convert.ToDouble(dt.DepositAmountMinQuotaForActiveCard)).ToString() + " " + dt.FiatCurrency;

                //btnBuyX.Text = "Buy Card (" + lblTotalX.InnerHtml + ")";

                hfCardTypeID.Value = dt.CardTypeId.ToString();

                hfIsHolderNeeded.Value = dt.NeedCardHolder.ToString();
                if (dt.NeedCardHolder == 1)
                {
                    icapay.Visible = true;
                    icgpay.Visible = true;
                    viewrc20.Visible = false;
                    viewCardholder.Visible = true;
                    checkHolder(dt.CardTypeId.ToString());
                }
                else
                {
                    icapay.Visible = false;
                    icgpay.Visible = false;
                    viewrc30.Visible = false;
                }
            }
            else
            {
                // Load failed upstream — surface the reason in the existing alert
                // modal instead of leaving the page showing "0 USD" with no feedback.
                lblalert.InnerHtml = string.IsNullOrEmpty(op.Message)
                    ? "Unable to load card details. Please try again."
                    : Server.HtmlEncode(op.Message);
                ShowBuyAlertInline();
            }

        }

        void enableButton()
        {
            btnBuyX.Enabled = true;
        }

        // Show the purchase/validation result in an inline banner next to the Buy button. The
        // content is already set on lblalert (kept for the #alertModal path); mirror it into the
        // always-visible inline panel and scroll it into view. The Bootstrap modal's show() is a
        // no-op when the NewDesign shell hasn't loaded Bootstrap JS, which is why results used to
        // fall to the page bottom and users re-clicked Buy (the observed double-submit). The
        // isModalAlert flag is still set for shells where the modal does fire.
        void ShowBuyAlertInline()
        {
            pnlBuyMsg.Visible = true;
            lblBuyMsg.InnerHtml = lblalert.InnerHtml;
            string js = "var isModalAlert = true; (function(){var m=document.getElementById('"
                + pnlBuyMsg.ClientID
                + "'); if(m){m.scrollIntoView({behavior:'smooth',block:'center'});}})();";
            ScriptManager.RegisterClientScriptBlock(this, GetType(), "Pop", js, true);
        }

        void checkHolder(string id)
        {
            CardholderModel dt;

            var req = new CardholderModel();
            req.CardTypeId = Convert.ToInt64(id);
            OutputModel op = new OutputModel();
            op = cs.checkHolder(req);
            if (op.Status == "success")
            {
                dt = JsonConvert.DeserializeObject<CardholderModel>(op.Data.ToString());

                lbtNewHolder.Visible = true;

                txtFirstName.Value = dt.FirstName;
                txtFirstName.Disabled = true;

                txtLastName.Value = dt.LastName;
                txtLastName.Disabled = true;

                txtEmail.Value = dt.Email;
                txtEmail.Disabled = true;

                hfHolderID.Value = dt.HolderID.ToString();
            }
            else
            {
                lbtNewHolder.Visible = false;
                hfHolderID.Value = "";
                //rptCard.DataSource = null;
                //rptCard.DataBind();
            }
        }

        protected void btnBuy_ServerClick(object sender, EventArgs e)
        {
            CardModel req = new CardModel();

            if (hfIsHolderNeeded.Value == "1")
            {
                if (txtFirstName.Value == "")
                {
                    enableButton();
                    lblalert.InnerHtml = "First name cannot be empty";
                    ShowBuyAlertInline();
                    return;
                }
                if (txtLastName.Value == "")
                {
                    enableButton();
                    lblalert.InnerHtml = "Last name cannot be empty";
                    ShowBuyAlertInline();
                    return;
                }
                if (txtEmail.Value == "")
                {
                    enableButton();
                    lblalert.InnerHtml = "Email name cannot be empty";
                    ShowBuyAlertInline();
                    return;
                }

                if (hfHolderID.Value != "")
                    req.HolderID = Convert.ToInt64(hfHolderID.Value);

                req.Param1 = txtFirstName.Value;
                req.Param2 = txtLastName.Value;
                req.Param3 = txtEmail.Value;
            }

            //TransactionModel uw = new TransactionModel();
            //uw.MerchantID = ddlMerchantAdd.SelectedValue.Trim();
            //uw.CoinID = ddlCoinAdd.SelectedValue.ToLower().Trim();
            //uw.isToken = isToken;
            //if (uw.isToken == 1)
            //{
            //    uw.CoinID = ddlNetworkAdd.SelectedValue.ToLower().Trim();
            //    uw.TokenID = ddlTokenAdd.SelectedValue.Trim();
            //}
            //uw.Amount = Convert.ToDecimal(txtAmountAdd.Text);
            //uw.Description = txtDescAdd.Text.Trim();

            if (txtDepositAmount.Text == "")
            {
                enableButton();
                lblalert.InnerHtml = "Deposit amount cannot be empty";
                ShowBuyAlertInline();
                return;
            }

            var amt = Convert.ToDouble(txtDepositAmount.Text);
            var min = Convert.ToDouble(hfMinDeposit.Value);
            var max = Convert.ToDouble(hfMaxDeposit.Value);
            if (amt < min)
            {
                enableButton();
                lblalert.InnerHtml = "Minimum initial deposit amount is " + hfMinDeposit.Value + " USD";
                ShowBuyAlertInline();
                return;
            }
            if (amt > max)
            {
                enableButton();
                lblalert.InnerHtml = "Maximum initial deposit amount is " + hfMaxDeposit.Value + " USD";
                ShowBuyAlertInline();
                return;
            }


            OutputModel op = new OutputModel();
            req.CardTypeId = Convert.ToInt64(hfCardTypeID.Value);
            req.InitialDeposit = amt;
            req.UserReferenceID = hfBuyRef.Value;
            op = cs.openCard(req);
            if (op.Status == "success")
            {
                enableButton();
                //lblSuccess.Text = op.Message;
                //divsuccess.Visible = true;
                var dt = JsonConvert.DeserializeObject<CardModel>(op.Data.ToString());
                Response.Redirect("~/txcard?id=" + dt.ID);
            }
            else
            {
                enableButton();
                // Re-mint the idempotency key ONLY on a CONFIRMED decline ("failed") so a retry is a
                // genuinely new order. On "error" the response was lost/ambiguous (CardService maps a
                // timeout / transport failure to "error") and the INT tier may have ALREADY committed
                // the order + debit — so RETAIN the key, letting a retry idempotently REPLAY the
                // original (dup-key -> the original order if it committed, a fresh insert if it did not).
                if (op.Status == "failed")
                    hfBuyRef.Value = Guid.NewGuid().ToString("N");
                lblalert.InnerHtml = BuildSpendError(op.Message);
                ShowBuyAlertInline();
                return;
            }
        }

        // Surface the server's spend-failure message. The charge is server-authoritative (we send
        // only CardTypeId + InitialDeposit), so this only reports the outcome. When the wallet
        // balance is short, add a direct CTA to top up — the per-card deposit-address flow is retired.
        string BuildSpendError(string message)
        {
            if (string.IsNullOrEmpty(message))
                message = "Unable to complete the purchase. Please try again.";
            bool insufficient = message.IndexOf("insufficient balance", StringComparison.OrdinalIgnoreCase) >= 0;
            string html = HttpUtility.HtmlEncode(message);
            if (insufficient)
                html += "<br /><a class=\"btn btn-cyan\" style=\"margin-top:10px;\" href=\"" + ResolveUrl("~/txdeposit") + "\">Add funds</a>";
            return html;
        }

        protected void lbtNewHolder_Click(object sender, EventArgs e)
        {
            txtFirstName.Value = "";
            txtFirstName.Disabled = false;

            txtLastName.Value = "";
            txtLastName.Disabled = false;

            txtEmail.Value = "";
            txtEmail.Disabled = false;

            lbtNewHolder.Visible = false;

            hfHolderID.Value = "";
        }

        protected void rc20_ServerClick(object sender, EventArgs e)
        {
            txtDepositAmount.Text = "20";
            calculateDeposit();
        }

        protected void rc30_ServerClick(object sender, EventArgs e)
        {
            txtDepositAmount.Text = "30";
            calculateDeposit();
        }

        protected void rc50_ServerClick(object sender, EventArgs e)
        {
            txtDepositAmount.Text = "50";
            calculateDeposit();
        }

        protected void rc100_ServerClick(object sender, EventArgs e)
        {
            txtDepositAmount.Text = "100";
            calculateDeposit();
        }

        protected void rc200_ServerClick(object sender, EventArgs e)
        {
            txtDepositAmount.Text = "200";
            calculateDeposit();
        }

        protected void txtDepositAmount_TextChanged(object sender, EventArgs e)
        {
            calculateDeposit();
        }
        void calculateDeposit()
        {
            var dt = JsonConvert.DeserializeObject<CardTypeModel>(hfCardData.Value);
            if (txtDepositAmount.Text != "")
            {
                lblCardFeeX.InnerHtml = dt.CardPrice + " " + dt.CardPriceCurrency;
                lblMinDepositX.InnerHtml = txtDepositAmount.Text + " " + dt.FiatCurrency;
                lblDepositFeeRateX.InnerHtml = dt.RechargeFeeRate + "%";
                lblDepositFeeX.InnerHtml = ((Convert.ToDouble(dt.RechargeFeeRate) / 100) * Convert.ToDouble(txtDepositAmount.Text)).ToString() + " " + dt.FiatCurrency;
                lblTotalX.InnerHtml = (((Convert.ToDouble(dt.RechargeFeeRate) / 100) * Convert.ToDouble(txtDepositAmount.Text)) + Convert.ToDouble(dt.CardPrice) + Convert.ToDouble(txtDepositAmount.Text)).ToString() + " " + dt.FiatCurrency;

                btnBuyX.Text = "Buy Card (" + lblTotalX.InnerHtml + ")";
            }
            else
            {
                txtDepositAmount.Text = dt.DepositAmountMinQuotaForActiveCard;
                lblCardFeeX.InnerHtml = dt.CardPrice + " " + dt.CardPriceCurrency;
                lblMinDepositX.InnerHtml = txtDepositAmount.Text + " " + dt.FiatCurrency;
                lblDepositFeeRateX.InnerHtml = dt.RechargeFeeRate + "%";
                lblDepositFeeX.InnerHtml = ((Convert.ToDouble(dt.RechargeFeeRate) / 100) * Convert.ToDouble(txtDepositAmount.Text)).ToString() + " " + dt.FiatCurrency;
                lblTotalX.InnerHtml = (((Convert.ToDouble(dt.RechargeFeeRate) / 100) * Convert.ToDouble(txtDepositAmount.Text)) + Convert.ToDouble(dt.CardPrice) + Convert.ToDouble(txtDepositAmount.Text)).ToString() + " " + dt.FiatCurrency;

                btnBuyX.Text = "Buy Card (" + lblTotalX.InnerHtml + ")";
            }
        }

        // Card background artwork (DD-7). Derived from the persisted card data so it
        // survives the page's auto-postbacks (deposit amount / quick-amount buttons),
        // rather than only being correct on the initial load. Falls back to the static
        // brand card whenever the type or its scheme is unknown.
        //
        // Security note: hfCardData round-trips through a client hidden field, so its
        // contents are attacker-controllable on postback and this value is emitted into
        // a CSS url(...). We therefore key the artwork ONLY off the card scheme, which
        // can do nothing but select among a fixed set of vetted, app-relative asset
        // paths — never a free-form URL carried in the posted-back data.
        protected string CardArtUrl()
        {
            try
            {
                if (string.IsNullOrEmpty(hfCardData.Value))
                    return ResolveUrl("~/Content/media/card-bg.png");

                var dt = JsonConvert.DeserializeObject<CardTypeModel>(hfCardData.Value);
                return ResolveCardArt(dt.Organization);
            }
            catch
            {
                return ResolveUrl("~/Content/media/card-bg.png");
            }
        }

        // Pick a vendored image by card scheme, with the static brand card as the final
        // fallback for an unmapped scheme. Returns only fixed, app-relative paths.
        string ResolveCardArt(string organization)
        {
            if (organization == "Visa")
                return ResolveUrl("~/Content/media/cards/visa.svg");
            if (organization == "MasterCard")
                return ResolveUrl("~/Content/media/cards/mastercard.svg");
            if (organization == "Discover")
                return ResolveUrl("~/Content/media/cards/discover.svg");

            return ResolveUrl("~/Content/media/card-bg.png");
        }
    }
}