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