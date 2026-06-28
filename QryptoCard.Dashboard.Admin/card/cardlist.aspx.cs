using Newtonsoft.Json;
using QryptoCard.Dashboard.Admin.Models;
using QryptoCard.Dashboard.Admin.Models.Service;
using QryptoCard.Dashboard.Admin.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.HtmlControls;
using System.Web.UI.WebControls;

namespace QryptoCard.Dashboard.Admin.card
{
    public partial class cardlist : System.Web.UI.Page
    {
        CardService us = new CardService();

        protected void Page_Load(object sender, EventArgs e)
        {
            Master.setTitle("Card List");
            if (Common.checkID())
            {
                if (SessionLib.Current.Role == RoleModel.Signer || SessionLib.Current.Role == RoleModel.Approver || SessionLib.Current.Role == RoleModel.Viewer)
                {
                    Response.Redirect("~/Default");
                }
                else
                {
                    if (!IsPostBack)
                    {
                        //btnGenerate.Visible = true;
                        bindGV();
                        loadPricing();
                    }
                }
            }
            else
            {
                Master.forceLogin();
            }

            //bindGV();
        }

        protected Boolean IsVerified(int active)
        {
            if (active == 1)
                return true;
            else
                return false;
        }

        protected Boolean IsOwner(string active)
        {
            if (active != SessionLib.Current.Email)
                return true;
            else
                return false;
        }


        void bindGV()
        {

            if (Session["SuccessAddress"] != null)
            {
                lblSuccess.Text = Session["SuccessAddress"].ToString();
                divsuccess.Visible = true;
                Session["SuccessAddress"] = null;
            }

            if (Session["ErrorAddress"] != null)
            {
                lblError.Text = Session["ErrorAddress"].ToString();
                divfailed.Visible = true;
                Session["ErrorAddress"] = null;
            }

            List<CardTypeModel> dt;
            OutputModel op = new OutputModel();
            op = us.getCardType();
            if (op.Status == "success")
            {
                dt = JsonConvert.DeserializeObject<List<CardTypeModel>>(op.Data.ToString());
                if (dt.Count > 0)
                {
                    for (int i = 0; i < dt.Count; i++)
                    {
                        dt[i].CardPrice = dt[i].CardPrice + " " + dt[i].CardPriceCurrency;
                        dt[i].RechargeFeeRate = dt[i].RechargeFeeRate + "%";
                    }
                    gvListItem.Visible = true;
                    gvListItem.DataSource = null;
                    gvListItem.DataSource = dt;

                    gvListItem.DataBind();

                    //gvListItem.Columns[5].ItemStyle.Width = 1200;
                    //gvListItem.Columns[8].ItemStyle.Width = 500;
                    divnorow.Visible = false;
                }
                else
                {
                    gvListItem.DataSource = null;
                    gvListItem.DataBind();
                    gvListItem.Visible = false;
                    divnorow.Visible = true;
                }
            }
            else
            {
                gvListItem.DataSource = null;
                gvListItem.DataBind();
                gvListItem.Visible = false;
                divnorow.Visible = true;
            }
        }



        protected void gvListItem_PageIndexChanging(object sender, GridViewPageEventArgs e)
        {

        }

        // ---- Global card pricing panel (CardPrice + CardDepositFeeRate settings) -------------

        void loadPricing()
        {
            OutputModel op = us.getCardPricing();
            if (op.Status == "success")
            {
                var dt = JsonConvert.DeserializeObject<CardTypeModel>(op.Data.ToString());
                txtGlobalCardPrice.Text = dt.CardPrice;
                txtGlobalDepositFee.Text = dt.RechargeFeeRate;
            }
            else
            {
                lblError.Text = op.Message;
                divfailed.Visible = true;
            }
        }

