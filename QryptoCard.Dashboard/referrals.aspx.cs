using Newtonsoft.Json;
using QryptoCard.Dashboard.Models;
using QryptoCard.Dashboard.Models.Service;
using QryptoCard.Dashboard.Services;
using System;
using System.Collections.Generic;
using System.Web.UI.WebControls;

namespace QryptoCard.Dashboard
{
    public partial class referrals : System.Web.UI.Page
    {
        UserService us = new UserService();

        protected void Page_Load(object sender, EventArgs e)
        {
            if (Common.checkID())
            {
                if (!IsPostBack) bindData();
            }
            else
            {
                if (Master.checkCookies())
                {
                    if (!IsPostBack) bindData();
                }
                else
                    Master.forceLogin();
            }
        }

        void bindData()
        {
            getReferral();
            getDashboardData();
            getReferralList();
            // Deposit-into-card: the internal wallet resurfaces here (only) as a display-only
            // "Available balance" (commissions + refunds + deposit residual), withdrawable soon. Dark by
            // default — the referrals page is unchanged until CARD_FUNDING_UI_ENABLED is set.
            if (KeyModel.CARD_FUNDING_UI_ENABLED) getAvailableBalance();
        }

        // Display-only available balance = the internal wallet (getBalance -> tblM_User_Balance). Shown
        // on the referrals tab now that the dashboard "Available balance" became "Total card balance".
        void getAvailableBalance()
        {
            try
            {
                OutputModel op = us.getBalance(new UserBalanceModel());
                if (op.Status == "success" && op.Data != null)
                {
                    var b = JsonConvert.DeserializeObject<UserBalanceModel>(op.Data.ToString());
                    if (b != null && b.Balance.HasValue)
                    {
                        lblAvailBalance.Text = b.Balance.Value.ToString("0.00") + " USDT";
                        pnlAvailBal.Visible = true;
                    }
                }
            }
            catch { /* leave hidden on failure */ }
        }

        // Referral code + link. The visible fields are read-only; the canonical value is mirrored
        // into a hidden field that the client-side copy reads.
        void getReferral()
        {
            try
            {
                OutputModel op = us.getReferralCode(new UserReferralModel());
                if (op.Status == "success" && op.Data != null)
                {
                    var dt = JsonConvert.DeserializeObject<UserReferralModel>(op.Data.ToString());
                    if (dt != null)
                    {
                        hfReferralCode.Value = dt.Code;
                        hfReferralLink.Value = KeyModel.REFERRAL_URL + dt.Code;
                        txtReferralCode.Text = dt.Code;
                        txtReferralLink.Text = hfReferralLink.Value;
                    }
                }
            }
            catch { /* leave the fields blank on failure */ }
        }

        // The two referral stats moved off the dashboard: commission rate + total commission earned.
        void getDashboardData()
        {
            try
            {
                OutputModel op = us.getDashboardData();
                if (op.Status == "success" && op.Data != null)
                {
                    var dt = JsonConvert.DeserializeObject<DashboardModal>(op.Data.ToString());
                    if (dt != null)
                    {
                        lblCommissionRate.Text = dt.CommissionRate == -1 ? "not found" : dt.CommissionRate.ToString() + "%";
                        lblTotalCommission.Text = dt.TotalCommission.ToString() + " USDT";
                    }
                }
            }
            catch { /* leave the stats at their markup default */ }
        }

        // Referral history (users who joined via this user's link) + the total-referrals count.
        void getReferralList()
        {
            ReferralTabModel data = null;
            try
            {
                OutputModel op = us.getReferralJoined();
                if (op.Status == "success" && op.Data != null)
                    data = JsonConvert.DeserializeObject<ReferralTabModel>(op.Data.ToString());
            }
            catch { /* leave both lists empty */ }

            var referrals = (data != null && data.Referrals != null) ? data.Referrals : new List<ReferralBreakdownModel>();
            var commissions = (data != null && data.Commissions != null) ? data.Commissions : new List<CommissionHistoryModel>();

            lblTotalReferrals.Text = referrals.Count.ToString();

            if (referrals.Count > 0)
            {
                divnoreferral.Visible = false;
                gvReferralList.DataSource = referrals;
                gvReferralList.DataBind();
            }
            else
            {
                divnoreferral.Visible = true;
                gvReferralList.DataSource = null;
                gvReferralList.DataBind();
            }

            if (commissions.Count > 0)
            {
                divnocommission.Visible = false;
                gvCommissionList.DataSource = commissions;
                gvCommissionList.DataBind();
            }
            else
            {
                divnocommission.Visible = true;
                gvCommissionList.DataSource = null;
                gvCommissionList.DataBind();
            }
        }

        protected void gvReferralList_PageIndexChanging(object sender, GridViewPageEventArgs e)
        {
            gvReferralList.PageIndex = e.NewPageIndex;
            getReferralList();
        }

        // ---- Referral history cell formatters (bound in referrals.aspx) ----
        protected string RefName(object item)
        {
            var m = item as ReferralBreakdownModel;
            if (m == null) return "";
            // Most users register with email only (no name), which left this column blank. Fall back
            // to the always-present email so every referral row is identifiable.
            string name = ((m.FirstName ?? "") + " " + (m.LastName ?? "")).Trim();
            return Server.HtmlEncode(name.Length > 0 ? name : (m.Email ?? "").Trim());
        }

        protected string RefJoined(object item)
        {
            var m = item as ReferralBreakdownModel;
            if (m != null && m.DateJoin.HasValue) return m.DateJoin.Value.ToString("dd MMM yyyy");
            return "";
        }

        protected string RefEarned(object item)
        {
            var m = item as ReferralBreakdownModel;
            double e = m != null ? m.Earned : 0;
            return e.ToString("0.00") + " USDT";
        }

        // "Active" once a referee has converted (bought a card) or earned the caller commission;
        // "Invited" while they've joined but done neither yet.
        protected string RefStatus(object item)
        {
            var m = item as ReferralBreakdownModel;
            bool active = m != null && (m.Converted || m.Earned > 0);
            return active
                ? "<span class=\"badge badge-light-success\">Active</span>"
                : "<span class=\"badge badge-light-info\">Invited</span>";
        }

        protected void gvCommissionList_PageIndexChanging(object sender, GridViewPageEventArgs e)
        {
            gvCommissionList.PageIndex = e.NewPageIndex;
            getReferralList();
        }

        // ---- Commission history cell formatters ----
        protected string CommWhen(object item)
        {
            var m = item as CommissionHistoryModel;
            if (m != null && m.DateCreated.HasValue) return m.DateCreated.Value.ToString("dd MMM yyyy");
            return "";
        }

        protected string CommReferral(object item)
        {
            var m = item as CommissionHistoryModel;
            if (m == null) return "";
            // Fall back to the referee's email when they never set a name (see RefName).
            string name = (m.RefereeName ?? "").Trim();
            return Server.HtmlEncode(name.Length > 0 ? name : (m.RefereeEmail ?? "").Trim());
        }

        protected string CommAmount(object item)
        {
            var m = item as CommissionHistoryModel;
            double a = (m != null && m.Commission.HasValue) ? m.Commission.Value : 0;
            return "+" + a.ToString("0.00") + " USDT";
        }
    }
}
