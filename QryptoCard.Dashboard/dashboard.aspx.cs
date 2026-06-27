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
            //lblName.InnerHtml = SessionLib.Current.FirstName;
            getReferral();
            getDashboardData();
            getReferralList();
            bindWallet();
            getCard();

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

        void getDashboardData()
        {
            DashboardModal dt;

            var req = new UserReferralModel();
            OutputModel op = new OutputModel();
            op = us.getDashboardData();
            if (op.Status == "success")
            {
                dt = JsonConvert.DeserializeObject<DashboardModal>(op.Data.ToString());

                if (dt.CommissionRate == -1)
                    lblCommissionRate.InnerHtml = "not found";
                else
                    lblCommissionRate.InnerHtml = dt.CommissionRate.ToString() + "%";

                lblTotalCards.InnerHtml = dt.TotalCards.ToString();
                lblTotalCommission.InnerHtml = dt.TotalCommission.ToString() + " USDT";

            }
            else
            {

                //rptCard.DataSource = null;
                //rptCard.DataBind();
            }
        }

        void getReferralList()
        {
            OutputModel op = new OutputModel();
            op = us.getReferralJoined();
            if (op.Status == "success")
            {
                var dt = JsonConvert.DeserializeObject<List<UserModel>>(op.Data.ToString());
                if (dt.Count > 0)
                {
                    divnoreferral.Visible = false;
                    gvReferralList.DataSource = dt;
                    gvReferralList.DataBind();
                }
                else
                {
                    divnoreferral.Visible = true;
                    gvReferralList.DataSource = null;
                    gvReferralList.DataBind();
                }
            }
            else
            {
                divnoreferral.Visible = true;
                gvReferralList.DataSource = null;
                gvReferralList.DataBind();
            }
        }

        void getReferral()
        {
            UserReferralModel dt;

            var req = new UserReferralModel();
            OutputModel op = new OutputModel();
            op = us.getReferralCode(req);
            if (op.Status == "success")
            {
                dt = JsonConvert.DeserializeObject<UserReferralModel>(op.Data.ToString());
                hfReferralCode.Value = dt.Code;
                hfReferralLink.Value = KeyModel.REFERRAL_URL + dt.Code;

                txtReferralCode.Text = dt.Code;
                txtReferralLink.Text = hfReferralLink.Value;
            }
            else
            {

                //rptCard.DataSource = null;
                //rptCard.DataBind();
            }
        }


    }
}