using Newtonsoft.Json;
using QryptoCard.Dashboard.Models;
using QryptoCard.Dashboard.Models.Service;
using QryptoCard.Dashboard.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Services.Description;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace QryptoCard.Dashboard
{
    public partial class dashboard : System.Web.UI.Page
    {
        UserService us = new UserService();
        CardService cs = new CardService();
        protected void Page_Load(object sender, EventArgs e)
        {
            if (Common.checkID())
            {

                if (!IsPostBack)
                {
                    var dtn = DateTime.Today.ToString("yyyy/MM/dd");
                    bindData();
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
                        var dtn = DateTime.Today.ToString("yyyy/MM/dd");
                        bindData();
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

        void bindData()
        {
            getDashboardData();
            bindWallet();
            getCard();
            // Deposit-into-card UI (dark by default): only when CARD_FUNDING_UI_ENABLED does the balance
            // panel relabel to "Total card balance" (getCard) and the in-progress banner appear. While OFF
            // this page is byte-for-byte today's dashboard.
            if (KeyModel.CARD_FUNDING_UI_ENABLED) bindFundingBanner();
        }

        // "N cards being funded" banner — a link into the card list's In-progress section. Best-effort:
        // a failed/empty lookup simply shows nothing (never breaks the dashboard).
        void bindFundingBanner()
        {
            try
            {
                OutputModel op = cs.getFundingOpenIntents();
                if (op == null || op.Status != "success" || op.Data == null) return;
                var items = JsonConvert.DeserializeObject<List<FundingIntentModel>>(op.Data.ToString());
                int n = items != null ? items.Count : 0;
                if (n <= 0) return;
                string url = ResolveUrl("~/card/mycardlist");
                string label = n == 1 ? "1 card being funded" : (n + " cards being funded");
                litFundingBanner.Text =
                    "<section class=\"panel\" style=\"display:flex;align-items:center;justify-content:space-between;gap:12px\">"
                    + "<span style=\"color:var(--ink-2)\">" + Server.HtmlEncode(label) + "</span>"
                    + "<a class=\"btn btn-line\" href=\"" + Server.HtmlEncode(url) + "\">Track &rarr;</a></section>";
            }
            catch { /* no banner on failure */ }
        }

        // Live wallet panel (S-F). Every figure shown here is server-returned; nothing is
        // computed client-side. Each read is isolated in its own try/catch so one failing (or
        // malformed-but-success) endpoint leaves its own panel at its default state instead of
        // throwing out of Page_Load and blanking every panel.
        void bindWallet()
        {
            try { getWalletBalance(); } catch { /* leave balance at the markup default ("—") */ }
            try
            {
                bindLedger();
            }
            catch
            {
                divNoLedger.Visible = true;
                rptLedger.Visible = false;
                ledgerPager.Visible = false;
            }
        }

        void getWalletBalance()
        {
            OutputModel op = us.getBalance(new UserBalanceModel());
            if (op.Status == "success" && op.Data != null)
            {
                var dt = JsonConvert.DeserializeObject<UserBalanceModel>(op.Data.ToString());
                if (dt != null && dt.Balance.HasValue)
                    lblBalance.InnerHtml = dt.Balance.Value.ToString("0.00");
            }
            // On failure leave the markup default ("—"); never fabricate a balance.
        }

        void bindLedger()
        {
            const int pageSize = 10;
            int page = 1;
            int.TryParse(Request.QueryString["lpage"], out page);
            if (page < 1) page = 1;

            LedgerModel dt = readLedgerPage(page, pageSize);

            // A hand-typed ?lpage past the end returns an empty page even though history exists.
            // Re-bind the last real page instead of showing the empty-state.
            if (dt != null && dt.Total > 0 && (dt.Items == null || dt.Items.Count == 0))
            {
                int lastPage = (int)Math.Ceiling((double)dt.Total / pageSize);
                if (lastPage >= 1 && lastPage != page)
                {
                    page = lastPage;
                    dt = readLedgerPage(page, pageSize);
                }
            }

            if (dt != null && dt.Items != null && dt.Items.Count > 0)
            {
                divNoLedger.Visible = false;
                rptLedger.Visible = true;
                rptLedger.DataSource = dt.Items;
                rptLedger.DataBind();
                renderLedgerPager(dt.Page, dt.PageSize, dt.Total);
                return;
            }
            // Empty or failed: show the empty-state, hide the list + pager.
            divNoLedger.Visible = true;
            rptLedger.Visible = false;
            ledgerPager.Visible = false;
        }

        LedgerModel readLedgerPage(int page, int pageSize)
        {
            OutputModel op = us.getLedger(page, pageSize);
            if (op.Status == "success" && op.Data != null)
                return JsonConvert.DeserializeObject<LedgerModel>(op.Data.ToString());
            return null;
        }

        // Query-string pager (?lpage=N): plain links, so a page change is a GET that re-binds the
        // whole dashboard — never a postback that would blank the other (!IsPostBack-bound) panels.
        void renderLedgerPager(int page, int pageSize, int total)
        {
            int totalPages = (pageSize > 0) ? (int)Math.Ceiling((double)total / pageSize) : 1;
            if (totalPages <= 1)
            {
                ledgerPager.Visible = false;
                return;
            }
            if (page < 1) page = 1;
            if (page > totalPages) page = totalPages;

            string basePath = ResolveUrl("~/dashboard");
            var sb = new System.Text.StringBuilder();
            if (page > 1)
                sb.Append("<a class=\"btn btn-line\" href=\"" + basePath + "?lpage=" + (page - 1) + "#wallet-ledger\">Prev</a>");
            sb.Append("<span style=\"color: var(--ink-3); font-size: .9rem;\">Page " + page + " of " + totalPages + "</span>");
            if (page < totalPages)
                sb.Append("<a class=\"btn btn-line\" href=\"" + basePath + "?lpage=" + (page + 1) + "#wallet-ledger\">Next</a>");

            ledgerPager.InnerHtml = sb.ToString();
            ledgerPager.Visible = true;
        }

        // Local brand mark for the 3D card, by card-type Organization (no external assets):
        // Visa/Discover are styled wordmarks, Mastercard the two interlocking discs (CSS).
        string CardBrandMark(string org)
        {
            org = (org ?? "").Trim();
            if (string.Equals(org, "MasterCard", StringComparison.OrdinalIgnoreCase))
                return "<span class=\"qcard-net mc\" title=\"Mastercard\"><i></i><i></i></span>";
            if (string.Equals(org, "Discover", StringComparison.OrdinalIgnoreCase))
                return "<span class=\"qcard-net disc\" title=\"Discover\">DISC<b>O</b>VER</span>";
            return "<span class=\"qcard-net visa\" title=\"Visa\">VISA</span>";
        }

        // Card-at-a-glance (S-F). The card list is server-returned (getCardList). Only the
        // masked last-4 and the expiry are surfaced here — never the full PAN or the CVV.
        void getCard()
        {
            try
            {
                OutputModel op = cs.getCardList(new CardModel());
                if (op.Status == "success" && op.Data != null)
                {
                    var cards = JsonConvert.DeserializeObject<List<CardModel>>(op.Data.ToString());
                    // "Total card balance" = sum of owned-card balances (from the list already fetched
                    // here — no extra per-card call). Gated: while the UI is dark the wallet "Available
                    // balance" the markup already bound stays exactly as today.
                    ApplyTotalCardBalance(cards);
                    if (cards != null && cards.Count > 0)
                    {
                        // Prefer an active card; otherwise the first. "Manage" covers the rest.
                        CardModel card = cards.FirstOrDefault(c => c.isActive == 1) ?? cards[0];

                        lblCardNetwork.InnerHtml = CardBrandMark(card.Organization);

                        // CardNumber is already a masked PAN (e.g. "4024 00** **** 0001"); show only
                        // the trailing four digits, never the whole number.
                        string digits = new string((card.CardNumber ?? "").Where(char.IsDigit).ToArray());
                        string last4 = digits.Length >= 4 ? digits.Substring(digits.Length - 4) : digits;
                        lblCardLast4.InnerHtml = "&#8226;&#8226;&#8226;&#8226; &#8226;&#8226;&#8226;&#8226; &#8226;&#8226;&#8226;&#8226; " + Server.HtmlEncode(last4);

                        // Expiry is stored encrypted; decrypt only the expiry for display. The CVV is
                        // never decrypted or rendered on the dashboard.
                        string exp = "";
                        try { exp = Common.decrypt(card.ValidPeriod); } catch { exp = ""; }
                        lblCardExp.InnerText = string.IsNullOrEmpty(exp) ? "—" : exp;

                        lnkCardDetails.HRef = string.IsNullOrEmpty(card.ID)
                            ? ResolveUrl("~/card/mycardlist")
                            : ResolveUrl("~/card/mycarddetail?id=" + HttpUtility.UrlEncode(card.ID));

                        // The whole card is a click target to the detail page (earns the hand cursor
                        // + tilt). HRef is already a ResolveUrl'd app path; encode for the JS string.
                        card3dWrap.Attributes["onclick"] = "window.location.href='" + HttpUtility.JavaScriptStringEncode(lnkCardDetails.HRef) + "';";
                        card3dWrap.Style["cursor"] = "pointer";

                        viewCard.Visible = true;
                        viewNoCard.Visible = false;
                        return;
                    }
                }
            }
            catch { /* fall through to the no-card empty state */ }

            viewCard.Visible = false;
            viewNoCard.Visible = true;
        }

        // Relabel the balance panel to "Total card balance" = sum of the owned cards' balances (Param5
        // is the per-card balance the list already carries). Only when the deposit-into-card UI is on;
        // otherwise a no-op so the wallet balance already bound by getWalletBalance stands.
        void ApplyTotalCardBalance(List<CardModel> cards)
        {
            if (!KeyModel.CARD_FUNDING_UI_ENABLED) return;
            decimal total = QryptoCard.Sec.CardFundingDisplay.SumBalances(
                (cards ?? new List<CardModel>()).Select(c => c.Param5));
            lblBalance.InnerHtml = QryptoCard.Sec.CardFundingDisplay.FormatMoney(total);
            lblBalanceLab.InnerText = "Total card balance";
            lblBalanceCur.InnerText = "USD";
            // No wallet top-up in the new flow — point the primary action at getting/funding a card.
            lnkTopUp.HRef = ResolveUrl("~/card/cardlist");
            lnkTopUp.InnerText = "Get a card";
        }

        // ---- Wallet-ledger row rendering (S-F) -------------------------------------------
        // Direction is read from the balance movement (Balance vs BalancePrevious) rather than
        // the Amount sign, so it stays correct regardless of how the server signs Amount.
        protected bool IsLedgerCredit(object item)
        {
            var m = item as LedgerEntryModel;
            if (m == null) return true;
            if (m.Balance.HasValue && m.BalancePrevious.HasValue)
                return m.Balance.Value >= m.BalancePrevious.Value;
            if (m.Amount.HasValue)
                return m.Amount.Value >= 0;
            return true;
        }

        protected string LedgerType(object item)
        {
            var m = item as LedgerEntryModel;
            string t = m != null ? m.Type : null;
            return Server.HtmlEncode(string.IsNullOrEmpty(t) ? "Transaction" : t);
        }

        protected string LedgerWhen(object item)
        {
            var m = item as LedgerEntryModel;
            if (m != null && m.CreatedDate.HasValue)
                return m.CreatedDate.Value.ToString("dd MMM yyyy, HH:mm");
            return "";
        }

        protected string LedgerAmtClass(object item)
        {
            return IsLedgerCredit(item) ? "amt in" : "amt out";
        }

        protected string LedgerAmt(object item)
        {
            var m = item as LedgerEntryModel;
            decimal amt = (m != null && m.Amount.HasValue) ? Math.Abs(m.Amount.Value) : 0m;
            string sign = IsLedgerCredit(item) ? "+" : "-";
            return sign + amt.ToString("0.00") + " USDT";
        }

        // Total Cards stat. The two referral stats (commission rate + total commission) moved to
        // the Referrals tab (~/referrals).
        void getDashboardData()
        {
            OutputModel op = us.getDashboardData();
            if (op.Status == "success" && op.Data != null)
            {
                var dt = JsonConvert.DeserializeObject<DashboardModal>(op.Data.ToString());
                if (dt != null)
                    lblTotalCards.InnerHtml = dt.TotalCards.ToString();
            }
        }

        // Referral code/link, history, and commission stats now live on the Referrals tab
        // (referrals.aspx) — getReferral()/getReferralList() moved there.


    }
}