        protected void btnUpdatePricing_Click(object sender, EventArgs e)
        {
            // Role gate mirrors the per-card finance-mutation gate (INT also enforces it).
            if (SessionLib.Current.Role == RoleModel.Approver || SessionLib.Current.Role == RoleModel.Viewer || SessionLib.Current.Role == RoleModel.Signer)
            {
                lblError.Text = "You have no authorize to do this request";
                divfailed.Visible = true;
                return;
            }

            double price, fee;
            if (!double.TryParse(txtGlobalCardPrice.Text, out price) || price < 0)
            {
                lblError.Text = "Card price must be a number greater than or equal to 0";
                divfailed.Visible = true;
                return;
            }
            if (!double.TryParse(txtGlobalDepositFee.Text, out fee) || fee < 0)
            {
                lblError.Text = "Deposit fee must be a number greater than or equal to 0";
                divfailed.Visible = true;
                return;
            }

            CardTypeModel uw = new CardTypeModel();
            uw.CardPrice = txtGlobalCardPrice.Text.Trim();
            uw.RechargeFeeRate = txtGlobalDepositFee.Text.Trim();

            OutputModel op = us.updateCardPricing(uw);
            if (op.Status == "success")
            {
                lblSuccess.Text = op.Message;
                divsuccess.Visible = true;
                loadPricing();
                bindGV();
            }
            else
            {
                lblError.Text = op.Message;
                divfailed.Visible = true;
            }
        }


        protected void btnSuccessClose_ServerClick(object sender, EventArgs e)
        {
            divsuccess.Visible = false;
        }

        protected void btnFailedClose_ServerClick(object sender, EventArgs e)
        {
            divfailed.Visible = false;
        }

        protected void btnGenerate_Click(object sender, EventArgs e)
        {
            if (SessionLib.Current.Role == RoleModel.Approver || SessionLib.Current.Role == RoleModel.Viewer || SessionLib.Current.Role == RoleModel.Signer)
            {
                lblError.Text = "You have no authorize to create this request";
                divfailed.Visible = true;
                return;
            }
            else
                ScriptManager.RegisterClientScriptBlock(this, GetType(), "Pop", "var isModalAdd = true", true);
        }




        //protected void ddlCoinTokenAdd_SelectedIndexChanged(object sender, EventArgs e)
        //{
        //    if (ddlCoinTokenAdd.SelectedValue == "coin")
        //    {
        //        divcoinadd.Visible = true;
        //        divnetworkadd.Visible = false;
        //        divtokenadd.Visible = false;
        //    }
        //    else
        //    {
        //        divcoinadd.Visible = false;
        //        divnetworkadd.Visible = true;
        //        divtokenadd.Visible = true;
        //        bindToken();
        //    }
        //    ScriptManager.RegisterClientScriptBlock(this, GetType(), "Pop", "var isModalAdd = true", true);
        //}

        //protected void ddlNetworkAdd_SelectedIndexChanged(object sender, EventArgs e)
        //{
        //    bindToken();
        //    ScriptManager.RegisterClientScriptBlock(this, GetType(), "Pop", "var isModalAdd = true", true);
        //}

        //protected void ddlTokenAdd_SelectedIndexChanged(object sender, EventArgs e)
        //{

        //    ScriptManager.RegisterClientScriptBlock(this, GetType(), "Pop", "var isModalAdd = true", true);
        //}

