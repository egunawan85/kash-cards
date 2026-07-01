using Newtonsoft.Json;
using QryptoCard.Dashboard.Models;
using QryptoCard.Dashboard.Models.Service;
using QryptoCard.Dashboard.Services;
using QryptoCard.Sec;
using System;
using System.Globalization;
using System.Web;

namespace QryptoCard.Dashboard.card
{
    // Deposit-into-card funding flow (Phase E, E1/E5/E6). One screen, three server states:
    //
    //   CHOOSE  -> the user enters the amount to load; a note explains the fee shape. Continue creates
    //             a per-intent Runegate invoice via the funding endpoints.
    //   TRACK   -> the invoice's OWN address + QR + the exact amount to send, the authoritative fee
    //             breakdown (server-returned, never client-computed), and a 3-stage tracker. A small
    //             client poller (backoff 5s->15s->30s->60s, then stops) advances it; the user can leave
    //             and we email them. Cancel is available while still awaiting funds.
    //   ERROR   -> a legible creation/lookup failure.
    //
    // Entry points (query string):
    //   ?type=<cardTypeId>            new card  (from the catalog "Get a card")
    //   ?topup=<cardNo>               top up an owned card (from the card detail "Add funds")
    //   ?intent=<intentId>            RESUME tracking an in-progress intent (card-list tile / dashboard)
    //
    // The whole feature is dark behind CardFundingStreamingEnabled: while OFF the endpoints return a
    // failure and this page shows the error state — no existing page/behavior changes.
    public partial class fundcard : System.Web.UI.Page
    {
        CardService cs = new CardService();

        protected void Page_Load(object sender, EventArgs e)
        {
            if (Common.checkID())
            {
                if (!IsPostBack) Init();
            }
            else
            {
                if (Master.checkCookies())
                {
                    if (!IsPostBack) Init();
                }
                else
                    Master.forceLogin();
            }
        }

        void Init()
        {
            hfStatusUrl.Value = ResolveUrl("~/card/fundingstatus.aspx");
            hfCardsUrl.Value = ResolveUrl("~/card/mycardlist");

            string intent = Request.QueryString["intent"];
            if (!string.IsNullOrEmpty(intent)) { ResumeTracking(intent); return; }

            string topup = Request.QueryString["topup"];
            string type = Request.QueryString["type"];

            if (!string.IsNullOrEmpty(topup))
            {
                hfMode.Value = "topup";
                hfCardNo.Value = topup;
                string last4 = Digits(topup);
                last4 = last4.Length >= 4 ? last4.Substring(last4.Length - 4) : last4;
                litContext.Text = "Add funds to your card ending " + Server.HtmlEncode(last4)
                    + ". Enter how much you want to load — you'll pay by crypto deposit.";
                ShowChoose();
                return;
            }
            if (!string.IsNullOrEmpty(type))
            {
                hfMode.Value = "new";
                hfCardTypeId.Value = type;
                litContext.Text = "Fund your new card. Enter how much you want to load onto it — "
                    + "you'll pay by crypto deposit and the card is issued once it lands.";
                ShowChoose();
                return;
            }

            // No card chosen — send them to the catalog rather than a dead form.
            ShowError("Choose a card to fund first.", ResolveUrl("~/card/cardlist"), "Browse cards");
        }

        // ---- CHOOSE -> create the intent ------------------------------------
        protected void btnContinue_Click(object sender, EventArgs e)
        {
            decimal amount;
            if (!decimal.TryParse((txtAmount.Text ?? "").Trim(), NumberStyles.Number, CultureInfo.InvariantCulture, out amount)
                || amount <= 0m)
            {
                litContext.Text = "Enter a valid amount to load.";
                ShowChoose();
                return;
            }

            OutputModel op;
            if (hfMode.Value == "topup")
            {
                op = cs.createFundingTopUp(hfCardNo.Value, amount);
            }
            else
            {
                long typeId;
                long.TryParse(hfCardTypeId.Value, out typeId);
                op = cs.createFundingIntent(typeId, amount);
            }

            if (op != null && op.Status == "success" && op.Data != null)
            {
                FundingIntentModel m = null;
                try { m = JsonConvert.DeserializeObject<FundingIntentModel>(op.Data.ToString()); }
                catch { m = null; }
                if (m != null && !string.IsNullOrEmpty(m.IntentID))
                {
                    RenderTrack(m);
                    return;
                }
            }
            // Creation failed (bad amount below the minimum, feature OFF, upstream error): keep the user
            // on the choose screen with the reason.
            litContext.Text = (op != null && !string.IsNullOrEmpty(op.Message))
                ? Server.HtmlEncode(op.Message)
                : "We couldn't start this funding. Please try again.";
            ShowChoose();
        }

        // ---- RESUME tracking an existing intent -----------------------------
        void ResumeTracking(string intentId)
        {
            OutputModel op = cs.getFundingIntentStatus(intentId);
            if (op != null && op.Status == "success" && op.Data != null)
            {
                FundingIntentModel m = null;
                try { m = JsonConvert.DeserializeObject<FundingIntentModel>(op.Data.ToString()); }
                catch { m = null; }
                if (m != null && !string.IsNullOrEmpty(m.IntentID))
                {
                    RenderTrack(m);
                    return;
                }
            }
            ShowError("We couldn't find that card funding. It may have already completed.",
                ResolveUrl("~/card/mycardlist"), "View your cards");
        }

