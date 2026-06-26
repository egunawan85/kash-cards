using Newtonsoft.Json;
using QRCoder;
using QryptoCard.Dashboard.Models.Service;
using QryptoCard.Dashboard.Services;
using System;
using System.Collections.Generic;
using System.Drawing.Imaging;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using QryptoCard.Dashboard.Models;

namespace QryptoCard.Dashboard
{
    public partial class txdeposit : System.Web.UI.Page
    {
        CardService cs = new CardService();

        protected void Page_Load(object sender, EventArgs e)
        {
            if (Common.checkID())
            {

                if (!IsPostBack)
                {
                    string id = Request.QueryString["id"];
                    if (id == null || id == "")
                    {
                        Response.Redirect("~/card/mycardlist");
                    }
                    else
                    {
                        getData(id);
                    }
                }
                else
                {
                    getCounter();
                }
            }
            else
            {
                Response.Redirect("~/dashboard");
            }
        }

        void getData(string id)
        {
            CardDepositModel dt;

            var req = new CardDepositModel();
            req.ID = id;
            OutputModel op = new OutputModel();
            op = cs.depositCardDetail(req);
            if (op.Status == "success")
            {
                dt = JsonConvert.DeserializeObject<CardDepositModel>(op.Data.ToString());
                //if (dt.Organization == "Visa")
                //    imgOrg.Src = "https://www.svgrepo.com/show/362035/visa-3.svg";
                //else if (dt.Organization == "MasterCard")
                //    imgOrg.Src = "https://www.svgrepo.com/show/508703/mastercard.svg";
                //else
                //    imgOrg.Src = "https://www.svgrepo.com/show/328132/discover.svg";

                //if (dt.HolderID != null)
                //    lblCardname.InnerHtml = dt.FirstName + " " + dt.LastName;

                //hfDepositFeeRate.Value = dt.RechargeFeeRate;

                //lblCardBalance.InnerHtml = dt.Param5 + " " + dt.Currency;

                //hfCardNumber.Value = dt.CardNumber;
                //hfCVV.Value = dt.CVV;
                //hfExpDate.Value = dt.ValidPeriod;
                //if (dt.HolderID != null)
                //    hfHolderID.Value = dt.HolderID.ToString();

                //getCardType(dt.CardTypeId.ToString());
                //getDepositList();

                lblTraID.InnerHtml = id;

                lblDeposit.InnerHtml = dt.Amount.ToString() + " " + dt.Currency;
                lblDepositFee.InnerHtml = dt.Fee.ToString() + " " + dt.Currency;
                lblTotalPay.InnerHtml = dt.Total.ToString() + " " + dt.Currency;
                lblTotal.InnerHtml = dt.Total.ToString() + " USDT";

                lbladdress.InnerHtml = dt.Address;
                hfAddress.Value = dt.Address;
                generateQRCode(dt.Address);

                hfStatus.Value = dt.Status;

                if (dt.Status == "created")
                {
                    viewaddress.Visible = true;
                    viewqr.Visible = true;
                    viewcounter.Visible = true;
                    viewalert.Visible = true;

                    badgecreated.Visible = true;
                    badgeexpired.Visible = false;
                    badgecancelled.Visible = false;
                    badgecompleted.Visible = false;

                    badgefailed.Visible = false;
                    badgeinprogress.Visible = false;
                    badgepaid.Visible = false;

                    hfExpDate.Value = dt.DateExpired.ToString();
                    lblTime.Text = Common.CalculateTimeDifference(DateTime.Now, dt.DateExpired.Value);
                }
                else if (dt.Status == "completed" || dt.Status == "success")
                {
                    badgecreated.Visible = false;
                    badgeexpired.Visible = false;
                    badgecancelled.Visible = false;
                    badgecompleted.Visible = true;

                    badgefailed.Visible = false;
                    badgeinprogress.Visible = false;
                    badgepaid.Visible = false;
                }
                else if (dt.Status == "expired")
                {
                    badgecreated.Visible = false;
                    badgeexpired.Visible = true;
                    badgecancelled.Visible = false;
                    badgecompleted.Visible = false;

                    badgefailed.Visible = false;
                    badgeinprogress.Visible = false;
                    badgepaid.Visible = false;
                }
                else if (dt.Status == "in progress")
                {
                    badgecreated.Visible = false;
                    badgeexpired.Visible = false;
                    badgecancelled.Visible = false;
                    badgecompleted.Visible = false;

                    badgefailed.Visible = false;
                    badgeinprogress.Visible = true;
                    badgepaid.Visible = false;
                }
                else if (dt.Status == "paid")
                {
                    badgecreated.Visible = false;
                    badgeexpired.Visible = false;
                    badgecancelled.Visible = false;
                    badgecompleted.Visible = false;

                    badgefailed.Visible = false;
                    badgeinprogress.Visible = false;
                    badgepaid.Visible = true;
                }
                else if (dt.Status == "failed")
                {
                    badgecreated.Visible = false;
                    badgeexpired.Visible = false;
                    badgecancelled.Visible = false;
                    badgecompleted.Visible = false;

                    badgefailed.Visible = true;
                    badgeinprogress.Visible = false;
                    badgepaid.Visible = false;
                }
                else if (dt.Status == "cancelled")
                {
                    badgecreated.Visible = false;
                    badgeexpired.Visible = false;
                    badgecancelled.Visible = true;
                    badgecompleted.Visible = false;

                    badgefailed.Visible = false;
                    badgeinprogress.Visible = false;
                    badgepaid.Visible = false;
                }
            }
            else
            {
                // Don't swallow the failure with a silent bounce to the dashboard —
                // tell the user why the deposit couldn't be loaded.
                ShowAlert(op.Message);
            }
        }

        void generateQRCode(string addr)
        {
            imgQR.ImageUrl = Common.GenerateQrDataUri(addr);
            imgQR.Visible = true;
        }

        // Surfaces a backend error message to the user. This screen has no styled
        // alert modal yet (added when it's re-skinned), so fall back to a client alert.
        void ShowAlert(string message)
        {
            if (string.IsNullOrEmpty(message))
                message = "Something went wrong. Please try again.";
            string js = "alert('" + HttpUtility.JavaScriptStringEncode(message) + "');";
            ClientScript.RegisterStartupScript(GetType(), "txDepositError", js, true);
        }

        protected void Timer1_Tick(object sender, EventArgs e)
        {
            getCounter();
        }

        void getCounter() {
            if (hfStatus.Value == "created")
            {
                if (!string.IsNullOrEmpty(hfExpDate.Value))
                    lblTime.Text = Common.CalculateTimeDifference(DateTime.Now, Convert.ToDateTime(hfExpDate.Value));
            }
        }
    }
}