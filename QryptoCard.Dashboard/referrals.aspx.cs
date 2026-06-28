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
            int count = 0;
            List<UserModel> dt = null;
            try
            {
                OutputModel op = us.getReferralJoined();
                if (op.Status == "success" && op.Data != null)
                {
                    dt = JsonConvert.DeserializeObject<List<UserModel>>(op.Data.ToString());
                    count = dt != null ? dt.Count : 0;
                }
            }
            catch { /* count stays 0 */ }

            lblTotalReferrals.Text = count.ToString();

            if (count > 0)
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

        protected void gvReferralList_PageIndexChanging(object sender, GridViewPageEventArgs e)
        {
            gvReferralList.PageIndex = e.NewPageIndex;
            getReferralList();
        }
    }
}
