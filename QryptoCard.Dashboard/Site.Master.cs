using Newtonsoft.Json;
using QryptoCard.Dashboard.Models.Service;
using QryptoCard.Dashboard.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using QryptoCard.Dashboard.Services;

namespace QryptoCard.Dashboard
{
    public partial class SiteMaster : MasterPage
    {
        UserService us = new UserService();
        CardService cs = new CardService();

        protected void Page_Load(object sender, EventArgs e)
        {
            //lblFullname.Text = SessionLib.Current.FirstName + " " + SessionLib.Current.LastName;
            //lblEmail.Text = SessionLib.Current.Email;
            //lblRole.Text = SessionLib.Current.Role;

            //if (SessionLib.Current.Role == RoleModel.Owner || SessionLib.Current.Role == RoleModel.Admin)
            //{
            //    liuser.Visible = true;
            //}
            //if (SessionLib.Current.Role == RoleModel.Viewer)
            //    menusetting.Visible = false;
            //liuser.HRef = (HttpContext.Current.Handler as Page).ResolveUrl("~/User/Users");
            if (SessionLib.Current.UserID != null)
            {
                lblFullname.Text = "Hi there!";
                //lblFullname.Text = SessionLib.Current.FirstName + " " + SessionLib.Current.LastName;
                lblEmail.Text = SessionLib.Current.Email;
                getBalance();
                getCards();
            }
        }

        void getBalance()
        {
            UserBalanceModel dt;

            var req = new UserBalanceModel();
            OutputModel op = new OutputModel();
            op = us.getBalance(req);
            if (op.Status == "success")
            {
                dt = JsonConvert.DeserializeObject<UserBalanceModel>(op.Data.ToString());
                lblBalance.InnerHtml = dt.Balance.ToString() + " USDT";
                //lblBalance.InnerHtml = dt.Balance.ToString() + " " + dt.Currency;
            }
            else
            {

                //rptCard.DataSource = null;
                //rptCard.DataBind();
            }
        }

        void getCards()
        {
            List<CardModel> dt;

            var req = new CardModel();
            OutputModel op = new OutputModel();
            op = cs.getCardList(req);
            if (op.Status == "success")
            {
                dt = JsonConvert.DeserializeObject<List<CardModel>>(op.Data.ToString());
                lblTotalCards.InnerHtml = dt.Count.ToString();

                if (dt.Count > 0)
                {
                    for (int i = 0; i < dt.Count; i++)
                    {
                        if (dt[i].Organization == "Visa")
                            dt[i].LogoURL = "https://www.svgrepo.com/show/362035/visa-3.svg";
                        else if (dt[i].Organization == "MasterCard")
                            dt[i].LogoURL = "https://www.svgrepo.com/show/508703/mastercard.svg";
                        else
                            dt[i].LogoURL = "https://www.svgrepo.com/show/328132/discover.svg";


                        dt[i].DetailURL = KeyModel.DETAIL_OWN_URL + dt[i].ID;

                        dt[i].CardNumber = MaskDigits(dt[i].CardNumber);

                        //dt[i].CardNumber = String.Format("{0:0000 0000 0000 0000}", Convert.ToInt64(dt[i].CardNumber));
                    }
                    rptCard.DataSource = dt;
                    rptCard.DataBind();
                }
                else
                {

                    rptCard.DataSource = null;
                    rptCard.DataBind();
                }
            }
            else
            {

                //rptCard.DataSource = null;
                //rptCard.DataBind();
            }
        }

        string MaskDigits(string input)
        {
            //take first 6 characters
            string firstPart = input.Substring(0, 4);
            string secondPart = input.Substring(4, 2);

            //take last 4 characters
            int len = input.Length;
            string lastPart = input.Substring(len - 4, 4);
            string maskedDigits = new String('*', 4);

            return firstPart +  " " + secondPart + "** **** " + lastPart;
        }
        public bool checkCookies()
        {
            //var x = Request.Cookies["QryptoEmail"].Value;
            if (Request.Cookies["QryptoCardEmail"] != null)
            {
                var dt = JsonConvert.DeserializeObject<UserModel>(Request.Cookies["QryptoCardData"].Value);
                SessionLib.Current.SessionID = dt.UserID;
                SessionLib.Current.UserID = dt.UserID;
                SessionLib.Current.FirstName = dt.FirstName;
                SessionLib.Current.LastName = dt.LastName;
                SessionLib.Current.Email = Request.Cookies["QryptoCardEmail"].Value;
                SessionLib.Current.DateJoin = dt.DateJoin;
                SessionLib.Current.Password = Request.Cookies["QryptoCardPassword"].Value;
                return true;
            }
            else
                return false;
        }

        public void forceLogin()
        {
            Response.Redirect("~/login");
        }
    }
}