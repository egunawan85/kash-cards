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
                lblCardNo.InnerHtml = String.Format("{0:0000 0000 0000 0000}", (Int64.Parse(dt.CardNumber)));
                if (dt.Organization == "Visa")
                    imgOrg.Src = "https://www.svgrepo.com/show/362035/visa-3.svg";
                else if (dt.Organization == "MasterCard")
                    imgOrg.Src = "https://www.svgrepo.com/show/508703/mastercard.svg";
                else
                    imgOrg.Src = "https://www.svgrepo.com/show/328132/discover.svg";

                if (dt.HolderID != null)
                    lblCardname.InnerHtml = dt.FirstName + " " + dt.LastName;

                hfDepositFeeRate.Value = dt.RechargeFeeRate;

                lblCardBalance.InnerHtml = dt.Param5 + " " + dt.Currency;

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
            lblCardNumber.InnerHtml = String.Format("{0:0000 0000 0000 0000}", (Int64.Parse(hfCardNumber.Value)));
            lblCVV.InnerHtml = Common.decrypt(hfCVV.Value);
            hfCVVDecr.Value = lblCVV.InnerHtml;
            lblExpDate.InnerHtml = Common.decrypt(hfExpDate.Value);
            hfExpDateDecr.Value = lblExpDate.InnerHtml;

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
                        lblCardholder.InnerHtml = dt.FirstName + " " + dt.LastName;
                        hfCardholder.Value = lblCardholder.InnerHtml;

                        lblPhone.InnerHtml = dt.AreaCode + dt.Mobile;
                        hfPhone.Value = lblPhone.InnerHtml;

                        lblEmail.InnerHtml = dt.Email;
                        hfEmail.Value = lblEmail.InnerHtml;

                        lblAddress.InnerHtml = dt.Address;
                        hfAddress.Value = lblAddress.InnerHtml;

                        lblCity.InnerHtml = dt.TownStr;
                        hfCity.Value = lblCity.InnerHtml;

                        lblState.InnerHtml = dt.StateStr;
                        hfState.Value = lblState.InnerHtml;

                        lblCountry.InnerHtml = dt.CountryStr;
                        hfCountry.Value = lblCountry.InnerHtml;

                        lblZipCode.InnerHtml = dt.PostCode;
                        hfZipCode.Value = lblZipCode.InnerHtml;

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
                lblUsage.InnerHtml = dt.CardDesc.Replace(",", ", ");
                lblDepositFee.InnerHtml = "Deposit Fee : " + dt.RechargeFeeRate + "%";

                lblMinDeposit.InnerHtml = "Minimum Deposit : " + dt.DepositAmountMinQuotaForActiveCard + " " + dt.FiatCurrency;
                lblMaxDeposit.InnerHtml = "Maximum Deposit : " + dt.DepositAmountMaxQuotaForActiveCard + " " + dt.FiatCurrency;


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
                if (dt.Count > 0)
                {
                    divnotrx.Visible = false;
                    gvDepositList.DataSource = dt;
                    gvDepositList.DataBind();
                }
                else
                {
                    divnotrx.Visible = true;
                    gvDepositList.DataSource = null;
                    gvDepositList.DataBind();
                }
            }
            else
            {
                divnotrx.Visible = true;
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
            ScriptManager.RegisterClientScriptBlock(this, GetType(), "Pop", "var isModalRecharge = true", true);
        }

        protected void rc20_ServerClick(object sender, EventArgs e)
        {
            txtDepositAmount.Text = "20";
            calculateDeposit();
            ScriptManager.RegisterClientScriptBlock(this, GetType(), "Pop", "var isModalRecharge = true", true);
        }

        protected void rc30_ServerClick(object sender, EventArgs e)
        {
            txtDepositAmount.Text = "30";
            calculateDeposit();
            ScriptManager.RegisterClientScriptBlock(this, GetType(), "Pop", "var isModalRecharge = true", true);

        }

        protected void rc50_ServerClick(object sender, EventArgs e)
        {
            txtDepositAmount.Text = "50";
            calculateDeposit();
            ScriptManager.RegisterClientScriptBlock(this, GetType(), "Pop", "var isModalRecharge = true", true);
        }

        protected void rc100_ServerClick(object sender, EventArgs e)
        {
            txtDepositAmount.Text = "100";
            calculateDeposit();
            ScriptManager.RegisterClientScriptBlock(this, GetType(), "Pop", "var isModalRecharge = true", true);
        }

        protected void rc200_ServerClick(object sender, EventArgs e)
        {
            txtDepositAmount.Text = "200";
            calculateDeposit();
            ScriptManager.RegisterClientScriptBlock(this, GetType(), "Pop", "var isModalRecharge = true", true);
        }

        protected void txtDepositAmount_TextChanged(object sender, EventArgs e)
        {
            calculateDeposit();
            ScriptManager.RegisterClientScriptBlock(this, GetType(), "Pop", "var isModalRecharge = true", true);
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
                lblfaileddeposit.Text = "Deposit amount cannot be empty";
                divfaileddeposit.Visible = true;
                ScriptManager.RegisterClientScriptBlock(this, GetType(), "Pop", "var isModalRecharge = true", true);
                return;
            }
            if (!checkMinimumDeposit())
            {
                btnDepositConfirm.Enabled = true;
                lblfaileddeposit.Text = "Minimum deposit amount is " + hfMinDeposit.Value + " USD";
                divfaileddeposit.Visible = true;
                ScriptManager.RegisterClientScriptBlock(this, GetType(), "Pop", "var isModalRecharge = true", true);
                return;
            }
            if (!checkMaximumDeposit())
            {
                btnDepositConfirm.Enabled = true;
                lblfaileddeposit.Text = "Maximum deposit amount is " + hfMaxDeposit.Value + " USD";
                divfaileddeposit.Visible = true;
                ScriptManager.RegisterClientScriptBlock(this, GetType(), "Pop", "var isModalRecharge = true", true);
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
                var dt = JsonConvert.DeserializeObject<CardDepositModel>(res.Data.ToString());
                bindData(hfCardID.Value);
                Response.Redirect("~/txdeposit?id=" + dt.ID);
            }
            else
            {
                btnDepositConfirm.Enabled = true;
                lblfaileddeposit.Text = res.Message;
                divfaileddeposit.Visible = true;
                ScriptManager.RegisterClientScriptBlock(this, GetType(), "Pop", "var isModalRecharge = true", true);
                return;
            }
        }

        protected void btndfaileddeposit_ServerClick(object sender, EventArgs e)
        {
            divfaileddeposit.Visible = false;
            ScriptManager.RegisterClientScriptBlock(this, GetType(), "Pop", "var isModalRecharge = true", true);
        }

        protected void btnDetail_ServerClick(object sender, EventArgs e)
        {
            var x = us.generateOTP();
            if (x.Status == "success")
            {
                hfOTPID.Value = x.Data.ToString();
                ScriptManager.RegisterClientScriptBlock(this, GetType(), "Pop", "var isModalDetail = true", true);
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
            ScriptManager.RegisterClientScriptBlock(this, GetType(), "Pop", "var isModalDetail = true", true);
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
                ScriptManager.RegisterClientScriptBlock(this, GetType(), "Pop", "var isModalDetail = true", true);
                return;
            }
            if (icode2.Value == "")
            {
                nableButtonConfirm();
                lblErrorDetail.Text = "All textbox should be filled";
                divfaileddetail.Visible = true;
                ScriptManager.RegisterClientScriptBlock(this, GetType(), "Pop", "var isModalDetail = true", true);
                return;
            }
            if (icode3.Value == "")
            {
                nableButtonConfirm();
                lblErrorDetail.Text = "All textbox should be filled";
                divfaileddetail.Visible = true;
                ScriptManager.RegisterClientScriptBlock(this, GetType(), "Pop", "var isModalDetail = true", true);
                return;
            }
            if (icode4.Value == "")
            {
                nableButtonConfirm();
                lblErrorDetail.Text = "All textbox should be filled";
                divfaileddetail.Visible = true;
                ScriptManager.RegisterClientScriptBlock(this, GetType(), "Pop", "var isModalDetail = true", true);
                return;
            }
            if (icode5.Value == "")
            {
                nableButtonConfirm();
                lblErrorDetail.Text = "All textbox should be filled";
                divfaileddetail.Visible = true;
                ScriptManager.RegisterClientScriptBlock(this, GetType(), "Pop", "var isModalDetail = true", true);
                return;
            }
            if (icode6.Value == "")
            {
                nableButtonConfirm();
                lblErrorDetail.Text = "All textboc should be filled";
                divfaileddetail.Visible = true;
                ScriptManager.RegisterClientScriptBlock(this, GetType(), "Pop", "var isModalDetail = true", true);
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

                //ScriptManager.RegisterClientScriptBlock(this, GetType(), "Pop", "var isModalKeyGenerate = false", true);
                ScriptManager.RegisterClientScriptBlock(this, GetType(), "Pop", "var isModalDetail = false", true);
            }
            else
            {
                nableButtonConfirm();
                lblErrorDetail.Text = x.Message;
                divfaileddetail.Visible = true;
                ScriptManager.RegisterClientScriptBlock(this, GetType(), "Pop", "var isModalDetail = true", true);
            }
        }

        protected void OnRowDataBound(object sender, System.Web.UI.WebControls.GridViewRowEventArgs e)
        {
            if (e.Row.RowType == DataControlRowType.DataRow)
            {
                e.Row.Attributes["onclick"] = Page.ClientScript.GetPostBackClientHyperlink(gvDepositList, "Select$" + e.Row.RowIndex);
                //e.Row.ToolTip = "Click to select this row.";
            }
        }

        protected void gvDepositList_SelectedIndexChanged(object sender, EventArgs e)
        {
            foreach (GridViewRow row in gvDepositList.Rows)
            {
                if (row.RowIndex == gvDepositList.SelectedIndex)
                {
                    var refs = ((HiddenField)row.FindControl("hfID")).Value;
                    Session["ID"] = refs;

                    //var a = Common.Base64Encode(refs);

                    Response.Redirect("~/txdeposit?id=" + refs);
                }
            }
        }
    }
}