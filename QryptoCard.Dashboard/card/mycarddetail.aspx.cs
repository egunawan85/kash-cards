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
using System.Windows.Documents;

namespace QryptoCard.Dashboard.card
{
    public partial class mycarddetail : System.Web.UI.Page
    {
        CardService cs = new CardService();
        UserService us = new UserService();
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
                        bindData(id);
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
                            Response.Redirect("~/card/mycardlist");
                        }
                        else
                        {
                            bindData(id);
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

        // Local brand mark for the 3D card, by card-type Organization (no external assets) — mirrors
        // My Cards. The org value only selects one of three fixed marks; it is never interpolated.
        string CardBrandMark(string org)
        {
            org = (org ?? "").Trim();
            if (string.Equals(org, "MasterCard", StringComparison.OrdinalIgnoreCase))
                return "<span class=\"qcard-net mc\" title=\"Mastercard\"><i></i><i></i></span>";
            if (string.Equals(org, "Discover", StringComparison.OrdinalIgnoreCase))
                return "<span class=\"qcard-net disc\" title=\"Discover\">DISC<b>O</b>VER</span>";
            return "<span class=\"qcard-net visa\" title=\"Visa\">VISA</span>";
        }

        // "This month" stat helpers — sum this calendar month's successful spends / deposits.
        static decimal MonthlySpendTotal(List<CardTransactionModel> rows)
        {
            decimal sum = 0m; var now = DateTime.UtcNow;
            if (rows == null) return sum;
            foreach (var t in rows)
            {
                var when = t.TransactionTime ?? DateTime.MinValue;
                string st = (t.Status ?? "").ToLowerInvariant();
                bool ok = st != "failed" && st != "fail" && st != "declined";
                if (ok && (t.Type ?? "").ToLowerInvariant() == "auth" && when.Year == now.Year && when.Month == now.Month)
                    sum += (decimal)(t.AuthorizedAmount ?? 0);
            }
            return sum;
        }
        static decimal MonthlyDepositTotal(List<CardDepositModel> rows)
        {
            decimal sum = 0m; var now = DateTime.UtcNow;
            if (rows == null) return sum;
            foreach (var d in rows)
            {
                var when = d.DateTransaction ?? DateTime.MinValue;
                if (string.Equals(d.Status, "success", StringComparison.OrdinalIgnoreCase) && when.Year == now.Year && when.Month == now.Month)
                    sum += (decimal)(d.Total ?? 0);
            }
            return sum;
        }

        void bindData(string id)
        {
            CardModel dt;

            var req = new CardModel();
            req.ID = id;
            OutputModel op = new OutputModel();
            op = cs.getCardDetail(req);
            if (op.Status == "success")
            {
                dt = JsonConvert.DeserializeObject<CardModel>(op.Data.ToString());
                hfCardID.Value = dt.ID;
                hfCardNo.Value = dt.CardNo;
                // Masked card-number shape on the 3D card — only the last 4 are real (the provider
                // never returns a full PAN). Strip to digits and show the last 4 behind a bullet mask.
                string cardDigits = new string((dt.CardNumber ?? "").Where(char.IsDigit).ToArray());
                string cardLast4 = cardDigits.Length >= 4 ? cardDigits.Substring(cardDigits.Length - 4) : cardDigits;
                lblCardNo.InnerHtml = "&#8226;&#8226;&#8226;&#8226; &#8226;&#8226;&#8226;&#8226; &#8226;&#8226;&#8226;&#8226; " + Server.HtmlEncode(cardLast4);
                lblCardBrand.InnerHtml = CardBrandMark(dt.Organization);

                if (dt.HolderID != null)
                    lblCardname.InnerHtml = Server.HtmlEncode(dt.FirstName + " " + dt.LastName);

                hfDepositFeeRate.Value = dt.RechargeFeeRate;

                lblCardBalance.InnerHtml = Server.HtmlEncode(dt.Param5 + " " + dt.Currency);

                hfCardNumber.Value = dt.CardNumber;
                hfCVV.Value = dt.CVV;
                hfExpDate.Value = dt.ValidPeriod;
                if (dt.HolderID != null)
                {
                    viewrc30.Visible = true;
                    viewrc20.Visible = false;
                    hfHolderID.Value = dt.HolderID.ToString();
                }
                else
                {
                    viewrc30.Visible = false;
                    viewrc20.Visible = true;
                }

                getCardType(dt.CardTypeId.ToString());
                getDepositList();
                getTrxList();
            }
            else
            {
                Response.Redirect("~/card/mycardlist");
                //rptCard.DataSource = null;
                //rptCard.DataBind();
            }
        }

        void getCardDetails()
        {
            viewdetails.Visible = true;
            long cardNumberVal;
            string cardNumberDisp = Int64.TryParse(hfCardNumber.Value, out cardNumberVal)
                ? String.Format("{0:0000 0000 0000 0000}", cardNumberVal)
                : (hfCardNumber.Value ?? "");
            lblCardNumber.InnerHtml = Server.HtmlEncode(cardNumberDisp);
            string cvvPlain = Common.decrypt(hfCVV.Value);
            hfCVVDecr.Value = cvvPlain;
            lblCVV.InnerHtml = Server.HtmlEncode(cvvPlain);
            string expPlain = Common.decrypt(hfExpDate.Value);
            hfExpDateDecr.Value = expPlain;
            lblExpDate.InnerHtml = Server.HtmlEncode(expPlain);

            if (hfHolderID.Value != "")
            {
                CardholderModel dt;

                var req = new CardholderModel();
                if (hfHolderID.Value != "" || hfHolderID.Value != null)
                {
                    req.HolderID = Convert.ToInt64(hfHolderID.Value);
                    OutputModel op = new OutputModel();
                    op = cs.getHolderDetail(req);
                    if (op.Status == "success")
                    {
                        dt = JsonConvert.DeserializeObject<CardholderModel>(op.Data.ToString());
                        string holderName = dt.FirstName + " " + dt.LastName;
                        hfCardholder.Value = holderName;
                        lblCardholder.InnerHtml = Server.HtmlEncode(holderName);

                        string holderPhone = dt.AreaCode + dt.Mobile;
                        hfPhone.Value = holderPhone;
                        lblPhone.InnerHtml = Server.HtmlEncode(holderPhone);

                        hfEmail.Value = dt.Email;
                        lblEmail.InnerHtml = Server.HtmlEncode(dt.Email);

                        hfAddress.Value = dt.Address;
                        lblAddress.InnerHtml = Server.HtmlEncode(dt.Address);

                        hfCity.Value = dt.TownStr;
                        lblCity.InnerHtml = Server.HtmlEncode(dt.TownStr);

                        hfState.Value = dt.StateStr;
                        lblState.InnerHtml = Server.HtmlEncode(dt.StateStr);

                        hfCountry.Value = dt.CountryStr;
                        lblCountry.InnerHtml = Server.HtmlEncode(dt.CountryStr);

                        hfZipCode.Value = dt.PostCode;
                        lblZipCode.InnerHtml = Server.HtmlEncode(dt.PostCode);

                        viewholder.Visible = true;
                    }
                }
            }

        }

        void getCardType(string id)
        {
            CardTypeModel dt;

            var req = new CardTypeModel();
            req.CardTypeId = Convert.ToInt32(id);
            OutputModel op = new OutputModel();
            op = cs.getCardTypesByID(req);
            if (op.Status == "success")
            {
                dt = JsonConvert.DeserializeObject<CardTypeModel>(op.Data.ToString());
                lblUsage.InnerHtml = Server.HtmlEncode((dt.CardDesc ?? "").Replace(",", ", "));
                lblDepositFee.InnerHtml = Server.HtmlEncode("Deposit Fee : " + dt.RechargeFeeRate + "%");

                lblMinDeposit.InnerHtml = Server.HtmlEncode("Minimum Deposit : " + dt.DepositAmountMinQuotaForActiveCard + " " + dt.FiatCurrency);
                lblMaxDeposit.InnerHtml = Server.HtmlEncode("Maximum Deposit : " + dt.DepositAmountMaxQuotaForActiveCard + " " + dt.FiatCurrency);


                hfCardTypeID.Value = dt.CardTypeId.ToString();
                hfIsHolderNeeded.Value = dt.NeedCardHolder.ToString();
                hfMinDeposit.Value = dt.DepositAmountMinQuotaForActiveCard;
                hfMaxDeposit.Value = dt.DepositAmountMaxQuotaForActiveCard;
            }
            else
            {

                //rptCard.DataSource = null;
                //rptCard.DataBind();
            }
        }

        void getDepositList()
        {
            var req = new CardDepositModel();
            req.CardNo = hfCardNo.Value;
            OutputModel op = new OutputModel();
            op = cs.depositCardList(req);
            if (op.Status == "success")
            {
                var dt = JsonConvert.DeserializeObject<List<CardDepositModel>>(op.Data.ToString());
                litTopped.Text = "$" + MonthlyDepositTotal(dt).ToString("0.00");
                if (dt.Count > 0)
                {
                    divnodepo.Visible = false;
                    gvDepositList.DataSource = dt;
                    gvDepositList.DataBind();
                }
                else
                {
                    divnodepo.Visible = true;
                    gvDepositList.DataSource = null;
                    gvDepositList.DataBind();
                }
            }
            else
            {
                divnodepo.Visible = true;
                gvDepositList.DataSource = null;
                gvDepositList.DataBind();
            }
        }

        //void getDepositList()
        //{
        //    var req = new CardDepositModel();
        //    req.CardNo = hfCardNo.Value;
        //    OutputModel op = new OutputModel();
        //    op = cs.depositCardList(req);
        //    if (op.Status == "success")
        //    {
        //        var dt = JsonConvert.DeserializeObject<List<CardDepositModel>>(op.Data.ToString());

        //        if (dt.Count > 0)
        //        {
        //            divnodepo.Visible = false;
        //            gvDepositList.DataSource = dt;
        //            gvDepositList.DataBind();
        //        }
        //        else
        //        {
        //            divnodepo.Visible = true;
        //            gvDepositList.DataSource = null;
        //            gvDepositList.DataBind();
        //        }
        //    }
        //    else
        //    {
        //        divnodepo.Visible = true;
        //        gvDepositList.DataSource = null;
        //        gvDepositList.DataBind();
        //    }
        //}
        void getTrxList()
        {
            var req = new CardModel();
            req.CardNo = hfCardNo.Value;
            OutputModel op = new OutputModel();
            op = cs.trxCardList(req);
            if (op.Status == "success")
            {
                var dt = JsonConvert.DeserializeObject<List<CardTransactionModel>>(op.Data.ToString());
                litSpent.Text = "$" + MonthlySpendTotal(dt).ToString("0.00");

                if (dt.Count > 0)
                {
                    divnotrx.Visible = false;
                    gvTrxList.DataSource = dt;
                    gvTrxList.DataBind();
                }
                else
                {
                    divnotrx.Visible = true;
                    gvTrxList.DataSource = null;
                    gvTrxList.DataBind();
                }
            }
            else
            {
                divnotrx.Visible = true;
                gvTrxList.DataSource = null;
                gvTrxList.DataBind();
            }
        }

        protected Boolean IsStatusCreated(string i)
        {
            if (i == "created")
                return true;
            else
                return false;
        }

        protected Boolean IsStatusInProgress(string i)
        {
            if (i == "in progress")
                return true;
            else
                return false;
        }

        protected Boolean IsStatusCreatedBadge(string i)
        {
            if (i == "created")
                return true;
            else
                return false;
        }

        protected Boolean IsStatusCompleted(string i)
        {
            if (i == "completed" || i == "success")
                return true;
            else
                return false;
        }

        protected Boolean IsStatusCancelled(string i)
        {
            if (i == "cancelled")
                return true;
            else
                return false;
        }

        protected Boolean IsStatusFailed(string i)
        {
            if (i == "failed")
                return true;
            else
                return false;
        }

        protected Boolean IsStatusExpired(string i)
        {
            if (i == "expired")
                return true;
            else
                return false;
        }

        protected void btnInfo_ServerClick(object sender, EventArgs e)
        {

            ScriptManager.RegisterClientScriptBlock(this, GetType(), "Pop", "var isModalInfo = true", true);
        }

        protected void btnRecharge_ServerClick(object sender, EventArgs e)
        {
            pnlRecharge.Visible = true;
        }

        protected void rc20_ServerClick(object sender, EventArgs e)
        {
            txtDepositAmount.Text = "20";
            calculateDeposit();
            pnlRecharge.Visible = true;
        }

        protected void rc30_ServerClick(object sender, EventArgs e)
        {
            txtDepositAmount.Text = "30";
            calculateDeposit();
            pnlRecharge.Visible = true;

        }

        protected void rc50_ServerClick(object sender, EventArgs e)
        {
            txtDepositAmount.Text = "50";
            calculateDeposit();
            pnlRecharge.Visible = true;
        }

        protected void rc100_ServerClick(object sender, EventArgs e)
        {
            txtDepositAmount.Text = "100";
            calculateDeposit();
            pnlRecharge.Visible = true;
        }

        protected void rc200_ServerClick(object sender, EventArgs e)
        {
            txtDepositAmount.Text = "200";
            calculateDeposit();
            pnlRecharge.Visible = true;
        }

        protected void txtDepositAmount_TextChanged(object sender, EventArgs e)
        {
            calculateDeposit();
            pnlRecharge.Visible = true;
        }

        void calculateDeposit()
        {
            if (txtDepositAmount.Text != "")
            {
                var fr = Convert.ToDouble(hfDepositFeeRate.Value);
                var em = Convert.ToDouble(txtDepositAmount.Text);

                lblDepositAmount.InnerHtml = em.ToString() + " USD";
                lblFee.InnerHtml = ((fr / 100) * em).ToString() + " USD";
                lblTotal.InnerHtml = (((fr / 100) * em) + em).ToString() + " USD";
            }
            else
            {
                lblTotal.InnerHtml = "0 USD";
                lblDepositAmount.InnerHtml = lblTotal.InnerHtml;
                lblFee.InnerHtml = lblTotal.InnerHtml;
            }
        }

        bool checkMinimumDeposit()
        {
            var x = Convert.ToDouble(hfMinDeposit.Value);
            var am = Convert.ToDouble(txtDepositAmount.Text);

            if (am < x)
                return false;
            else
                return true;
        }

        bool checkMaximumDeposit()
        {
            var x = Convert.ToDouble(hfMaxDeposit.Value);
            var am = Convert.ToDouble(txtDepositAmount.Text);

            if (am > x)
                return false;
            else
                return true;
        }

        protected void btnDepositConfirm_Click(object sender, EventArgs e)
        {
            if (txtDepositAmount.Text == "")
            {
                btnDepositConfirm.Enabled = true;
                lblfaileddeposit.InnerText = "Deposit amount cannot be empty";
                divfaileddeposit.Visible = true;
                pnlRecharge.Visible = true;
                return;
            }
            if (!checkMinimumDeposit())
            {
                btnDepositConfirm.Enabled = true;
                lblfaileddeposit.InnerText = "Minimum deposit amount is " + hfMinDeposit.Value + " USD";
                divfaileddeposit.Visible = true;
                pnlRecharge.Visible = true;
                return;
            }
            if (!checkMaximumDeposit())
            {
                btnDepositConfirm.Enabled = true;
                lblfaileddeposit.InnerText = "Maximum deposit amount is " + hfMaxDeposit.Value + " USD";
                divfaileddeposit.Visible = true;
                pnlRecharge.Visible = true;
                return;
            }

            CardDepositModel x = new CardDepositModel();
            x.ID = hfCardID.Value;
            x.Amount = Convert.ToDouble(txtDepositAmount.Text);
            x.CardNo = hfCardNo.Value;
            var res = cs.depositCard(x);
            if (res.Status == "success")
            {
                btnDepositConfirm.Enabled = true;
                // Top-up is paid from the wallet balance immediately, so there is no per-card
                // deposit address to show. Return to this card's (re-skinned) detail page, which
                // now reflects the updated balance + the new deposit row.
                Response.Redirect("~/card/mycarddetail?id=" + HttpUtility.UrlEncode(hfCardID.Value));
            }
            else
            {
                btnDepositConfirm.Enabled = true;
                lblfaileddeposit.InnerHtml = BuildSpendError(res.Message);
                divfaileddeposit.Visible = true;
                pnlRecharge.Visible = true;
                return;
            }
        }

        // Surface the server's top-up-failure message. The charge is server-authoritative (we send
        // only the card id, card no, and the user-entered amount), so this only reports the outcome.
        // When the wallet balance is short, add a direct CTA to top up the wallet.
        string BuildSpendError(string message)
        {
            if (string.IsNullOrEmpty(message))
                message = "Unable to complete the top-up. Please try again.";
            bool insufficient = message.IndexOf("insufficient balance", StringComparison.OrdinalIgnoreCase) >= 0;
            string html = HttpUtility.HtmlEncode(message);
            if (insufficient)
                html += "<br /><a class=\"btn btn-cyan\" style=\"margin-top:10px;\" href=\"" + ResolveUrl("~/txdeposit") + "\">Add funds</a>";
            return html;
        }

        protected void btndfaileddeposit_ServerClick(object sender, EventArgs e)
        {
            divfaileddeposit.Visible = false;
            pnlRecharge.Visible = true;
        }

        protected void btnDetail_ServerClick(object sender, EventArgs e)
        {
            var x = us.generateOTP();
            if (x.Status == "success")
            {
                hfOTPID.Value = x.Data.ToString();
                pnlDetailOTP.Visible = true;
            }
            //else
            //{
            //    lblError.Text = x.Message;
            //    divfailed.Visible = true;
            //}
        }

        protected void btnfaileddetail_ServerClick(object sender, EventArgs e)
        {
            divfaileddetail.Visible = false;
            pnlDetailOTP.Visible = true;
        }

        void nableButtonConfirm()
        {
            btnDetailX.Enabled = true;
        }

        protected void btnDetailX_Click(object sender, EventArgs e)
        {
            if (icode1.Value == "")
            {
                nableButtonConfirm();
                lblErrorDetail.Text = "All textbox should be filled";
                divfaileddetail.Visible = true;
                pnlDetailOTP.Visible = true;
                return;
            }
            if (icode2.Value == "")
            {
                nableButtonConfirm();
                lblErrorDetail.Text = "All textbox should be filled";
                divfaileddetail.Visible = true;
                pnlDetailOTP.Visible = true;
                return;
            }
            if (icode3.Value == "")
            {
                nableButtonConfirm();
                lblErrorDetail.Text = "All textbox should be filled";
                divfaileddetail.Visible = true;
                pnlDetailOTP.Visible = true;
                return;
            }
            if (icode4.Value == "")
            {
                nableButtonConfirm();
                lblErrorDetail.Text = "All textbox should be filled";
                divfaileddetail.Visible = true;
                pnlDetailOTP.Visible = true;
                return;
            }
            if (icode5.Value == "")
            {
                nableButtonConfirm();
                lblErrorDetail.Text = "All textbox should be filled";
                divfaileddetail.Visible = true;
                pnlDetailOTP.Visible = true;
                return;
            }
            if (icode6.Value == "")
            {
                nableButtonConfirm();
                lblErrorDetail.Text = "All textboc should be filled";
                divfaileddetail.Visible = true;
                pnlDetailOTP.Visible = true;
                return;
            }
            UserOTPModel z = new UserOTPModel();
            z.OTPID = hfOTPID.Value;
            z.Code = icode1.Value + icode2.Value + icode3.Value + icode4.Value + icode5.Value + icode6.Value;

            var x = us.validateOTP(z);
            if (x.Status == "success")
            {
                nableButtonConfirm();
                icode1.Value = "";
                icode2.Value = "";
                icode3.Value = "";
                icode4.Value = "";
                icode5.Value = "";
                icode6.Value = "";
                getCardDetails();

                // Reveal succeeded — the OTP overlay stays closed (Visible defaults false).
            }
            else
            {
                nableButtonConfirm();
                lblErrorDetail.Text = x.Message;
                divfaileddetail.Visible = true;
                pnlDetailOTP.Visible = true;
            }
        }

        // Overlay close buttons — hide the server-rendered overlay (no Bootstrap/jQuery on the shell).
        protected void btnRechargeClose_Click(object sender, EventArgs e) { pnlRecharge.Visible = false; }
        protected void btnDetailClose_Click(object sender, EventArgs e) { pnlDetailOTP.Visible = false; }
        protected void btnAlertClose_Click(object sender, EventArgs e) { pnlAlert.Visible = false; }

        protected void OnRowDataBound(object sender, System.Web.UI.WebControls.GridViewRowEventArgs e)
        {
            if (e.Row.RowType == DataControlRowType.DataRow)
            {
                // Rows are no longer clickable: they used to navigate to the per-card
                // deposit-address detail page, which is retired now that top-ups pay from the
                // wallet balance. The list stays as read-only top-up history.
                //e.Row.Attributes["onclick"] = Page.ClientScript.GetPostBackClientHyperlink(gvDepositList, "Select$" + e.Row.RowIndex);
            }
        }

        // The per-card deposit-detail navigation is retired (top-ups pay from the wallet
        // balance now), so the row select handler and its redirect to the old ~/txdeposit?id=
        // page were removed along with the row onclick wiring above. The list is read-only history.
    }
}