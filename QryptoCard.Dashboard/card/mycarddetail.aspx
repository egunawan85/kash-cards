<%@ Page Title="Card Detail" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="mycarddetail.aspx.cs" Inherits="QryptoCard.Dashboard.card.mycarddetail" EnableEventValidation="false" %>

<%@ MasterType VirtualPath="~/Site.Master" %>

<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" runat="server">
    <asp:HiddenField runat="server" ID="hfIsHolderNeeded" />
    <asp:HiddenField runat="server" ID="hfCardID" />
    <asp:HiddenField runat="server" ID="hfCardNo" />
    <asp:HiddenField runat="server" ID="hfReferralLink" />
    <asp:HiddenField runat="server" ID="hfCardTypeID" />
    <asp:HiddenField runat="server" ID="hfHolderID" />
    <asp:HiddenField runat="server" ID="hfDepositFeeRate" />
    <asp:HiddenField runat="server" ID="hfMinDeposit" />
    <asp:HiddenField runat="server" ID="hfMaxDeposit" />
    <asp:HiddenField runat="server" ID="hfCardNumber" />
    <asp:HiddenField runat="server" ID="hfCVV" />
    <asp:HiddenField runat="server" ID="hfExpDate" />
    <asp:HiddenField runat="server" ID="hfCVVDecr" />
    <asp:HiddenField runat="server" ID="hfExpDateDecr" />
    <asp:HiddenField runat="server" ID="hfCardholder" />
    <asp:HiddenField runat="server" ID="hfPhone" />
    <asp:HiddenField runat="server" ID="hfEmail" />
    <asp:HiddenField runat="server" ID="hfAddress" />
    <asp:HiddenField runat="server" ID="hfCity" />
    <asp:HiddenField runat="server" ID="hfState" />
    <asp:HiddenField runat="server" ID="hfCountry" />
    <asp:HiddenField runat="server" ID="hfZipCode" />
    <asp:HiddenField runat="server" ID="hfOTPID" />

    <style>
        .cd-grid { display: grid; grid-template-columns: minmax(0, 1fr) minmax(0, 1.1fr); gap: 22px; align-items: start; margin-top: 20px; }
        @media (max-width: 900px) { .cd-grid { grid-template-columns: 1fr; } }
        .cd-stats { display: flex; gap: 10px; }
        .cd-stat { flex: 1; padding: 14px 16px; border-radius: 12px; background: rgba(255,255,255,.02); border: 1px solid var(--line-2); }
        .cd-stat .n { font-family: var(--font-display); font-weight: 700; font-size: 1.3rem; letter-spacing: -.02em; }
        .cd-stat .l { font-size: .68rem; letter-spacing: .1em; text-transform: uppercase; color: var(--ink-3); margin-top: 2px; }
        .cd-field { display: flex; align-items: center; justify-content: space-between; gap: 12px; padding: 11px 0; border-top: 1px solid var(--line-2); }
        .cd-field:first-child { border-top: 0; }
        .cd-field .k { color: var(--ink-3); font-size: .85rem; }
        .cd-field .v { font-weight: 600; font-family: var(--font-mono); font-size: .92rem; display: flex; align-items: center; gap: 8px; }
        .cd-copy { background: none; border: 1px solid var(--line-2); border-radius: 7px; color: var(--ink-3); cursor: pointer; padding: 3px 7px; font-size: .72rem; }
        .cd-copy:hover { color: var(--cyan); border-color: var(--cyan); }
        .cd-usage { color: var(--ink-2); font-size: .9rem; line-height: 1.6; }
        .cd-usage .k { color: var(--ink-3); font-size: .72rem; letter-spacing: .08em; text-transform: uppercase; display: block; margin: 12px 0 3px; }
        .cd-usage .k:first-child { margin-top: 0; }
        .txn-table, .txn-table tbody, .txn-table tr, .txn-table td { display: block; width: 100%; padding: 0; border: 0; }
        .cd-otp-inputs { display: flex; gap: 8px; justify-content: center; margin: 6px 0 4px; }
        .cd-otp-inputs input { width: 44px; height: 52px; text-align: center; font-size: 1.3rem; font-family: var(--font-mono); border-radius: 10px; border: 1px solid var(--line-2); background: rgba(255,255,255,.03); color: var(--ink); outline: none; }
        .cd-otp-inputs input:focus, .cd-otp-inputs input.filled { border-color: var(--cyan); }
        .cd-presets { display: flex; gap: 8px; flex-wrap: wrap; margin: 10px 0; }
        .cd-presets > div { display: inline-block; }
        .topup-from { display: flex; justify-content: space-between; align-items: center; padding: .7em 1em; border: 1px solid var(--line-2); border-radius: 12px; background: rgba(255, 255, 255, .03); }
        .topup-from-name { color: var(--ink-2); font-size: .9rem; }
        .topup-from-bal { font-family: var(--font-mono); font-weight: 600; color: var(--cyan-bright); }
    </style>

    <div class="dash-top">
        <div>
            <h1>Card details</h1>
            <div class="sub">Manage this card, reveal its details, and top it up.</div>
        </div>
        <div class="dash-top-actions">
            <asp:Literal runat="server" ID="litAddFunds" />
            <a class="btn btn-line" href='<%= ResolveUrl("~/card/mycardlist") %>'>Back to My Cards</a>
        </div>
    </div>

    <span runat="server" id="lblCardPrice" visible="false">Qrypto Card</span>

    <div class="cd-grid">
        <%-- ============ LEFT: card visual + controls + reveal + cardholder ============ --%>
        <div>
            <section class="panel">
                <div class="card3d-wrap">
                    <div class="card-3d">
                        <div class="qcard">
                            <div class="qcard-inner">
                                <div class="qcard-top">
                                    <div class="qcard-brand">K<b>ash</b></div>
                                    <span runat="server" id="lblCardBrand"></span>
                                </div>
                                <div><div class="qcard-chip"></div></div>
                                <div class="qcard-num"><span runat="server" id="lblCardNo">&#8226;&#8226;&#8226;&#8226; &#8226;&#8226;&#8226;&#8226; &#8226;&#8226;&#8226;&#8226; 0000</span></div>
                                <div class="qcard-bottom">
                                    <div><div class="lab">Card holder</div><div class="val"><span runat="server" id="lblCardname">&mdash;</span></div></div>
                                    <div class="qcard-logo">K</div>
                                </div>
                            </div>
                        </div>
                    </div>
                </div>
                <div class="card-controls" style="grid-template-columns: 1fr 1fr; margin-top: 18px;">
                    <button class="ctrl-btn" runat="server" id="btnRecharge" onserverclick="btnRecharge_ServerClick">
                        <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.7"><path d="M12 5v14M5 12l7 7 7-7" /></svg> Top up
                    </button>
                    <button class="ctrl-btn" runat="server" id="btnDetail" onserverclick="btnDetail_ServerClick">
                        <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.7"><circle cx="12" cy="12" r="3" /><path d="M2 12s3.6-7 10-7 10 7 10 7-3.6 7-10 7-10-7-10-7z" /></svg> Reveal details
                    </button>
                </div>
            </section>

            <%-- Revealed card details — populated only after a valid OTP (getCardDetails). --%>
            <section class="panel" runat="server" id="viewdetails" visible="false" style="margin-top: 20px;">
                <div class="panel-h"><h3>Card number</h3></div>
                <div class="cd-field"><span class="k">Card number</span><span class="v"><span runat="server" id="lblCardNumber">0000 0000 0000 0000</span><button type="button" class="cd-copy" onclick="cdCopy('<%= hfCardNumber.ClientID %>')">Copy</button></span></div>
                <div class="cd-field"><span class="k">CVV</span><span class="v"><span runat="server" id="lblCVV">000</span><button type="button" class="cd-copy" onclick="cdCopy('<%= hfCVVDecr.ClientID %>')">Copy</button></span></div>
                <div class="cd-field"><span class="k">Expiry</span><span class="v"><span runat="server" id="lblExpDate">00/00</span><button type="button" class="cd-copy" onclick="cdCopy('<%= hfExpDateDecr.ClientID %>')">Copy</button></span></div>
            </section>

            <%-- Cardholder PII — same OTP gate as the card details; only shown for cards with a holder. --%>
            <section class="panel" runat="server" id="viewholder" visible="false" style="margin-top: 20px;">
                <div class="panel-h"><h3>Cardholder</h3></div>
                <div class="cd-field"><span class="k">Name</span><span class="v"><span runat="server" id="lblCardholder">-</span><button type="button" class="cd-copy" onclick="cdCopy('<%= hfCardholder.ClientID %>')">Copy</button></span></div>
                <div class="cd-field"><span class="k">Phone</span><span class="v"><span runat="server" id="lblPhone">-</span><button type="button" class="cd-copy" onclick="cdCopy('<%= hfPhone.ClientID %>')">Copy</button></span></div>
                <div class="cd-field"><span class="k">Email</span><span class="v"><span runat="server" id="lblEmail">-</span><button type="button" class="cd-copy" onclick="cdCopy('<%= hfEmail.ClientID %>')">Copy</button></span></div>
                <div class="cd-field"><span class="k">Address</span><span class="v"><span runat="server" id="lblAddress">-</span><button type="button" class="cd-copy" onclick="cdCopy('<%= hfAddress.ClientID %>')">Copy</button></span></div>
                <div class="cd-field"><span class="k">City</span><span class="v"><span runat="server" id="lblCity">-</span><button type="button" class="cd-copy" onclick="cdCopy('<%= hfCity.ClientID %>')">Copy</button></span></div>
                <div class="cd-field"><span class="k">State</span><span class="v"><span runat="server" id="lblState">-</span><button type="button" class="cd-copy" onclick="cdCopy('<%= hfState.ClientID %>')">Copy</button></span></div>
                <div class="cd-field"><span class="k">Country</span><span class="v"><span runat="server" id="lblCountry">-</span><button type="button" class="cd-copy" onclick="cdCopy('<%= hfCountry.ClientID %>')">Copy</button></span></div>
                <div class="cd-field"><span class="k">Zip code</span><span class="v"><span runat="server" id="lblZipCode">-</span><button type="button" class="cd-copy" onclick="cdCopy('<%= hfZipCode.ClientID %>')">Copy</button></span></div>
            </section>
        </div>

        <%-- ============ RIGHT: balance + stats, where-it-works, transactions, deposits ============ --%>
        <div>
            <section class="panel">
                <div class="cd-field" style="padding-top: 0;"><span class="k" style="font-size: .95rem;">Balance</span><span class="v" style="font-size: 1.15rem;"><span runat="server" id="lblCardBalance">$0.00</span></span></div>
                <div class="cd-stats" style="margin-top: 14px;">
                    <div class="cd-stat"><div class="n"><asp:Literal runat="server" ID="litSpent" Text="$0.00" /></div><div class="l">Spent this month</div></div>
                    <div class="cd-stat"><div class="n"><asp:Literal runat="server" ID="litTopped" Text="$0.00" /></div><div class="l">Topped up this month</div></div>
                </div>
            </section>

            <section class="panel" style="margin-top: 20px;">
                <div class="panel-h"><h3>Where this card works</h3></div>
                <div class="cd-usage">
                    <span class="k">Supported usage</span>
                    <span runat="server" id="lblUsage"></span>
                    <span class="k">Fees</span>
                    <span runat="server" id="lblDepositFee"></span>
                    <span class="k">Limits</span>
                    <span runat="server" id="lblMinDeposit"></span><br />
                    <span runat="server" id="lblMaxDeposit"></span>
                </div>
            </section>

            <section class="panel tx-panel" style="margin-top: 20px;">
                <div class="panel-h"><h3>Recent transactions</h3></div>
                <asp:GridView CssClass="txn-table" ID="gvTrxList" runat="server" ShowHeader="false" AutoGenerateColumns="false" DataKeyNames="ID" AllowPaging="True" PageSize="50" AllowCustomPaging="False" GridLines="None">
                    <PagerStyle HorizontalAlign="Center" CssClass="bs4-aspnet-pager" />
                    <Columns>
                        <asp:TemplateField>
                            <ItemTemplate>
                                <asp:HiddenField runat="server" ID="hfID" Value='<%# Eval("ID") %>' />
                                <div class="txn">
                                    <div class="ic"><svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.7"><circle cx="12" cy="12" r="9" /><path d="M8 14c2.5-1 5.5-1 8 0" /></svg></div>
                                    <div class="meta">
                                        <div class="merchant"><%# Server.HtmlEncode((string)Eval("MerchantName")) %></div>
                                        <div class="when"><%# Eval("TransactionTime") %></div>
                                    </div>
                                    <div class="amt out"><%# Eval("AuthorizedAmount") %> USD</div>
                                </div>
                            </ItemTemplate>
                        </asp:TemplateField>
                    </Columns>
                </asp:GridView>
                <div class="tx-empty" runat="server" id="divnotrx">
                    <asp:Label runat="server" ID="lblNoRow" Text="No transactions yet." />
                </div>
            </section>

            <section class="panel tx-panel" style="margin-top: 20px;">
                <div class="panel-h"><h3>Deposit history</h3></div>
                <asp:GridView CssClass="txn-table" ID="gvDepositList" runat="server" ShowHeader="false" AutoGenerateColumns="false" DataKeyNames="ID" AllowPaging="True" PageSize="50" AllowCustomPaging="False" OnRowDataBound="OnRowDataBound" GridLines="None">
                    <PagerStyle HorizontalAlign="Center" CssClass="bs4-aspnet-pager" />
                    <Columns>
                        <asp:TemplateField>
                            <ItemTemplate>
                                <asp:HiddenField runat="server" ID="hfID" Value='<%# Eval("ID") %>' />
                                <div class="txn">
                                    <div class="ic tx-ic-in"><svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.7"><path d="M12 5v14M5 12l7 7 7-7" /></svg></div>
                                    <div class="meta">
                                        <div class="merchant">Top up &middot; USDT</div>
                                        <div class="when"><%# Eval("DateTransaction") %>
                                            <span class="tx-tag tx-tag-pending" runat="server" visible='<%# IsStatusCreated((string)Eval("Status")) %>'>Pending</span>
                                            <span class="tx-tag tx-tag-declined" runat="server" visible='<%# IsStatusExpired((string)Eval("Status")) %>'>Expired</span>
                                            <span class="tx-tag tx-tag-declined" runat="server" visible='<%# IsStatusFailed((string)Eval("Status")) %>'>Failed</span>
                                            <span class="tx-tag tx-tag-declined" runat="server" visible='<%# IsStatusCancelled((string)Eval("Status")) %>'>Cancelled</span>
                                        </div>
                                    </div>
                                    <div class="amt in">+<%# Eval("Total") %> USD</div>
                                </div>
                            </ItemTemplate>
                        </asp:TemplateField>
                    </Columns>
                </asp:GridView>
                <div class="tx-empty" runat="server" id="divnodepo">
                    <asp:Label runat="server" ID="Label2" Text="No deposits yet." />
                </div>
            </section>
        </div>
    </div>
