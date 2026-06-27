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

namespace QryptoCard.Dashboard.card
{
    public partial class mycardlist : System.Web.UI.Page
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
            var req = new CardModel();
            OutputModel op = new OutputModel();
            op = cs.getCardList(req);
            if (op.Status == "success")
            {
                dt = JsonConvert.DeserializeObject<List<CardModel>>(op.Data.ToString());
                if (dt.Count > 0)
                {
                    for (int i = 0; i < dt.Count; i++)
                    {
                        try
                        {
                            if (dt[i].Organization == "Visa")
                                dt[i].LogoURL = "https://www.svgrepo.com/show/362035/visa-3.svg";
                            else if (dt[i].Organization == "MasterCard")
                                dt[i].LogoURL = "https://www.svgrepo.com/show/508703/mastercard.svg";
                            else
                                dt[i].LogoURL = "https://www.svgrepo.com/show/328132/discover.svg";

                            // Card numbers come back MASKED (e.g. "4024 00** **** 0001") — the
                            // real provider never returns a full PAN. Only a clean integer can be
                            // re-grouped; otherwise keep the stored (already grouped + masked)
                            // value as-is. Never Int64.Parse a masked PAN — it throws
                            // FormatException and used to take the whole list down.
                            long pan;
                            if (Int64.TryParse(dt[i].CardNumber, out pan))
                                dt[i].CardNumber = String.Format("{0:0000 0000 0000 0000}", pan);

                            dt[i].DetailURL = KeyModel.DETAIL_OWN_URL + dt[i].ID;

                            dt[i].Param5 = dt[i].Param5 + " " + dt[i].Currency;
                        }
                        catch
                        {
                            // One malformed row must not blank the entire list — leave this
                            // row's raw values and keep rendering the rest.
                        }
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
                // Load failed upstream — bind empty AND surface the reason, rather than
                // silently presenting an empty grid that reads identically to "you have
                // no cards yet" (a silent failure the buy-card screen also guards against).
                rptCard.DataSource = null;
                rptCard.DataBind();
                ShowAlert(op.Message);
            }

        }

        // Surfaces a backend error inline, matching the NewDesign alert idiom used
        // across the re-skin, so an upstream failure is legible instead of looking
        // like an empty card list.
        void ShowAlert(string message)
        {
            pnlAlert.Visible = true;
            lblAlert.Text = Server.HtmlEncode(string.IsNullOrEmpty(message)
                ? "Unable to load your cards. Please try again."
                : message);
        }
    }
}