        protected void btnCreateExec_Click(object sender, EventArgs e)
        {
            if (SessionLib.Current.Role == RoleModel.Approver || SessionLib.Current.Role == RoleModel.Viewer || SessionLib.Current.Role == RoleModel.Signer)
            {
                lblFailedAdd.Text = "You have no authorize to create this request";
                divfailedadd.Visible = true;
                ScriptManager.RegisterClientScriptBlock(this, GetType(), "Pop", "var isModalAdd = true", true);
                return;
            }
            if (ddlRoleAdd.SelectedValue == "")
            {
                lblFailedAdd.Text = "Role cannot be empty";
                divfailedadd.Visible = true;
                ScriptManager.RegisterClientScriptBlock(this, GetType(), "Pop", "var isModalAdd = true", true);
                return;
            }
            if (txtFirstNameAdd.Text == "")
            {
                lblFailedAdd.Text = "First name cannot be empty";
                divfailedadd.Visible = true;
                ScriptManager.RegisterClientScriptBlock(this, GetType(), "Pop", "var isModalAdd = true", true);
                return;
            }
            if (txtLastNameAdd.Text == "")
            {
                lblFailedAdd.Text = "Last name cannot be empty";
                divfailedadd.Visible = true;
                ScriptManager.RegisterClientScriptBlock(this, GetType(), "Pop", "var isModalAdd = true", true);
                return;
            }
            if (txtEmailAdd.Text == "")
            {
                lblFailedAdd.Text = "Email address cannot be empty";
                divfailedadd.Visible = true;
                ScriptManager.RegisterClientScriptBlock(this, GetType(), "Pop", "var isModalAdd = true", true);
                return;
            }

            AdminModel uw = new AdminModel();
            uw.FirstName = txtFirstNameAdd.Text.Trim();
            uw.LastName = txtLastNameAdd.Text.Trim();
            uw.Email = txtEmailAdd.Text.Trim();
            uw.RoleID = ddlRoleAdd.SelectedValue.Trim();
            uw.InvitedBy = SessionLib.Current.AdminID;


            OutputModel op = new OutputModel();
            //op = us.inviteAdmin(uw);
            //if (op.Status == "success")
            //{
            //    lblSuccess.Text = op.Message;
            //    divsuccess.Visible = true;
            //    txtFirstNameAdd.Text = "";
            //    txtLastNameAdd.Text = "";
            //    txtEmailAdd.Text = "";
            //    bindGV();
            //}
            //else
            //{
            //    lblError.Text = op.Message;
            //    divfailed.Visible = true;
            //}
        }

        protected void btndfailedadd_ServerClick(object sender, EventArgs e)
        {

            ScriptManager.RegisterClientScriptBlock(this, GetType(), "Pop", "var isModalAdd = true", true);
        }

        protected void ddlStatus_SelectedIndexChanged(object sender, EventArgs e)
        {
            bindGV();
        }

        protected void gvListItem_RowCreated(object sender, GridViewRowEventArgs e)
        {
            //if ((e.Row.RowType == DataControlRowType.Header))
            //{
            //    // For first column set to 200 px
            //    TableCell cell = e.Row.Cells[0];
            //    cell.Width = new Unit("50px");
            //    // For others set to 50 px
            //    // You can set all the width individually
            //    for (int i = 1; (i <= (e.Row.Cells.Count - 1)); i++)
            //    {
            //        // Mind that i used i=1 not 0 because the width of cells(0) has already been set
            //        TableCell cell2 = e.Row.Cells[i];
            //        cell2.Width = new Unit("1000px");
            //    }
            //}
        }

        protected void btnCancel_ServerClick(object sender, EventArgs e)
        {
            HtmlButton btn = (HtmlButton)sender;
            GridViewRow row = (GridViewRow)btn.NamingContainer;
            hfTID.Value = ((HiddenField)row.FindControl("hfID")).Value;

            //hfMerchantID.Value = id;
            //lblBalanceTopup.Text = row.Cells[8].Text;
            //lblNameTopup.Text = row.Cells[1].Text;
            //lblBatchTopup.Text = row.Cells[6].Text;
            getCardDetail(false, hfTID.Value);

            ScriptManager.RegisterClientScriptBlock(this, GetType(), "Pop", "var isModalDetail = true", true);

        }


        protected void btndfaileddetail_ServerClick(object sender, EventArgs e)
        {
            divfaileddetail.Visible = false;
            ScriptManager.RegisterClientScriptBlock(this, GetType(), "Pop", "var isModalDetail = true", true);
        }

