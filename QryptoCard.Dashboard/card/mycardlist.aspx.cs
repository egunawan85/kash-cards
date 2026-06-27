using Newtonsoft.Json;
using QryptoCard.Dashboard.Models;
using QryptoCard.Dashboard.Models.Service;
using QryptoCard.Dashboard.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.HtmlControls;
using System.Web.UI.WebControls;

namespace QryptoCard.Dashboard.card
{
    public partial class mycardlist : System.Web.UI.Page
    {
        CardService cs = new CardService();
        UserService us = new UserService();
        protected void Page_Load(object sender, EventArgs e)
        {
            if (Common.checkID())
            {
                if (!IsPostBack) { getData(); getOrders(); }
            }
            else
            {
                if (Master.checkCookies())
                {
                    if (!IsPostBack) { getData(); getOrders(); }
                }
                else
                    Master.forceLogin();
            }
        }


        void getData()
        {
            setAvailBalance();
            List<CardModel> dt;
            var req = new CardModel();
            OutputModel op = new OutputModel();
            op = cs.getCardList(req);
            if (op.Status == "success")
            {
                dt = JsonConvert.DeserializeObject<List<CardModel>>(op.Data.ToString());
                litActiveCards.Text = dt.Count.ToString();
                if (dt.Count > 0)
                {
                    for (int i = 0; i < dt.Count; i++)
                    {
                        try
                        {
                            // Masked card-number shape — only the last 4 are real (the provider
                            // never returns a full PAN). Strip to digits and show the last 4 behind a
                            // full 16-digit bullet mask. The brand mark is rendered from Organization
                            // (CardBrandMark), so the old external logo-URL hotlinks are gone.
                            string digits = new string((dt[i].CardNumber ?? "").Where(char.IsDigit).ToArray());
                            string last4 = digits.Length >= 4 ? digits.Substring(digits.Length - 4) : digits;
                            dt[i].CardNumber = "•••• •••• •••• " + last4;

                            dt[i].DetailURL = KeyModel.DETAIL_OWN_URL + dt[i].ID;

                            dt[i].Param5 = dt[i].Param5 + " " + dt[i].Currency;
                        }
                        catch
                        {
                            // One malformed row must not blank the entire list — leave this
                            // row's raw values and keep rendering the rest.
                        }
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
                // Load failed upstream — bind empty AND surface the reason, rather than
                // silently presenting an empty grid that reads identically to "you have
                // no cards yet" (a silent failure the buy-card screen also guards against).
                rptCard.DataSource = null;
                rptCard.DataBind();
                ShowAlert(op.Message);
            }

        }

        // ====================================================================
        // Card orders (all states: active / pending-payment / cancelled / expired).
        // The user's card *purchase orders* with their status + the pay-link and
        // Cancel actions. Server-returned via getCardListAll; relocated here so the
        // Transactions page can be a pure spend feed.
        // ====================================================================
        void getOrders()
        {
            List<CardModel> dt;
            OutputModel op = new OutputModel();
            CardModel req = new CardModel();
            op = cs.getCardListAll(req);
            if (op.Status == "success")
            {
                dt = JsonConvert.DeserializeObject<List<CardModel>>(op.Data.ToString());
                litTotalCards.Text = dt.Count.ToString();
                if (dt.Count > 0)
                {
                    for (int i = 0; i < dt.Count; i++)
                    {
                        dt[i].FirstName = dt[i].FirstName + " " + dt[i].LastName;
                        dt[i].DetailURL = KeyModel.TXCARD_URL + dt[i].ID;
                    }
                    gvListItem.Visible = true;
                    gvListItem.DataSource = null;
                    gvListItem.DataSource = dt;
                    gvListItem.DataBind();
                    divnorow.Visible = false;
                }
                else
                {
                    gvListItem.DataSource = null;
                    gvListItem.DataBind();
                    gvListItem.Visible = false;
                    divnorow.Visible = true;
                }
            }
            else
            {
                gvListItem.DataSource = null;
                gvListItem.DataBind();
                gvListItem.Visible = false;
                divnorow.Visible = true;
            }
        }

        // Wallet USDT balance for the summary bar — same source/format as the dashboard's
        // "Available balance" (getBalance → tblM_User_Balance). Read-only.
        void setAvailBalance()
        {
            try
            {
                OutputModel op = us.getBalance(new UserBalanceModel());
                if (op.Status == "success" && op.Data != null)
                {
                    var b = JsonConvert.DeserializeObject<UserBalanceModel>(op.Data.ToString());
                    if (b != null && b.Balance.HasValue)
                        litAvailBal.Text = b.Balance.Value.ToString("0.00") + " USDT";
                }
            }
            catch { /* leave the markup default */ }
        }

        // Local brand mark for the 3D card, by card-type Organization. No external assets:
        // Visa/Discover are styled wordmarks, Mastercard the two interlocking discs (CSS).
        protected string CardBrandMark(string org)
        {
            org = (org ?? "").Trim();
            if (string.Equals(org, "MasterCard", StringComparison.OrdinalIgnoreCase))
                return "<span class=\"qcard-net mc\" title=\"Mastercard\"><i></i><i></i></span>";
            if (string.Equals(org, "Discover", StringComparison.OrdinalIgnoreCase))
                return "<span class=\"qcard-net disc\" title=\"Discover\">DISC<b>O</b>VER</span>";
            return "<span class=\"qcard-net visa\" title=\"Visa\">VISA</span>";
        }

        protected Boolean IsStatusCreated(string i) { return i == "created"; }
        protected Boolean IsStatusInProgress(string i) { return i == "in progress"; }
        protected Boolean IsStatusPaid(string i) { return i == "paid"; }
        protected Boolean IsStatusCreatedBadge(string i) { return i == "created"; }
        protected Boolean IsStatusCompleted(string i) { return i == "completed" || i == "success"; }
        protected Boolean IsStatusCancelled(string i) { return i == "cancelled"; }
        protected Boolean IsStatusExpired(string i) { return i == "expired"; }

        protected void gvListItem_PageIndexChanging(object sender, GridViewPageEventArgs e)
        {
            gvListItem.PageIndex = e.NewPageIndex;
            getOrders();
        }

        protected void gvListItem_RowCreated(object sender, GridViewRowEventArgs e)
        {
        }

        // A row's Cancel button posts back here; surface a SERVER-rendered confirm overlay
        // (Visible toggled in code) rather than a JS-shown modal — the shell loads no Bootstrap.
        protected void btnCancel_ServerClick(object sender, EventArgs e)
        {
            HtmlButton btn = (HtmlButton)sender;
            GridViewRow row = (GridViewRow)btn.NamingContainer;
            hfID.Value = ((HiddenField)row.FindControl("hfID")).Value;
            pnlCancelConfirm.Visible = true;
        }

        protected void btnCloseConfirm_Click(object sender, EventArgs e)
        {
            pnlCancelConfirm.Visible = false;
        }

        protected void btnCancelExec_Click(object sender, EventArgs e)
        {
            pnlCancelConfirm.Visible = false;

            CardModel z = new CardModel();
            z.ID = hfID.Value;

            var q = cs.cancelCardTransaction(z);
            if (q.Status == "success")
            {
                getData();
                getOrders();
                ShowBanner(Server.HtmlEncode(q.Message), true);
            }
            else
            {
                ShowBanner(Server.HtmlEncode(q.Message), false);
            }
        }

        // Server-rendered inline result banner (no Bootstrap/jQuery needed).
        void ShowBanner(string html, bool ok)
        {
            pnlMsg.Visible = true;
            pnlMsg.CssClass = "hist-banner " + (ok ? "ok" : "err");
            lblMsg.InnerHtml = html;
            string js = "(function(){var m=document.getElementById('" + pnlMsg.ClientID
                + "');if(m){m.scrollIntoView({behavior:'smooth',block:'center'});}})();";
            ScriptManager.RegisterClientScriptBlock(this, GetType(), "Pop", js, true);
        }

        // Surfaces a backend error inline, matching the NewDesign alert idiom used
        // across the re-skin, so an upstream failure is legible instead of looking
        // like an empty card list.
        void ShowAlert(string message)
        {
            pnlAlert.Visible = true;
            lblAlert.Text = Server.HtmlEncode(string.IsNullOrEmpty(message)
                ? "Unable to load your cards. Please try again."
                : message);
        }
    }
}