</asp:Content>

<asp:Content ID="Content2" ContentPlaceHolderID="DrawerContent" runat="server">
</asp:Content>

<asp:Content ID="Content3" ContentPlaceHolderID="ModalContent" runat="server">
    <%-- ===== Top-up (recharge) overlay — server-rendered (Visible toggled in code-behind) ===== --%>
    <asp:Panel runat="server" ID="pnlRecharge" Visible="false" CssClass="hist-modal">
        <div class="hist-modal-card">
            <h3>Top up card</h3>
            <div class="hist-banner err" runat="server" id="divfaileddeposit" visible="false">
                <span runat="server" id="lblfaileddeposit">Error message</span>
                <button type="button" class="btn btn-line btn-sm" runat="server" id="btndfaileddeposit" onserverclick="btndfaileddeposit_ServerClick">&times;</button>
            </div>

            <div class="form-label" style="margin-bottom: 6px;">Deposit amount (USD)</div>
            <asp:TextBox class="form-control" runat="server" ID="txtDepositAmount" TextMode="Number" AutoPostBack="true" OnTextChanged="txtDepositAmount_TextChanged" />
            <div class="cd-presets">
                <div runat="server" id="viewrc20"><button type="button" class="btn btn-line btn-sm" runat="server" id="rc20" onserverclick="rc20_ServerClick">$20</button></div>
                <div runat="server" id="viewrc30"><button type="button" class="btn btn-line btn-sm" runat="server" id="rc30" onserverclick="rc30_ServerClick">$30</button></div>
                <button type="button" class="btn btn-line btn-sm" runat="server" id="rc50" onserverclick="rc50_ServerClick">$50</button>
                <button type="button" class="btn btn-line btn-sm" runat="server" id="rc100" onserverclick="rc100_ServerClick">$100</button>
                <button type="button" class="btn btn-line btn-sm" runat="server" id="rc200" onserverclick="rc200_ServerClick">$200</button>
            </div>

            <div class="mb-10">
                <div class="form-label" style="margin-bottom: 6px;">Paid from</div>
                <div class="topup-from">
                    <span class="topup-from-name">Wallet balance</span>
                    <asp:Label runat="server" ID="lblWalletBalance" CssClass="topup-from-bal" Text="&mdash; USDT" />
                </div>
                <div class="hist-banner err" runat="server" id="divOverBalance" visible="false" style="margin-top: 10px;">
                    Amount exceeds your wallet balance &mdash; top up your wallet first.
                </div>
                <asp:HiddenField runat="server" ID="hfWalletBalance" />
            </div>

            <div style="margin-top: 14px; border-top: 1px solid var(--line-2); padding-top: 12px;">
                <div class="cd-field" style="border-top: 0; padding-top: 0;"><span class="k">You need to pay</span><span class="v"><span runat="server" id="lblTotal">0 USD</span></span></div>
                <div class="cd-field"><span class="k">You will deposit</span><span class="v"><span runat="server" id="lblDepositAmount">0 USD</span></span></div>
                <div class="cd-field"><span class="k">Deposit fee</span><span class="v"><span runat="server" id="lblFee">0 USD</span></span></div>
            </div>

            <div class="hist-modal-actions">
                <asp:Button CssClass="btn btn-line" runat="server" ID="btnRechargeClose" Text="Close" OnClick="btnRechargeClose_Click" CausesValidation="false" />
                <asp:Button CssClass="btn btn-cyan" runat="server" ID="btnDepositConfirm" Text="Confirm top up" OnClick="btnDepositConfirm_Click" OnClientClick="this.disabled=true;" UseSubmitBehavior="false" />
            </div>
        </div>
    </asp:Panel>

    <%-- ===== Reveal-details OTP overlay ===== --%>
    <asp:Panel runat="server" ID="pnlDetailOTP" Visible="false" CssClass="hist-modal">
        <div class="hist-modal-card">
            <h3>Verify it's you</h3>
            <p class="hist-modal-text">Enter the 6-digit code we sent to your email to reveal your card number, CVV, and expiry.</p>
            <div class="hist-banner err" runat="server" id="divfaileddetail" visible="false">
                <asp:Label runat="server" ID="lblErrorDetail" Text="Error message" />
                <button type="button" class="btn btn-line btn-sm" runat="server" id="btnfaileddetail" onserverclick="btnfaileddetail_ServerClick">&times;</button>
            </div>
            <div class="cd-otp-inputs">
                <input type="text" runat="server" id="icode1" inputmode="numeric" autocomplete="one-time-code" maxlength="1" value="" />
                <input type="text" runat="server" id="icode2" inputmode="numeric" maxlength="1" value="" />
                <input type="text" runat="server" id="icode3" inputmode="numeric" maxlength="1" value="" />
                <input type="text" runat="server" id="icode4" inputmode="numeric" maxlength="1" value="" />
                <input type="text" runat="server" id="icode5" inputmode="numeric" maxlength="1" value="" />
                <input type="text" runat="server" id="icode6" inputmode="numeric" maxlength="1" value="" />
            </div>
            <div class="hist-modal-actions">
                <asp:Button CssClass="btn btn-line" runat="server" ID="btnDetailClose" Text="Close" OnClick="btnDetailClose_Click" CausesValidation="false" />
                <asp:Button CssClass="btn btn-cyan" runat="server" ID="btnDetailX" Text="Reveal" OnClick="btnDetailX_Click" OnClientClick="this.disabled=true;" UseSubmitBehavior="false" />
            </div>
        </div>
    </asp:Panel>

    <%-- ===== Generic alert overlay ===== --%>
    <asp:Panel runat="server" ID="pnlAlert" Visible="false" CssClass="hist-modal">
        <div class="hist-modal-card">
            <h3>Notice</h3>
            <p class="hist-modal-text"><label runat="server" id="Label1"></label></p>
            <div class="hist-modal-actions">
                <asp:Button CssClass="btn btn-line" runat="server" ID="btnAlertClose" Text="Close" OnClick="btnAlertClose_Click" CausesValidation="false" />
            </div>
        </div>
    </asp:Panel>
