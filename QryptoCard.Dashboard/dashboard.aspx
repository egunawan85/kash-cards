<%@ Page Title="Dashboard" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="dashboard.aspx.cs" Inherits="QryptoCard.Dashboard.dashboard" %>

<%@ MasterType VirtualPath="~/Site.Master" %>
<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" runat="server">
    <asp:HiddenField runat="server" ID="hfReferralCode" />
    <asp:HiddenField runat="server" ID="hfReferralLink" />

    <!--begin::Page header-->
    <div class="dash-top">
        <div>
            <h1>Welcome back</h1>
            <div class="sub">Here's your account at a glance.</div>
        </div>
        <div class="dash-top-actions">
            <a class="btn btn-cyan" href='<%= ResolveUrl("~/card/cardlist") %>'>Buy a card
                <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M5 12h14M13 6l6 6-6 6" /></svg>
            </a>
        </div>
    </div>
    <!--end::Page header-->

    <div class="dash-grid">
        <!--begin::Balance — live wallet (S-F). Balance comes from the server (getBalance);
            no figures are computed or fabricated client-side. -->
        <section class="panel balance">
            <div class="lab">Available balance</div>
            <div class="amt"><span runat="server" id="lblBalance">&mdash;</span><span class="cur">USDT</span></div>
            <div class="balance-actions">
                <a class="btn btn-cyan" href='<%= ResolveUrl("~/txdeposit") %>'>Top up</a>
                <a class="btn btn-line" href='<%= ResolveUrl("~/tx/cardhistory") %>'>Statements</a>
            </div>
        </section>
        <!--end::Balance-->

        <!--begin::Card — live card-at-a-glance (S-F). Card list is server-returned (getCardList);
            only the masked last-4 and expiry are shown here, never the full PAN or CVV. -->
        <section class="panel card-panel">
            <div class="panel-h"><h3>Your card</h3><a href='<%= ResolveUrl("~/card/mycardlist") %>'>Manage</a></div>
            <div runat="server" id="viewCard" visible="false">
                <div class="card3d-wrap">
                    <div class="card-3d">
                        <div class="qcard">
                            <div class="qcard-inner">
                                <div class="qcard-top">
                                    <div class="qcard-brand">K<b>ash</b></div>
                                    <span class="qcard-tag"><span runat="server" id="lblCardNetwork">Virtual</span></span>
                                </div>
                                <div><div class="qcard-chip"></div></div>
                                <div class="qcard-num"><span runat="server" id="lblCardLast4">&#8226;&#8226;&#8226;&#8226;</span></div>
                                <div class="qcard-bottom">
                                    <div><div class="lab">Expires</div><div class="val"><span runat="server" id="lblCardExp">&mdash;</span></div></div>
                                    <div class="qcard-logo">K</div>
                                </div>
                            </div>
                        </div>
                    </div>
                </div>
                <div class="card-controls" style="grid-template-columns: 1fr; margin-top: 18px;">
                    <a class="ctrl-btn" runat="server" id="lnkCardDetails" href="#">
                        <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.7"><circle cx="12" cy="12" r="3" /><path d="M2 12s3.6-7 10-7 10 7 10 7-3.6 7-10 7-10-7-10-7z" /></svg> Details
                    </a>
                </div>
            </div>
            <div runat="server" id="viewNoCard" class="card-empty" style="color: var(--ink-3); font-size: .95rem; padding: 18px 0;">
                Your card details will appear here once your card is active.
            </div>
        </section>
        <!--end::Card-->

        <!--begin::Stats (live)-->
        <section class="stat-row">
            <div class="stat">
                <div class="ic"><svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.7"><path d="M12 1v22M17 5H9.5a3.5 3.5 0 000 7h5a3.5 3.5 0 010 7H6" /></svg></div>
                <div class="n"><span runat="server" id="lblCommissionRate">-</span></div>
                <div class="l">Commission Rate</div>
            </div>
            <div class="stat">
                <div class="ic"><svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.7"><circle cx="12" cy="12" r="9" /><path d="M12 8v8M8 12h8" /></svg></div>
                <div class="n"><span runat="server" id="lblTotalCommission">-</span></div>
                <div class="l">Total Commission</div>
            </div>
            <div class="stat">
                <div class="ic"><svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.7"><rect x="2" y="5" width="20" height="14" rx="3" /><path d="M2 10h20" /></svg></div>
                <div class="n"><span runat="server" id="lblTotalCards">-</span></div>
                <div class="l">Total Cards</div>
            </div>
        </section>
        <!--end::Stats-->

        <!--begin::Wallet ledger (S-F, live) — paginated via query string (?lpage=N) so a page
            change is a normal GET that re-binds the whole dashboard, never a postback that would
            blank the other panels. All figures are server-returned (getLedger); none computed here. -->
        <section class="panel txns" id="wallet-ledger">
            <div class="panel-h"><h3>Wallet transactions</h3><a href='<%= ResolveUrl("~/tx/cardhistory") %>'>View all</a></div>
            <div runat="server" id="divNoLedger" class="txns-empty" style="color: var(--ink-3); font-size: .95rem; padding: 18px 0;">
                No wallet transactions yet.
            </div>
            <asp:Repeater ID="rptLedger" runat="server" Visible="false">
                <ItemTemplate>
                    <div class="txn">
                        <div class="ic">
                            <span runat="server" visible='<%# IsLedgerCredit(Container.DataItem) %>'><svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.7"><path d="M12 5v14M5 12l7 7 7-7" /></svg></span>
                            <span runat="server" visible='<%# !IsLedgerCredit(Container.DataItem) %>'><svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.7"><path d="M12 19V5M5 12l7-7 7 7" /></svg></span>
                        </div>
                        <div class="meta">
                            <div class="merchant"><%# LedgerType(Container.DataItem) %></div>
                            <div class="when"><%# LedgerWhen(Container.DataItem) %></div>
                        </div>
                        <div class='<%# LedgerAmtClass(Container.DataItem) %>'><%# LedgerAmt(Container.DataItem) %></div>
                    </div>
                </ItemTemplate>
            </asp:Repeater>
            <div class="ledger-pager" runat="server" id="ledgerPager" visible="false" style="display:flex; gap:12px; align-items:center; margin-top:12px;"></div>
        </section>
        <!--end::Wallet ledger-->

        <!--begin::Referral (live)-->
        <section class="panel referral">
            <div class="panel-h"><h3>Refer friends, earn rewards</h3></div>
            <p style="color: var(--ink-3); font-size: .95rem; margin-bottom: 6px;">Share your link &mdash; you both earn when they top up their first card.</p>
            <div class="ref-grid">
                <div class="ref-field">
                    <label>Your referral code</label>
                    <div class="copy-row">
                        <asp:TextBox runat="server" ID="txtReferralCode" ReadOnly="true" />
                        <button type="button" class="copy-btn" runat="server" id="btnCopyReferralCode" onclick="copyReferralCode();" aria-label="Copy code">
                            <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8"><rect x="9" y="9" width="11" height="11" rx="2" /><path d="M5 15V5a2 2 0 012-2h10" /></svg>
                        </button>
                    </div>
                </div>
                <div class="ref-field">
                    <label>Your referral link</label>
                    <div class="copy-row">
                        <asp:TextBox runat="server" ID="txtReferralLink" ReadOnly="true" />
                        <button type="button" class="copy-btn" runat="server" id="btnCopyReferralLink" onclick="copyReferralLink();" aria-label="Copy link">
                            <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8"><rect x="9" y="9" width="11" height="11" rx="2" /><path d="M5 15V5a2 2 0 012-2h10" /></svg>
                        </button>
                    </div>
                </div>
            </div>
        </section>
        <!--end::Referral-->

        <!--begin::Referral history (live)-->
        <section class="panel">
            <div class="panel-h"><h3>Referral history</h3></div>
            <div runat="server" id="divnoreferral" style="color: var(--ink-3); font-size: .95rem; padding: 14px 0;">
                <asp:Label runat="server" ID="Label2" Text="No user joined at the moment." />
            </div>
            <asp:GridView CssClass="dash-table" ID="gvReferralList" runat="server" ShowHeader="false" AutoGenerateColumns="false" DataKeyNames="UserID" AllowPaging="True" PageSize="50" AllowCustomPaging="False" GridLines="None">
                <PagerStyle HorizontalAlign="Center" />
                <Columns>
                    <asp:TemplateField ItemStyle-VerticalAlign="Top" ItemStyle-HorizontalAlign="Left">
                        <ItemTemplate>
                            <asp:HiddenField runat="server" ID="hfID" Value='<%# Eval("UserID") %>' />
                            <div class="ref-row">
                                <span class="ref-name"><%# Eval("FirstName") %> <%# Eval("LastName") %></span>
                                <span class="ref-when"><%# Eval("DateJoin") %></span>
                            </div>
                        </ItemTemplate>
                    </asp:TemplateField>
                </Columns>
            </asp:GridView>
        </section>
        <!--end::Referral history-->

        <!--begin::Commission history (live placeholder)-->
        <section class="panel">
            <div class="panel-h"><h3>Commission history</h3></div>
            <div runat="server" id="divnocommission" style="color: var(--ink-3); font-size: .95rem; padding: 14px 0;">
                <asp:Label runat="server" ID="Label1" Text="No commission at the moment." />
            </div>
        </section>
        <!--end::Commission history-->
    </div>
</asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderID="DrawerContent" runat="server">
</asp:Content>
<asp:Content ID="Content3" ContentPlaceHolderID="ModalContent" runat="server">
</asp:Content>
<asp:Content ID="Content4" ContentPlaceHolderID="ScriptContent" runat="server">
    <script type="text/javascript">
        // Client-only copy (no postback). The visible field is read-only; the canonical
        // value is mirrored into a hidden field by the code-behind.
        function copyReferralCode() {
            var copyText = document.getElementById("<%= hfReferralCode.ClientID %>");
            navigator.clipboard.writeText(copyText.value).then(function () { }).catch(function () { });
        }

        function copyReferralLink() {
            var copyText = document.getElementById("<%= hfReferralLink.ClientID %>");
            navigator.clipboard.writeText(copyText.value).then(function () { }).catch(function () { });
        }
    </script>
    <script type="text/javascript">
        // 3D tilt + idle float for the dashboard virtual card (.card-3d in .card3d-wrap). Reduced-motion safe.
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
