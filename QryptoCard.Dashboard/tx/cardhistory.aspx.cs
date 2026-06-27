using Newtonsoft.Json;
using QryptoCard.Dashboard.Models;
using QryptoCard.Dashboard.Models.Service;
using QryptoCard.Dashboard.Services;
using System;
using System.Collections.Generic;
using System.Globalization;
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
                if (!IsPostBack) { getFeed(); }
            }
            else
            {
                if (Master.checkCookies())
                {
                    if (!IsPostBack) { getFeed(); }
                }
                else
                    Master.forceLogin();
            }
        }

        // =====================================================================
        // Unified money-activity feed (S-F): card spends + top-ups, account-wide.
        // Each figure is server-returned (trxCardList / depositCardList per card);
        // nothing is computed/fabricated. Only the masked last-4 is shown, never PAN/CVV.
        // =====================================================================
        public class FeedRow
        {
            public DateTime When;
            public string Merchant;
            public string CardLast4;
            public string Category;   // spend | topup | refund | fee | other
            public bool IsCredit;
            public bool IsDeclined;
            public string Tag;        // "", Declined, Refund, Pending
            public string AmountText;
            public string GroupLabel;
            public string SearchKey;
            public string TypeLabel;  // humanized type for the sub-line (Purchase / Refund / Top-up / …)
        }
        public class FeedGroup { public string Label { get; set; } public List<FeedRow> Rows { get; set; } }

        void getFeed()
        {
            var rows = new List<FeedRow>();
            try
            {
                var op = cs.getCardListAll(new CardModel());
                if (op.Status == "success" && op.Data != null)
                {
                    var cards = JsonConvert.DeserializeObject<List<CardModel>>(op.Data.ToString()) ?? new List<CardModel>();
                    foreach (var card in cards)
                    {
                        if (string.IsNullOrEmpty(card.CardNo)) continue;
                        string last4 = Last4(string.IsNullOrEmpty(card.CardNumber) ? card.CardNo : card.CardNumber);

                        try
                        {
                            var sop = cs.trxCardList(new CardModel { CardNo = card.CardNo });
                            if (sop.Status == "success" && sop.Data != null)
                            {
                                var txns = JsonConvert.DeserializeObject<List<CardTransactionModel>>(sop.Data.ToString()) ?? new List<CardTransactionModel>();
                                foreach (var t in txns) rows.Add(SpendRow(t, last4));
                            }
                        }
                        catch { /* one card's spends failing must not blank the whole feed */ }

                        try
                        {
                            var dop = cs.depositCardList(new CardDepositModel { CardNo = card.CardNo });
                            if (dop.Status == "success" && dop.Data != null)
                            {
                                var deps = JsonConvert.DeserializeObject<List<CardDepositModel>>(dop.Data.ToString()) ?? new List<CardDepositModel>();
                                foreach (var d in deps) rows.Add(DepositRow(d, last4));
                            }
                        }
                        catch { /* one card's deposits failing must not blank the whole feed */ }
                    }
                }
            }
            catch { /* leave the empty-state */ }

            var ordered = rows.Where(r => r != null && r.When > DateTime.MinValue)
                              .OrderByDescending(r => r.When).Take(150).ToList();
            foreach (var r in ordered) r.GroupLabel = GroupOf(r.When);

            if (ordered.Count > 0)
            {
                var groups = ordered.GroupBy(r => r.GroupLabel)
                                    .Select(grp => new FeedGroup { Label = grp.Key, Rows = grp.ToList() })
                                    .ToList();
                rptGroups.Visible = true;
                rptGroups.DataSource = groups;
                rptGroups.DataBind();
                divNoFeed.Visible = false;
                divCardFilter.InnerHtml = BuildCardSelect(ordered);
            }
            else
            {
                rptGroups.Visible = false;
                divNoFeed.Visible = true;
                divCardFilter.Visible = false;
            }
        }

        FeedRow SpendRow(CardTransactionModel t, string last4)
        {
            string type = (t.Type ?? "").ToLowerInvariant();
            bool declined = IsDeclinedStatus(t.Status);
            string cat = (type == "refund") ? "refund"
                        : (type == "maintain_fee" || type == "card_patch_fee") ? "fee"
                        : (type == "verification" || type == "void") ? "other"
                        : "spend";
            bool credit = (cat == "refund");
            string tag = declined ? "Declined" : (cat == "refund" ? "Refund" : "");
            string typeLabel = cat == "refund" ? "Refund" : cat == "fee" ? "Card fee"
                             : type == "verification" ? "Card check" : type == "void" ? "Reversal" : "Purchase";
            decimal amt = (decimal)(t.Amount ?? 0);
            decimal auth = (decimal)(t.AuthorizedAmount ?? 0);
            string cur = string.IsNullOrEmpty(t.Currency) ? "USD" : t.Currency;
            return new FeedRow
            {
                When = t.TransactionTime ?? t.SettleDate ?? DateTime.MinValue,
                Merchant = string.IsNullOrEmpty(t.MerchantName) ? "Card transaction" : t.MerchantName,
                CardLast4 = last4,
                Category = cat,
                IsCredit = credit,
                IsDeclined = declined,
                Tag = tag,
                AmountText = MoneyText(credit, amt, cur, auth),
                SearchKey = (t.MerchantName ?? "").ToLowerInvariant(),
                TypeLabel = typeLabel
            };
        }

        FeedRow DepositRow(CardDepositModel d, string last4)
        {
            string sym = string.IsNullOrEmpty(d.Symbol) ? "USDT" : d.Symbol;
            bool ok = string.Equals(d.Status, "success", StringComparison.OrdinalIgnoreCase);
            decimal amt = (decimal)(d.Amount ?? 0);
            string cur = string.IsNullOrEmpty(d.Currency) ? "USD" : d.Currency;
            return new FeedRow
            {
                When = d.DateTransaction ?? DateTime.MinValue,
                Merchant = "Top up · " + sym,
                CardLast4 = last4,
                Category = "topup",
                IsCredit = true,
                IsDeclined = false,
                Tag = ok ? "" : "Pending",
                AmountText = MoneyText(true, amt, cur, amt),
                SearchKey = "top up " + sym.ToLowerInvariant(),
                TypeLabel = "Top-up"
            };
        }

        static string Last4(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            var digits = new string(s.Where(char.IsDigit).ToArray());
            return digits.Length >= 4 ? digits.Substring(digits.Length - 4) : digits;
        }

        static string MoneyText(bool credit, decimal amt, string cur, decimal authUsd)
        {
            string sign = credit ? "+" : "-";
            decimal a = Math.Abs(amt);
            if (string.Equals(cur, "USD", StringComparison.OrdinalIgnoreCase))
                return sign + "$" + a.ToString("0.00", CultureInfo.InvariantCulture);
            string main = sign + cur + " " + a.ToString("#,##0.##", CultureInfo.InvariantCulture);
            if (authUsd > 0) main += " (≈ $" + Math.Abs(authUsd).ToString("0.00", CultureInfo.InvariantCulture) + ")";
            return main;
        }

        static bool IsDeclinedStatus(string s)
        {
            s = (s ?? "").ToLowerInvariant();
            return s == "failed" || s == "fail" || s == "declined" || s == "reject" || s == "rejected";
        }

        static string GroupOf(DateTime when)
        {
            var today = DateTime.UtcNow.Date;
            var d = when.Date;
            if (d == today) return "Today";
            if (d == today.AddDays(-1)) return "Yesterday";
            if (d > today.AddDays(-7)) return "This week";
            return "Earlier";
        }

        string BuildCardSelect(List<FeedRow> rows)
        {
            var cards = rows.Select(r => r.CardLast4).Where(c => !string.IsNullOrEmpty(c)).Distinct().ToList();
            if (cards.Count < 2) { divCardFilter.Visible = false; return ""; }
            var sb = new System.Text.StringBuilder();
            sb.Append("<label class=\"tx-cardselect\">Card <select id=\"tx-card-select\"><option value=\"all\">All cards</option>");
            foreach (var c in cards)
                sb.Append("<option value=\"" + Server.HtmlEncode(c) + "\">•••• " + Server.HtmlEncode(c) + "</option>");
            sb.Append("</select></label>");
            return sb.ToString();
        }

        // ---- Repeater binding helpers (inner row data item = FeedRow) ----
        protected string FeedRowClass(object o) { var r = o as FeedRow; return "txn" + (r != null && r.IsDeclined ? " declined" : ""); }
        protected string FeedIcClass(object o) { var r = o as FeedRow; return "ic" + (r != null && r.IsCredit ? " tx-ic-in" : ""); }
        protected string FeedCat(object o) { var r = o as FeedRow; return r != null ? r.Category : ""; }
        protected string FeedCardKey(object o) { var r = o as FeedRow; return r != null ? r.CardLast4 : ""; }
        protected string FeedSearchKey(object o) { var r = o as FeedRow; return Server.HtmlEncode(r != null ? r.SearchKey : ""); }
        protected string FeedMerchant(object o) { var r = o as FeedRow; return Server.HtmlEncode(r != null ? r.Merchant : ""); }
        protected string FeedWhen(object o) { var r = o as FeedRow; return r != null ? r.When.ToString("dd MMM, HH:mm", CultureInfo.InvariantCulture) : ""; }
        protected string FeedAmtClass(object o) { var r = o as FeedRow; return "amt " + (r != null && r.IsCredit ? "in" : "out"); }
        protected string FeedAmt(object o) { var r = o as FeedRow; return Server.HtmlEncode(r != null ? r.AmountText : ""); }
        protected string FeedCardChip(object o)
        {
            var r = o as FeedRow;
            if (r == null || string.IsNullOrEmpty(r.CardLast4)) return "";
            return "<span class=\"cardchip\">•••• " + Server.HtmlEncode(r.CardLast4) + "</span>";
        }
        // Muted sub-line under the merchant: humanized type · date · masked-card chip.
        protected string FeedSubline(object o)
        {
            var r = o as FeedRow;
            if (r == null) return "";
            var parts = new List<string>();
            if (!string.IsNullOrEmpty(r.TypeLabel)) parts.Add(Server.HtmlEncode(r.TypeLabel));
            parts.Add(Server.HtmlEncode(r.When.ToString("dd MMM, HH:mm", CultureInfo.InvariantCulture)));
            if (!string.IsNullOrEmpty(r.CardLast4)) parts.Add("<span class=\"cardchip\">•••• " + Server.HtmlEncode(r.CardLast4) + "</span>");
            return string.Join(" · ", parts);
        }
        protected string FeedTag(object o)
        {
            var r = o as FeedRow;
            if (r == null || string.IsNullOrEmpty(r.Tag)) return "";
            string cls = r.Tag == "Declined" ? "tx-tag-declined" : r.Tag == "Refund" ? "tx-tag-refund" : "tx-tag-pending";
            return " <span class=\"tx-tag " + cls + "\">" + Server.HtmlEncode(r.Tag) + "</span>";
        }
        protected string FeedIcon(object o)
        {
            var r = o as FeedRow;
            bool credit = r != null && r.IsCredit;
            if (credit)
                return "<svg viewBox=\"0 0 24 24\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"1.7\"><path d=\"M12 5v14M5 12l7 7 7-7\"/></svg>";
            return "<svg viewBox=\"0 0 24 24\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"1.7\"><rect x=\"2\" y=\"5\" width=\"20\" height=\"14\" rx=\"3\"/><path d=\"M2 10h20\"/></svg>";
        }
    }
}