        protected void btnInfo_ServerClick(object sender, EventArgs e)
        {
            HtmlButton btn = (HtmlButton)sender;
            GridViewRow row = (GridViewRow)btn.NamingContainer;
            hfTID.Value = ((HiddenField)row.FindControl("hfID")).Value;
            hfCID.Value = ((HiddenField)row.FindControl("hfCID")).Value;

            //hfMerchantID.Value = id;
            //lblBalanceTopup.Text = row.Cells[8].Text;
            //lblNameTopup.Text = row.Cells[1].Text;
            //lblBatchTopup.Text = row.Cells[6].Text;
            getCardDetail(false, hfTID.Value);
        }

        void getCardDetail(bool fl, string uid)
        {
            CardTypeModel z = new CardTypeModel();
            z.ID = Convert.ToInt32(uid);
            z.CardTypeId = Convert.ToInt32(hfCID.Value);

            var q = us.getCardTypeDetail(z);
            if (q.Status == "success")
            {
                var dt = JsonConvert.DeserializeObject<CardTypeModel>(q.Data.ToString());
                txtOrganizationDetail.Text = dt.Organization;
                txtBankCardBinDetail.Text = dt.BankCardBin;
                txtCardDescDetail.Text = dt.CardDesc;
                txtCardPriceDetail.Text = dt.CardPrice + " " + dt.CardPriceCurrency;
                txtRechargeFeeRateDetail.Text = dt.RechargeFeeRate + "%";
                txtMinDepositDetail.Text = dt.DepositAmountMinQuotaForActiveCard + " " + dt.CardPriceCurrency;
                txtMaxDepositDetail.Text = dt.DepositAmountMaxQuotaForActiveCard + " " + dt.CardPriceCurrency;
                setNeedCardholder(dt.isActive.Value);

                ScriptManager.RegisterClientScriptBlock(this, GetType(), "Pop", "var isModalDetail = true", true);
            }
            else
            {
                lblError.Text = q.Message;
                divfailed.Visible = true;
            }
        }

        void setNeedCardholder(int status)
        {
            if (status == 1)
            {
                badgeCardholder.Visible = true;
                badgeCardholderNot.Visible = false;
            }
            else
            {
                badgeCardholder.Visible = false;
                badgeCardholderNot.Visible = true;
            }
        }

        protected void btnCopyAddrDetail_ServerClick(object sender, EventArgs e)
        {
            //ClientScript.RegisterStartupScript(GetType(), "Javascript", "javascript:copyAddress('" + txtInvitedByDetail.Text + "'); ", true);
            ScriptManager.RegisterClientScriptBlock(this, GetType(), "Pop", "var isModalDetail = true", true);
            ScriptManager.RegisterClientScriptBlock(this, GetType(), "Pop", "var isModalPopup = true", true);
        }

        protected void ddlRole_SelectedIndexChanged(object sender, EventArgs e)
        {
            bindGV();
        }

        protected void btnBanUser_Click(object sender, EventArgs e)
        {
            AdminModel z = new AdminModel();
            z.AdminID = hfTID.Value;
            z.isActive = 1;

            //var x = us.banAdmin(z);
            //if (x.Status == "success")
            //{
            //    lblSuccess.Text = x.Message;
            //    divsuccess.Visible = true;
            //    bindGV();
            //    ScriptManager.RegisterClientScriptBlock(this, GetType(), "Pop", "var isModalDetail = false", true);
            //}
            //else
            //{
            //    lblFailedDetail.Text = x.Message;
            //    divfaileddetail.Visible = true;
            //    ScriptManager.RegisterClientScriptBlock(this, GetType(), "Pop", "var isModalDetail = true", true);
            //}
        }

        protected void ddlRoleAdd_SelectedIndexChanged(object sender, EventArgs e)
        {
            ScriptManager.RegisterClientScriptBlock(this, GetType(), "Pop", "var isModalAdd = true", true);
        }

