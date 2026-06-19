using Newtonsoft.Json;
using QryptoCard.Dashboard.Admin.Models;
using QryptoCard.Dashboard.Admin.Models.Service;
using QryptoCard.Dashboard.Admin.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace QryptoCard.Dashboard.Admin
{
    public partial class SiteMaster : MasterPage
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            getData(); 
            if (SessionLib.Current.AdminID != null)
            {
                lblFullname.Text = SessionLib.Current.FirstName + " " + SessionLib.Current.LastName;
                lblEmail.Text = SessionLib.Current.Email;
                lblRole.Text = SessionLib.Current.Role;
            }
        }

        public void forceLogin()
        {
            Response.Redirect("~/login");
        }

        public void setTitle(string title)
        {
            lblTitle.InnerHtml = title;
        }

        void getData()
        {
            DashboardService us = new DashboardService();

            DashboardAdminModel dt;
            OutputModel op = new OutputModel();
            op = us.getDashboardData();
            if (op.Status == "success")
            {
                dt = JsonConvert.DeserializeObject<DashboardAdminModel>(op.Data.ToString());
                lblCardSold.InnerHtml = dt.TotalCards.ToString();
                lblTotalDeposit.InnerText = dt.TotalDeposit.ToString() + " USD";
                lblUserTotal.InnerHtml = dt.TotalUsers.ToString();
            }
            else
            {
                lblCardSold.InnerHtml = "-";
                lblTotalDeposit.InnerText = "-";
                lblUserTotal.InnerHtml = "-";
            }
        }
    }
}