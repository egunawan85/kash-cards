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

namespace QryptoCard.Dashboard.Admin.setting
{
    public partial class admin : System.Web.UI.Page
    {
        AdminService us = new AdminService();

        protected void Page_Load(object sender, EventArgs e)
        {
            Master.setTitle("Administrator");
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
                        btnGenerate.Visible = true;
                        bindGV();
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

            List<AdminModel> dt;
            OutputModel op = new OutputModel();
            AdminFilterModel mf = new AdminFilterModel();
            mf.Role = ddlRole.SelectedValue;
            if (ddlStatus.SelectedValue == "all")
                mf.isVerified = false;
            else
                mf.isVerified = true;

            op = us.getAdminList(mf);
            if (op.Status == "success")
            {
                dt = JsonConvert.DeserializeObject<List<AdminModel>>(op.Data.ToString());
                if (dt.Count > 0)
                {
                    for (int i = 0; i < dt.Count; i++)
                    {
                        dt[i].FirstName = dt[i].FirstName + " " + dt[i].LastName;
                        dt[i].InvitedByFirstName = dt[i].InvitedByFirstName + " " + dt[i].InvitedByLastName;
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
            //if (uw.isToken == 1)
            //    uw.TokenID = ddlTokenAdd.SelectedValue.Trim();
            //uw.Amount = Convert.ToDouble(txtAmountAdd.Text);


            OutputModel op = new OutputModel();
            op = us.inviteAdmin(uw);
            if (op.Status == "success")
            {
                lblSuccess.Text = op.Message;
                divsuccess.Visible = true;
                txtFirstNameAdd.Text = "";
                txtLastNameAdd.Text = "";
                txtEmailAdd.Text = "";
                bindGV();
            }
            else
            {
                lblError.Text = op.Message;
                divfailed.Visible = true;
            }
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
            btnBanUser.Visible = true;
            getAdminDetail(false, hfTID.Value);

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

            //hfMerchantID.Value = id;
            //lblBalanceTopup.Text = row.Cells[8].Text;
            //lblNameTopup.Text = row.Cells[1].Text;
            //lblBatchTopup.Text = row.Cells[6].Text;
            btnBanUser.Visible = false;
            getAdminDetail(false, hfTID.Value);
        }

        void getAdminDetail(bool fl, string uid)
        {
            if (fl) divheaderdetail.Visible = true;
            else divheaderdetail.Visible = false;
            AdminModel z = new AdminModel();
            z.AdminID = uid;

            var q = us.getAdminDetail(z);
            if (q.Status == "success")
            {
                var dt = JsonConvert.DeserializeObject<AdminModel>(q.Data.ToString());
                txtFirstNameDetail.Text = dt.FirstName;
                txtLastNameDetail.Text = dt.LastName;
                txtRoleDetail.Text = dt.Role;
                txtEmailDetail.Text = dt.Email;
                txtPhoneDetail.Text = dt.Phone;
                txtCreatedDateDetail.Text = dt.DateJoin.ToString();
                setVerifiedStatus(dt.isActive.Value);
                txtInvitedByDetail.Text = dt.InvitedByFirstName + " " + dt.InvitedByLastName;

                ScriptManager.RegisterClientScriptBlock(this, GetType(), "Pop", "var isModalDetail = true", true);
            }
            else
            {
                lblError.Text = q.Message;
                divfailed.Visible = true;
            }
        }

        void setVerifiedStatus(int status)
        {
            if (status == 1)
            {
                badgeVerfied.Visible = true;
                badgeVerfiedNot.Visible = false;
            }
            else
            {
                badgeVerfied.Visible = false;
                badgeVerfiedNot.Visible = true;
            }
        }

        protected void btnCopyAddrDetail_ServerClick(object sender, EventArgs e)
        {
            ClientScript.RegisterStartupScript(GetType(), "Javascript", "javascript:copyAddress('" + txtInvitedByDetail.Text + "'); ", true);
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

            var x = us.banAdmin(z);
            if (x.Status == "success")
            {
                lblSuccess.Text = x.Message;
                divsuccess.Visible = true;
                bindGV();
                ScriptManager.RegisterClientScriptBlock(this, GetType(), "Pop", "var isModalDetail = false", true);
            }
            else
            {
                lblFailedDetail.Text = x.Message;
                divfaileddetail.Visible = true;
                ScriptManager.RegisterClientScriptBlock(this, GetType(), "Pop", "var isModalDetail = true", true);
            }
        }

        protected void ddlRoleAdd_SelectedIndexChanged(object sender, EventArgs e)
        {
            ScriptManager.RegisterClientScriptBlock(this, GetType(), "Pop", "var isModalAdd = true", true);
        }
    }
}