        protected void btnUpdatePrice_ServerClick(object sender, EventArgs e)
        {
            HtmlButton btn = (HtmlButton)sender;
            GridViewRow row = (GridViewRow)btn.NamingContainer;
            hfTID.Value = ((HiddenField)row.FindControl("hfID")).Value;
            hfCID.Value = ((HiddenField)row.FindControl("hfCID")).Value;

            //hfMerchantID.Value = id;
            //lblBalanceTopup.Text = row.Cells[8].Text;
            //lblNameTopup.Text = row.Cells[1].Text;
            //lblBatchTopup.Text = row.Cells[6].Text;
            getCardDetailUpdate(hfTID.Value, hfCID.Value);

            ScriptManager.RegisterClientScriptBlock(this, GetType(), "Pop", "var isModalUpdatePrice = true", true);
        }

        protected void btndfaileduprice_ServerClick(object sender, EventArgs e)
        {
            divfaileduprice.Visible = false;
            ScriptManager.RegisterClientScriptBlock(this, GetType(), "Pop", "var isModalUpdatePrice = true", true);
        }

        void getCardDetailUpdate(string uid, string cid)
        {
            CardTypeModel z = new CardTypeModel();
            z.ID = Convert.ToInt32(uid);
            z.CardTypeId = Convert.ToInt32(hfCID.Value);

            var q = us.getCardTypeDetail(z);
            if (q.Status == "success")
            {
                var dt = JsonConvert.DeserializeObject<CardTypeModel>(q.Data.ToString());
                txtOrganizationUPrice.Text = dt.Organization;
                txtCardBinUPrice.Text = dt.BankCardBin;
                txtDescUPrice.Text = dt.CardDesc;
                txtCurrentPrice.Text = dt.CardPrice + " " + dt.CardPriceCurrency;
                txtNewPrice.Text = dt.CardPrice;

                ScriptManager.RegisterClientScriptBlock(this, GetType(), "Pop", "var isModalUpdatePrice = true", true);
            }
            else
            {
                lblError.Text = q.Message;
                divfailed.Visible = true;
            }
        }

        protected void btnUpdatePriceExec_Click(object sender, EventArgs e)
        {
            if (SessionLib.Current.Role == RoleModel.Approver || SessionLib.Current.Role == RoleModel.Viewer || SessionLib.Current.Role == RoleModel.Signer)
            {
                lblFailedUPrice.Text = "You have no authorize to do this request";
                divfaileduprice.Visible = true;
                ScriptManager.RegisterClientScriptBlock(this, GetType(), "Pop", "var isModalUpdatePrice = true", true);
                return;
            }
            if (txtNewPrice.Text == "")
            {
                lblFailedAdd.Text = "New Price cannot be empty";
                divfaileduprice.Visible = true;
                ScriptManager.RegisterClientScriptBlock(this, GetType(), "Pop", "var isModalUpdatePrice = true", true);
                return;
            }
            if (Convert.ToDouble(txtNewPrice.Text) < 0)
            {
                lblFailedAdd.Text = "New price cannot be less than 0 USD";
                divfaileduprice.Visible = true;
                ScriptManager.RegisterClientScriptBlock(this, GetType(), "Pop", "var isModalUpdatePrice = true", true);
                return;
            }

            CardTypeModel uw = new CardTypeModel();
            uw.ID = Convert.ToInt32(hfTID.Value);
            uw.CardTypeId = Convert.ToInt32(hfCID.Value);
            uw.CardPrice = txtNewPrice.Text;


            OutputModel op = new OutputModel();
            op = us.updateCardPrice(uw);
            if (op.Status == "success")
            {
                lblSuccess.Text = op.Message;
                divsuccess.Visible = true;
                txtNewPrice.Text = "";
                bindGV();
            }
            else
            {
                lblError.Text = op.Message;
                divfailed.Visible = true;
            }
        }





