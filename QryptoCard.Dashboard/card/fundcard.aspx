<%@ Page Title="Fund Card" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="fundcard.aspx.cs" Inherits="QryptoCard.Dashboard.card.fundcard" %>

<%@ MasterType VirtualPath="~/Site.Master" %>

<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" runat="server">
    <asp:HiddenField runat="server" ID="hfMode" />
    <asp:HiddenField runat="server" ID="hfCardTypeId" />
    <asp:HiddenField runat="server" ID="hfCardNo" />
    <asp:HiddenField runat="server" ID="hfIntentId" />
    <asp:HiddenField runat="server" ID="hfStatusUrl" />
    <asp:HiddenField runat="server" ID="hfCardsUrl" />

    <style>
        .fc-wrap { max-width: 620px; margin: 0 auto; }
        .fc-block { border: 1px solid var(--line); border-radius: 14px; padding: 20px; margin-bottom: 18px; background: rgba(255, 255, 255, .02); }
        .fc-lead { color: var(--ink-3); font-size: .95rem; line-height: 1.5; margin-bottom: 16px; display: block; }
        .fc-amount { display: flex; align-items: stretch; border: 1px solid var(--line-2); border-radius: 12px; overflow: hidden; background: rgba(255, 255, 255, .03); margin-bottom: 8px; }
        .fc-amount-cur { display: flex; align-items: center; padding: 0 14px; background: rgba(255, 255, 255, .04); color: var(--ink-3); font-family: var(--font-mono); font-size: .82rem; border-right: 1px solid var(--line-2); }
        .fc-amount input { flex: 1; border: none; background: transparent; color: var(--ink); font-size: 1.05rem; padding: .8em 1em; outline: none; }
        .fc-note { color: var(--ink-faint); font-size: .82rem; margin-bottom: 16px; display: block; }
        .fc-btn { width: 100%; }

        /* Fee summary */
        .fc-summary { border: 1px solid var(--line); border-radius: 14px; padding: 4px 18px; margin-bottom: 18px; }
        .fc-sum-row { display: flex; justify-content: space-between; align-items: center; padding: 12px 0; border-bottom: 1px solid var(--line); font-size: .92rem; color: var(--ink-2); }
        .fc-sum-row:last-child { border-bottom: none; }
        .fc-sum-v { font-family: var(--font-mono); font-weight: 600; color: var(--ink); }
        .fc-sum-total { font-weight: 700; }
        .fc-sum-total .fc-sum-v { color: var(--cyan-bright); }

        /* Pay block */
        .fc-pay { text-align: center; }
        .fc-pay-amt { font-size: 1rem; color: var(--ink-2); margin-bottom: 14px; }
        .fc-pay-amt b { color: var(--cyan-bright); font-family: var(--font-mono); }
        .fc-pay-net { color: var(--ink-faint); font-size: .82rem; }
        .fc-qr { display: inline-block; padding: 10px; background: #fff; border-radius: 12px; margin-bottom: 14px; }
        .fc-qr img { display: block; width: 190px; height: 190px; }
        .fc-addr-label { color: var(--ink-faint); font-size: .78rem; text-transform: uppercase; letter-spacing: .06em; margin-bottom: 4px; }
        .fc-addr { font-family: var(--font-mono); font-size: .9rem; color: var(--ink); word-break: break-all; background: rgba(255, 255, 255, .03); border: 1px solid var(--line-2); border-radius: 10px; padding: 10px 12px; }

        /* 3-stage vertical tracker */
        .fc-track { list-style: none; margin: 18px 0 4px; padding: 0; }
        .fc-track li { display: flex; align-items: flex-start; gap: 12px; padding-bottom: 18px; position: relative; }
        .fc-track li:not(:last-child)::before { content: ""; position: absolute; left: 11px; top: 26px; bottom: 0; width: 2px; background: var(--line-2); }
        .fc-track .fc-dot { flex: 0 0 auto; width: 24px; height: 24px; border-radius: 50%; border: 2px solid var(--line-2); background: var(--panel, #0c141c); display: flex; align-items: center; justify-content: center; font-size: .7rem; color: var(--ink-faint); }
        .fc-track .fc-st-body { flex: 1; }
        .fc-track .fc-st-title { font-weight: 600; color: var(--ink-3); font-size: .95rem; }
        .fc-track .fc-st-sub { color: var(--ink-faint); font-size: .82rem; margin-top: 2px; }
        .fc-track li.done .fc-dot { border-color: var(--cyan); background: var(--cyan); color: #04222a; }
        .fc-track li.done .fc-st-title { color: var(--ink); }
        .fc-track li.active .fc-dot { border-color: var(--cyan-bright); color: var(--cyan-bright); }
        .fc-track li.active .fc-st-title { color: var(--ink); }

        .fc-leave { color: var(--ink-3); font-size: .88rem; background: rgba(0, 230, 255, .06); border: 1px solid var(--line-2); border-radius: 12px; padding: 12px 14px; margin: 6px 0 16px; }
        .fc-done, .fc-fail { text-align: center; padding: 8px 0 4px; }
        .fc-done .fc-big, .fc-fail .fc-big { font-family: var(--font-display); font-size: 1.2rem; font-weight: 700; margin-bottom: 8px; }
        .fc-done .fc-big { color: var(--cyan-bright); }
        .fc-fail .fc-big { color: #ffb4b4; }
        .fc-muted { color: var(--ink-3); font-size: .9rem; }
        .fc-cancel { margin-top: 8px; }
        .fc-cancel .btn { width: 100%; }
    </style>

    <!--begin::Page header-->
    <div class="dash-top">
        <div>
            <h1>Fund your card</h1>
            <div class="sub">Load your card by depositing crypto — no wallet top-up step.</div>
        </div>
    </div>
    <!--end::Page header-->

    <div class="fc-wrap">

        <!-- ========== CHOOSE ========== -->
        <asp:Panel runat="server" ID="pnlChoose" Visible="false">
            <div class="fc-block">
                <span class="fc-lead"><asp:Literal runat="server" ID="litContext" /></span>
                <div class="fc-amount">
                    <span class="fc-amount-cur">USD</span>
                    <asp:TextBox runat="server" ID="txtAmount" TextMode="Number" placeholder="0.00" />
                </div>
                <span class="fc-note">You'll see the card price, our service fee and the network fee before you send anything.</span>
                <asp:Button runat="server" ID="btnContinue" CssClass="btn btn-primary fc-btn" Text="Continue" OnClick="btnContinue_Click" />
            </div>
        </asp:Panel>

        <!-- ========== TRACK ========== -->
        <asp:Panel runat="server" ID="pnlTrack" Visible="false">
            <div class="fc-block">
                <asp:Literal runat="server" ID="litBreakdown" />
                <asp:Literal runat="server" ID="litPay" />
            </div>

            <div class="fc-block">
                <ul class="fc-track" id="fcTrack">
                    <li data-stage="0" id="fcStage0">
                        <span class="fc-dot">1</span>
                        <span class="fc-st-body">
                            <span class="fc-st-title">Waiting for your deposit</span>
                            <span class="fc-st-sub" id="fcStage0sub">Send the exact amount above to the address.</span>
                        </span>
                    </li>
                    <li data-stage="1" id="fcStage1">
                        <span class="fc-dot">2</span>
                        <span class="fc-st-body">
                            <span class="fc-st-title">Funding your card</span>
                            <span class="fc-st-sub">This can take a few minutes.</span>
                        </span>
                    </li>
                    <li data-stage="2" id="fcStage2">
                        <span class="fc-dot">3</span>
                        <span class="fc-st-body">
                            <span class="fc-st-title">Your card is ready</span>
                            <span class="fc-st-sub">We'll show it in your cards.</span>
                        </span>
                    </li>
                </ul>

                <div class="fc-leave" id="fcLeave">
                    You can close this page — we'll email you the moment your card is ready. It'll also appear in <b>My Cards</b>.
                </div>

                <div class="fc-done" id="fcDone" style="display:none">
                    <div class="fc-big">Your card is ready 🎉</div>
                    <div class="fc-muted" id="fcDoneMsg">Your funds have landed and the card is live.</div>
                    <div style="margin-top:14px"><a class="btn btn-primary" id="fcCardLink" href="#">View your cards</a></div>
                </div>

                <div class="fc-fail" id="fcFail" style="display:none">
                    <div class="fc-big">This funding didn't complete</div>
                    <div class="fc-muted" id="fcFailMsg">Any crypto already received stays in your available balance for your next purchase.</div>
                    <div style="margin-top:14px"><a class="btn btn-line" id="fcFailLink" href="#">View your cards</a></div>
                </div>

                <div class="fc-cancel">
                    <asp:Button runat="server" ID="btnCancel" CssClass="btn btn-line" Text="Cancel this funding" OnClick="btnCancel_Click" Visible="false" />
                </div>
            </div>
        </asp:Panel>

        <!-- ========== ERROR ========== -->
        <asp:Panel runat="server" ID="pnlError" Visible="false">
            <div class="fc-block" style="text-align:center">
                <div class="fc-muted" style="margin-bottom:14px"><asp:Literal runat="server" ID="litError" /></div>
                <asp:Literal runat="server" ID="litErrorAction" />
            </div>
        </asp:Panel>

    </div>

    <script type="text/javascript">
        (function () {
            var track = document.getElementById('fcTrack');
            if (!track) return; // not on the tracker view

            var statusUrl = document.getElementById('<%= hfStatusUrl.ClientID %>').value;
            var intentId = document.getElementById('<%= hfIntentId.ClientID %>').value;
            var cardsUrl = document.getElementById('<%= hfCardsUrl.ClientID %>').value;
            if (!intentId) return;

            var cardLink = document.getElementById('fcCardLink');
            var failLink = document.getElementById('fcFailLink');
            if (cardLink) cardLink.href = cardsUrl;
            if (failLink) failLink.href = cardsUrl;

            // Mirror QryptoCard.Sec.CardFundingDisplay: status -> stage (0 waiting, 1 funding, 2 ready).
            function stageOf(s) {
                s = (s || '').toLowerCase();
                if (s === 'completed') return 2;
                if (s === 'funding' || s === 'confirming' || s === 'issuing') return 1;
                return 0;
            }
            function isFailure(s) {
                s = (s || '').toLowerCase();
                return s === 'failed' || s === 'expired' || s === 'cancelled';
            }
            function isTerminal(s) {
                s = (s || '').toLowerCase();
                return s === 'completed' || isFailure(s);
            }

            function paint(stage) {
                for (var i = 0; i <= 2; i++) {
                    var li = document.getElementById('fcStage' + i);
                    if (!li) continue;
                    li.className = '';
                    if (i < stage) li.className = 'done';
                    else if (i === stage) li.className = 'active';
                }
            }

            function showDone(data) {
                paint(2);
                document.getElementById('fcStage2').className = 'done';
                var done = document.getElementById('fcDone');
                var leave = document.getElementById('fcLeave');
                var cancel = document.querySelector('.fc-cancel');
                if (done) done.style.display = 'block';
                if (leave) leave.style.display = 'none';
                if (cancel) cancel.style.display = 'none';
                // Overpay note, if the customer sent more than the total.
                try {
                    if (data && data.ReceivedTotal != null && data.ExpectedTotal != null
                        && (+data.ReceivedTotal) > (+data.ExpectedTotal) + 0.009) {
                        var extra = ((+data.ReceivedTotal) - (+data.ExpectedTotal)).toFixed(2);
                        var msg = document.getElementById('fcDoneMsg');
                        if (msg) msg.innerHTML = 'Your card is live. You sent a little extra — $' + extra
                            + ' was added to your available balance.';
                    }
                } catch (e) { }
            }

            function showFail(s) {
                var fail = document.getElementById('fcFail');
                var leave = document.getElementById('fcLeave');
                var cancel = document.querySelector('.fc-cancel');
                if (fail) fail.style.display = 'block';
                if (leave) leave.style.display = 'none';
                if (cancel) cancel.style.display = 'none';
                var msg = document.getElementById('fcFailMsg');
                if (msg && (s || '').toLowerCase() === 'expired')
                    msg.innerHTML = 'This request expired before your deposit arrived. Any crypto already received stays in your available balance.';
            }

            function partialNote(data) {
                try {
                    if (!data || data.ReceivedTotal == null || data.ExpectedTotal == null) return;
                    var got = +data.ReceivedTotal, need = +data.ExpectedTotal;
                    var sub = document.getElementById('fcStage0sub');
                    if (sub && got > 0 && got < need) {
                        sub.innerHTML = 'Received $' + got.toFixed(2) + ' of $' + need.toFixed(2) + ' — send the rest to finish.';
                    }
                } catch (e) { }
            }

            var delays = [5000, 15000, 30000, 60000]; // backoff, then stop and rely on email + My Cards
            var step = 0;

            function apply(data) {
                if (!data) return false;
                var s = data.Status || data.status;
                partialNote(data);
                if ((s || '').toLowerCase() === 'completed') { showDone(data); return true; }
                if (isFailure(s)) { showFail(s); return true; }
                paint(stageOf(s));
                return isTerminal(s);
            }

            function poll() {
                var req = new XMLHttpRequest();
                req.open('GET', statusUrl + '?intent=' + encodeURIComponent(intentId), true);
                req.onreadystatechange = function () {
                    if (req.readyState !== 4) return;
                    var done = false;
                    if (req.status === 200) {
                        try {
                            var body = JSON.parse(req.responseText);
                            if (body && body.status === 'success' && body.data) done = apply(body.data);
                        } catch (e) { }
                    }
                    if (done) return;
                    if (step < delays.length) {
                        setTimeout(poll, delays[step]);
                        step++;
                    }
                    // else: stop polling; the leave-note + email + My Cards cover it.
                };
                req.send();
            }

            poll(); // first check immediately
        })();
    </script>
</asp:Content>
