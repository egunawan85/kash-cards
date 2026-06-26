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

        }

        // Live wallet panel (S-F). Every figure shown here is server-returned; nothing is
        // computed client-side. Each read is isolated in its own try/catch so one failing (or
        // malformed-but-success) endpoint leaves its own panel at its default state instead of
        // throwing out of Page_Load and blanking every panel.
        void bindWallet()
        {
            try { getWalletBalance(); } catch { /* leave balance at the markup default ("—") */ }
            try { getDepositAddress(); } catch { viewDeposit.Visible = false; }
            try
            {
                bindLedger();
            }
            catch
            {
                divNoLedger.Visible = true;
                gvLedger.Visible = false;
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

        void getDepositAddress()
        {
            OutputModel op = us.getDepositAddress();
            if (op.Status == "success" && op.Data != null)
            {
                var dt = JsonConvert.DeserializeObject<DepositAddressModel>(op.Data.ToString());
                if (dt != null && !string.IsNullOrEmpty(dt.Address))
                {
                    txtDepositAddress.Text = dt.Address;
                    hfDepositAddress.Value = dt.Address;
                    imgDepositQR.ImageUrl = Common.GenerateQrDataUri(dt.Address);
                    imgDepositQR.Visible = true;
                    return;
                }
            }
            // No address yet (or read failed): hide the deposit block rather than show an empty box.
            viewDeposit.Visible = false;
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
                gvLedger.Visible = true;
                gvLedger.DataSource = dt.Items;
                gvLedger.DataBind();
                renderLedgerPager(dt.Page, dt.PageSize, dt.Total);
                return;
            }
            // Empty or failed: show the empty-state, hide grid + pager.
            divNoLedger.Visible = true;
            gvLedger.Visible = false;
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