        protected void btnUpdateFee_ServerClick(object sender, EventArgs e)
        {
            HtmlButton btn = (HtmlButton)sender;
            GridViewRow row = (GridViewRow)btn.NamingContainer;
            hfTID.Value = ((HiddenField)row.FindControl("hfID")).Value;
            hfCID.Value = ((HiddenField)row.FindControl("hfCID")).Value;

            //hfMerchantID.Value = id;
            //lblBalanceTopup.Text = row.Cells[8].Text;
            //lblNameTopup.Text = row.Cells[1].Text;
            //lblBatchTopup.Text = row.Cells[6].Text;
            getCardDetailUpdateFee(hfTID.Value, hfCID.Value);

            ScriptManager.RegisterClientScriptBlock(this, GetType(), "Pop", "var isModalUpdateFee = true", true);
        }

        void getCardDetailUpdateFee(string uid, string cid)
        {
            CardTypeModel z = new CardTypeModel();
            z.ID = Convert.ToInt32(uid);
            z.CardTypeId = Convert.ToInt32(hfCID.Value);

            var q = us.getCardTypeDetail(z);
            if (q.Status == "success")
            {
                var dt = JsonConvert.DeserializeObject<CardTypeModel>(q.Data.ToString());
                txtOrganizationUFee.Text = dt.Organization;
                txtCardBinUFee.Text = dt.BankCardBin;
                txtDescUFee.Text = dt.CardDesc;
                txtCurrentFee.Text = dt.RechargeFeeRate + "%";
                txtNewFee.Text = dt.RechargeFeeRate;
                ScriptManager.RegisterClientScriptBlock(this, GetType(), "Pop", "var isModalUpdateFee = true", true);
            }
            else
            {
                lblError.Text = q.Message;
                divfailed.Visible = true;
            }
        }

        protected void btndfailedufee_ServerClick(object sender, EventArgs e)
        {
            divfailedufee.Visible = false;
            ScriptManager.RegisterClientScriptBlock(this, GetType(), "Pop", "var isModalUpdateFee = true", true);
        }

        protected void btnUpdateFeeExec_Click(object sender, EventArgs e)
        {
            if (SessionLib.Current.Role == RoleModel.Approver || SessionLib.Current.Role == RoleModel.Viewer || SessionLib.Current.Role == RoleModel.Signer)
            {
                lblFailedUFee.Text = "You have no authorize to do this request";
                divfailedufee.Visible = true;
                ScriptManager.RegisterClientScriptBlock(this, GetType(), "Pop", "var isModalUpdateFee = true", true);
                return;
            }
            if (txtNewFee.Text == "")
            {
                lblFailedAdd.Text = "New fee rate cannot be empty";
                divfailedufee.Visible = true;
                ScriptManager.RegisterClientScriptBlock(this, GetType(), "Pop", "var isModalUpdateFee = true", true);
                return;
            }
            if (Convert.ToDouble(txtNewFee.Text) < 0)
            {
                lblFailedAdd.Text = "New fee rate cannot be less than 0";
                divfailedufee.Visible = true;
                ScriptManager.RegisterClientScriptBlock(this, GetType(), "Pop", "var isModalUpdateFee = true", true);
                return;
            }

            CardTypeModel uw = new CardTypeModel();
            uw.ID = Convert.ToInt32(hfTID.Value);
            uw.CardTypeId = Convert.ToInt32(hfCID.Value);
            uw.RechargeFeeRate = txtNewFee.Text;


            OutputModel op = new OutputModel();
            op = us.updateCardDepositFee(uw);
            if (op.Status == "success")
            {
                lblSuccess.Text = op.Message;
                divsuccess.Visible = true;
                txtNewFee.Text = "";
                bindGV();
            }
            else
            {
                lblError.Text = op.Message;
                divfailed.Visible = true;
            }
        }
    }
}