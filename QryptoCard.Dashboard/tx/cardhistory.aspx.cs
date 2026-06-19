using Newtonsoft.Json;
using QryptoCard.Dashboard.Models;
using QryptoCard.Dashboard.Models.Service;
using QryptoCard.Dashboard.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.HtmlControls;
using System.Web.UI.WebControls;

namespace QryptoCard.Dashboard.tx
{
    public partial class cardhistory : System.Web.UI.Page
    {
        CardService cs = new CardService();
        protected void Page_Load(object sender, EventArgs e)
        {
            if (Common.checkID())
            {

                if (!IsPostBack)
                {
                    getData();

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
                        getData();
                    }
                    else
                    {
                        //bindData("00000000000000000001");
                    }
                }
                else
                    Master.forceLogin();
            }
            //getData();
        }

        void getData()
        {
            List<CardModel> dt;
            OutputModel op = new OutputModel();
            CardModel req = new CardModel();
            op = cs.getCardListAll(req);
            if (op.Status == "success")
            {
                dt = JsonConvert.DeserializeObject<List<CardModel>>(op.Data.ToString());
                if (dt.Count > 0)
                {
                    for (int i = 0; i < dt.Count; i++)
                    {
                        dt[i].FirstName = dt[i].FirstName + " " + dt[i].LastName;
                        dt[i].DetailURL = KeyModel.TXCARD_URL + dt[i].ID;
                        //if (dt[i].Status == "expired")
                        //{
                        //    int asa = Convert.ToInt32((DateTime.Today - dt[i].DateExpired.Value).TotalDays);

                        //    if (asa == 0)
                        //        dt[i].Param5 = "Past due";
                        //    else if (asa == 1)
                        //        dt[i].Param5 = "Past due 1 day";
                        //    else
                        //        dt[i].Param5 = "Past due " + asa.ToString() + " days";


                        //    //dt[i].Param5 = "Past due " + Convert.ToInt32((DateTime.Today - dt[i].DateExpired.Value).TotalDays).ToString() + " days";
                        //}
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

        protected Boolean IsStatusPaid(string i)
        {
            if (i == "paid")
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

        protected Boolean IsStatusExpired(string i)
        {
            if (i == "expired")
                return true;
            else
                return false;
        }

        protected void gvListItem_PageIndexChanging(object sender, GridViewPageEventArgs e)
        {
            gvListItem.PageIndex = e.NewPageIndex;
            getData();
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

        protected void btnInfo_ServerClick(object sender, EventArgs e)
        {
            HtmlButton btn = (HtmlButton)sender;
            GridViewRow row = (GridViewRow)btn.NamingContainer;
            hfID.Value = ((HiddenField)row.FindControl("hfID")).Value;

            ScriptManager.RegisterClientScriptBlock(this, GetType(), "Pop", "var isModalDetail = true", true);
            //hfMerchantID.Value = id;
            //lblBalanceTopup.Text = row.Cells[8].Text;
            //lblNameTopup.Text = row.Cells[1].Text;
            //lblBatchTopup.Text = row.Cells[6].Text;

            //getCustomerDetail(row.Cells[1].Text);
        }

        protected void btnLink_ServerClick(object sender, EventArgs e)
        {
            HtmlButton btn = (HtmlButton)sender;
            GridViewRow row = (GridViewRow)btn.NamingContainer;
            var a = ((HiddenField)row.FindControl("hfURL")).Value;
            Page.ClientScript.RegisterClientScriptBlock(this.GetType(), "openModal", "window.open('" + a + "' ,'_blank');", true);
        }

        protected void btnCancel_ServerClick(object sender, EventArgs e)
        {
            


            HtmlButton btn = (HtmlButton)sender;
            GridViewRow row = (GridViewRow)btn.NamingContainer;
            hfID.Value = ((HiddenField)row.FindControl("hfID")).Value;

            ScriptManager.RegisterClientScriptBlock(this, GetType(), "Pop", "var isModalCancel = true", true);
        }

        protected void btndfailedcancel_ServerClick(object sender, EventArgs e)
        {
            //divfailedcancel.Visible = false;
            //ScriptManager.RegisterClientScriptBlock(this, GetType(), "Pop", "var isModalCancel = true", true);
        }

        protected void btnCancelExec_Click(object sender, EventArgs e)
        {
            CardModel z = new CardModel();
            z.ID = hfID.Value;

            var q = cs.cancelCardTransaction(z);
            if (q.Status == "success")
            {
                var dt = JsonConvert.DeserializeObject<CardModel>(q.Data.ToString());

                getData();
                lblSuccess.InnerHtml = q.Message;
                ScriptManager.RegisterClientScriptBlock(this, GetType(), "Pop", "var isModalSuccess = true", true);
                return;
            }
            else
            {
                lblalert.InnerHtml = q.Message;
                ScriptManager.RegisterClientScriptBlock(this, GetType(), "Pop", "var isModalAlert = true", true);
                return;
            }
        }
    }
}