        // ---- CANCEL (only while still awaiting funds) -----------------------
        protected void btnCancel_Click(object sender, EventArgs e)
        {
            string id = hfIntentId.Value;
            if (string.IsNullOrEmpty(id)) { ShowChooseGone(); return; }

            OutputModel op = cs.cancelFundingIntent(id);
            // Whether it cancelled or was already past cancelling, land on a clean terminal message
            // rather than a stale tracker.
            pnlChoose.Visible = false;
            pnlTrack.Visible = false;
            pnlError.Visible = true;
            if (op != null && op.Status == "success")
                litError.Text = "This card funding was cancelled. Any crypto already received stays in "
                    + "your available balance for your next purchase.";
            else
                litError.Text = (op != null && !string.IsNullOrEmpty(op.Message))
                    ? Server.HtmlEncode(op.Message)
                    : "This funding can no longer be cancelled.";
            litErrorAction.Text = ActionLink(ResolveUrl("~/card/mycardlist"), "View your cards");
        }

        // ---- rendering ------------------------------------------------------

        void ShowChoose()
        {
            pnlChoose.Visible = true;
            pnlTrack.Visible = false;
            pnlError.Visible = false;
        }

        void ShowChooseGone()
        {
            ShowError("This funding is no longer available.", ResolveUrl("~/card/mycardlist"), "View your cards");
        }

        void ShowError(string message, string actionUrl, string actionText)
        {
            pnlChoose.Visible = false;
            pnlTrack.Visible = false;
            pnlError.Visible = true;
            litError.Text = Server.HtmlEncode(message);
            litErrorAction.Text = ActionLink(actionUrl, actionText);
        }

        void RenderTrack(FundingIntentModel m)
        {
            hfIntentId.Value = m.IntentID;

            bool isTopUp = string.Equals(m.Kind, "topup", StringComparison.OrdinalIgnoreCase)
                || hfMode.Value == "topup";

            decimal face = m.Face ?? 0m;
            decimal price = m.Price ?? 0m;
            decimal pct = m.PercentageFee ?? 0m;
            decimal fixedFee = m.FixedFee ?? 0m;
            decimal total = m.ExpectedTotal ?? (face + price + pct + fixedFee);
            string coin = string.IsNullOrEmpty(m.Coin) ? "USDT" : m.Coin;
            string net = string.IsNullOrEmpty(m.Network) ? "TRC20" : m.Network;
            string addr = m.DepositAddress ?? "";

            // Authoritative, server-returned fee breakdown (never re-derived on the client).
            var b = new System.Text.StringBuilder();
            b.Append("<div class=\"fc-summary\">");
            if (!isTopUp && price > 0m)
                SumRow(b, "Card price", Money(price));
            SumRow(b, isTopUp ? "Amount to load" : "Loaded onto card", Money(face));
            if (pct > 0m) SumRow(b, "Service fee", Money(pct));
            if (fixedFee > 0m) SumRow(b, "Network fee", Money(fixedFee));
            b.Append("<div class=\"fc-sum-row fc-sum-total\"><span>Total to send</span><span class=\"fc-sum-v\">"
                + Money(total) + " " + Server.HtmlEncode(coin) + "</span></div>");
            b.Append("</div>");
            litBreakdown.Text = b.ToString();

            // Address + QR + exact amount.
            string qr = "";
            try { qr = string.IsNullOrEmpty(addr) ? "" : Common.GenerateQrDataUri(addr); } catch { qr = ""; }
            var a = new System.Text.StringBuilder();
            a.Append("<div class=\"fc-pay\">");
            a.Append("<div class=\"fc-pay-amt\">Send exactly <b>" + Money(total) + " " + Server.HtmlEncode(coin)
                + "</b> <span class=\"fc-pay-net\">(" + Server.HtmlEncode(coin) + " · " + Server.HtmlEncode(net) + ")</span></div>");
            if (!string.IsNullOrEmpty(qr))
                a.Append("<div class=\"fc-qr\"><img src=\"" + qr + "\" alt=\"Deposit address QR\" /></div>");
            a.Append("<div class=\"fc-addr-label\">Deposit address</div>");
            a.Append("<div class=\"fc-addr\" id=\"fcAddr\">" + Server.HtmlEncode(addr) + "</div>");
            a.Append("</div>");
            litPay.Text = a.ToString();

            // Cancel is only meaningful while still Pending; hide once funds are moving/terminal (the
            // client poller also hides it live).
            btnCancel.Visible = CardFundingDisplay.IsOpen(m.Status)
                && string.Equals(m.Status, CardFundingStatuses.Pending, StringComparison.OrdinalIgnoreCase);

            pnlChoose.Visible = false;
            pnlError.Visible = false;
            pnlTrack.Visible = true;
        }

        // ---- small helpers --------------------------------------------------

        void SumRow(System.Text.StringBuilder b, string label, string value)
        {
            b.Append("<div class=\"fc-sum-row\"><span>" + Server.HtmlEncode(label)
                + "</span><span class=\"fc-sum-v\">" + value + "</span></div>");
        }

        string Money(decimal v) { return CardFundingDisplay.FormatMoney(v); }

        static string Digits(string s)
        {
            var sb = new System.Text.StringBuilder();
            foreach (char c in s ?? "") if (char.IsDigit(c)) sb.Append(c);
            return sb.ToString();
        }

        string ActionLink(string url, string text)
        {
            return "<a class=\"btn btn-primary\" href=\"" + Server.HtmlEncode(url) + "\">"
                + Server.HtmlEncode(text) + "</a>";
        }
    }
}
