using Newtonsoft.Json;
using QryptoCard.Dashboard.Models;
using QryptoCard.Dashboard.Models.Service;
using QryptoCard.Dashboard.Services;
using System;
using System.Web;
using System.Web.UI;

namespace QryptoCard.Dashboard
{
    // Wallet deposit view (S-F). Shows the user's single reusable USDT (TRC20) deposit address +
    // QR and their live balance. The per-card deposit-address flow is retired: top-ups now pay
    // from the wallet balance, so this page no longer takes a transaction id.
    public partial class txdeposit : System.Web.UI.Page
    {
        UserService us = new UserService();

        protected void Page_Load(object sender, EventArgs e)
        {
            // Standalone page (no master): mirror the original txdeposit auth guard — bounce to
            // the dashboard (which enforces login) when there's no authenticated session.
            if (Common.checkID())
            {
                if (!IsPostBack)
                    bindData();
            }
            else
            {
                Response.Redirect("~/dashboard");
            }
        }

        void bindData()
        {
            // Each read is isolated: a failure (or a malformed-but-success payload that fails to
            // deserialize) leaves its own section at its default state instead of failing the page.
            try { getBalance(); } catch { /* leave balance at the markup default ("—") */ }
            try
            {
                getDepositAddress();
            }
            catch
            {
                viewDeposit.Visible = false;
                viewNoAddress.Visible = true;
            }
        }

        void getBalance()
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
                    lbladdress.InnerText = dt.Address;
                    hfAddress.Value = dt.Address;
                    imgQR.ImageUrl = Common.GenerateQrDataUri(dt.Address);
                    imgQR.Visible = true;
                    return;
                }
            }
            // No address available (or the read failed): show a clear message instead of an
            // empty address box, and surface the backend reason when there is one.
            viewDeposit.Visible = false;
            viewNoAddress.Visible = true;
            if (!string.IsNullOrEmpty(op.Message) && op.Status != "success")
                lblNoAddress.InnerText = op.Message;
        }
    }
}
