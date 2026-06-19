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
                    lblTime.Text = CalculateTimeDifference(DateTime.Now, dt.DateExpired.Value);
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
                Response.Redirect("~/dashboard");
                //rptCard.DataSource = null;
                //rptCard.DataBind();
            }
        }

        void generateQRCode(string addr)
        {
            QRCodeGenerator qrGenerator = new QRCodeGenerator();
            QRCodeData qrCodeData = qrGenerator.CreateQrCode(addr, QRCodeGenerator.ECCLevel.Q);
            QRCode qrCode = new QRCode(qrCodeData);
            Bitmap qrCodeImage = qrCode.GetGraphic(20);

            System.IO.MemoryStream ms = new MemoryStream();
            qrCodeImage.Save(ms, ImageFormat.Jpeg);
            byte[] byteImage = ms.ToArray();
            var SigBase64 = Convert.ToBase64String(byteImage);
            imgQR.ImageUrl = "data:image/jpeg;base64," + SigBase64;
            imgQR.Visible = true;
        }

        public string CalculateTimeDifference(DateTime startDate, DateTime endDate)
        {
            int days = 0; int hours = 0; int mins = 0; int secs = 0;
            string final = string.Empty;
            if (endDate > startDate)
            {
                //days = (endDate - startDate).Days;
                hours = (endDate - startDate).Hours;
                mins = (endDate - startDate).Minutes;
                secs = (endDate - startDate).Seconds;
                final = string.Format("{0} h : {1} m : {2} s", hours, mins, secs);
            }
            return final;
        }

        protected void Timer1_Tick(object sender, EventArgs e)
        {
            getCounter();
        }

        void getCounter() {
            if (hfStatus.Value == "creatred")
            {
                if (hfExpDate.Value != null | hfExpDate.Value != "")
                    lblTime.Text = CalculateTimeDifference(DateTime.Now, Convert.ToDateTime(hfExpDate.Value));
            }
        }
    }
}