</asp:Content>

<asp:Content ID="Content4" ContentPlaceHolderID="ScriptContent" runat="server">
    <script type="text/javascript">
        // Copy a hidden field's value to the clipboard (plain JS; no jQuery on the shell).
        function cdCopy(id) {
            var el = document.getElementById(id);
            if (el && navigator.clipboard) { navigator.clipboard.writeText(el.value).then(function () { }).catch(function () { }); }
        }
        // OTP auto-advance / backspace / paste across the six reveal-code inputs (otplogin pattern).
        (function () {
            var ids = ['<%= icode1.ClientID %>', '<%= icode2.ClientID %>', '<%= icode3.ClientID %>', '<%= icode4.ClientID %>', '<%= icode5.ClientID %>', '<%= icode6.ClientID %>'];
            var inputs = ids.map(function (id) { return document.getElementById(id); }).filter(Boolean);
            if (!inputs.length) return;
            function focusAt(i) { if (inputs[i]) inputs[i].focus(); }
            inputs.forEach(function (input, idx) {
                input.addEventListener('input', function () {
                    input.value = input.value.replace(/\D/g, '').slice(-1);
                    input.classList.toggle('filled', !!input.value);
                    if (input.value && idx < inputs.length - 1) focusAt(idx + 1);
                });
                input.addEventListener('keydown', function (e) {
                    if (e.key === 'Backspace' && !input.value && idx > 0) focusAt(idx - 1);
                });
                input.addEventListener('paste', function (e) {
                    e.preventDefault();
                    var digits = (e.clipboardData || window.clipboardData).getData('text').replace(/\D/g, '').slice(0, 6);
                    if (!digits) return;
                    digits.split('').forEach(function (d, k) { if (inputs[k]) { inputs[k].value = d; inputs[k].classList.add('filled'); } });
                    focusAt(Math.min(digits.length, inputs.length - 1));
                });
            });
            focusAt(0);
        })();
        // 3D tilt + idle float for the card (reduced-motion safe) — mirrors My Cards.
        (function () {
            if (window.matchMedia && window.matchMedia('(prefers-reduced-motion: reduce)').matches) return;
            [].slice.call(document.querySelectorAll('.card-3d')).forEach(function (card) {
                var wrap = card.parentElement, face = card.querySelector('.qcard');
                var raf = null, tx = 0, ty = 0, cx = 0, cy = 0;
                function apply() {
                    cx += (tx - cx) * 0.14; cy += (ty - cy) * 0.14;
                    card.style.transform = 'rotateY(' + cx + 'deg) rotateX(' + (-cy) + 'deg)';
                    if (face) { face.style.setProperty('--mx', (cx * 2.2) + '%'); face.style.setProperty('--my', (cy * 2.2) + '%'); }
                    if (Math.abs(tx - cx) > 0.05 || Math.abs(ty - cy) > 0.05) raf = requestAnimationFrame(apply); else raf = null;
                }
                function queue() { if (!raf) raf = requestAnimationFrame(apply); }
                wrap.addEventListener('mousemove', function (e) {
                    var r = wrap.getBoundingClientRect();
                    tx = ((e.clientX - r.left) / r.width - 0.5) * 22;
                    ty = ((e.clientY - r.top) / r.height - 0.5) * 22;
                    card.classList.remove('qcard-float'); queue();
                });
                wrap.addEventListener('mouseleave', function () { tx = 0; ty = 0; queue(); setTimeout(function () { card.classList.add('qcard-float'); }, 400); });
                card.classList.add('qcard-float');
            });
        })();
    </script>
</asp:Content>
