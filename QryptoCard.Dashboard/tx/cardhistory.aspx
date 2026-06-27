<%@ Page Title="Transactions" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="cardhistory.aspx.cs" Inherits="QryptoCard.Dashboard.tx.cardhistory" %>

<%@ MasterType VirtualPath="~/Site.Master" %>

<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" runat="server">
    <div class="dash-top">
        <div>
            <h1>Transactions</h1>
            <div class="sub">All your card activity.</div>
        </div>
    </div>

    <!--begin::Unified money-activity feed (S-F) — card spends + top-ups across the user's cards,
        server-returned via trxCardList / depositCardList. Masked last-4 only; no PAN/CVV. -->
    <div class="tx-toolbar">
        <div class="tx-chips" id="tx-type" role="tablist" aria-label="Filter by type">
            <button type="button" class="tx-chip is-active" data-filter="all">All</button>
            <button type="button" class="tx-chip" data-filter="spend">Spends</button>
            <button type="button" class="tx-chip" data-filter="topup">Top-ups</button>
            <button type="button" class="tx-chip" data-filter="refund">Refunds</button>
            <button type="button" class="tx-chip" data-filter="fee">Fees</button>
            <button type="button" class="tx-chip" data-filter="other">Other</button>
        </div>
        <div class="tx-search">
            <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.7" aria-hidden="true"><circle cx="11" cy="11" r="7" /><path d="M21 21l-4.3-4.3" /></svg>
            <input type="search" id="tx-search-input" placeholder="Search merchants&hellip;" aria-label="Search by merchant" autocomplete="off" />
        </div>
    </div>
    <div class="tx-cardfilter" runat="server" id="divCardFilter"></div>

    <section class="panel tx-panel">
        <asp:Repeater runat="server" ID="rptGroups" Visible="false">
            <ItemTemplate>
                <div class="tx-group" data-group>
                    <div class="tx-group-h"><%# Server.HtmlEncode((string)Eval("Label")) %></div>
                    <asp:Repeater runat="server" DataSource='<%# Eval("Rows") %>'>
                        <ItemTemplate>
                            <div class='<%# FeedRowClass(Container.DataItem) %>' data-type='<%# FeedCat(Container.DataItem) %>' data-card='<%# FeedCardKey(Container.DataItem) %>' data-merchant='<%# FeedSearchKey(Container.DataItem) %>'>
                                <div class='<%# FeedIcClass(Container.DataItem) %>'><%# FeedIcon(Container.DataItem) %></div>
                                <div class="meta">
                                    <div class="merchant"><%# FeedMerchant(Container.DataItem) %><%# FeedTag(Container.DataItem) %></div>
                                    <div class="when"><%# FeedWhen(Container.DataItem) %> <%# FeedCardChip(Container.DataItem) %></div>
                                </div>
                                <div class='<%# FeedAmtClass(Container.DataItem) %>'><%# FeedAmt(Container.DataItem) %></div>
                            </div>
                        </ItemTemplate>
                    </asp:Repeater>
                </div>
            </ItemTemplate>
        </asp:Repeater>
        <div class="tx-empty" runat="server" id="divNoFeed" visible="false">No transactions yet.</div>
    </section>
    <!--end::Unified feed-->
</asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderID="DrawerContent" runat="server">
</asp:Content>
<asp:Content ID="Content3" ContentPlaceHolderID="ModalContent" runat="server">
</asp:Content>
<asp:Content ID="Content4" ContentPlaceHolderID="ScriptContent" runat="server">
    <script type="text/javascript">
        // Client-side filter (type chips + per-card chips + merchant search) over the rendered
        // feed rows. No data round-trip — all rows are already on the page.
        (function () {
            var typeChips = [].slice.call(document.querySelectorAll('#tx-type .tx-chip'));
            var cardWrap = document.querySelector('.tx-cardfilter');
            var cardChips = cardWrap ? [].slice.call(cardWrap.querySelectorAll('.tx-chip')) : [];
            var search = document.getElementById('tx-search-input');
            var rows = [].slice.call(document.querySelectorAll('.tx-group .txn'));
            var groups = [].slice.call(document.querySelectorAll('.tx-group[data-group]'));
            var fType = 'all', fCard = 'all';
            function apply() {
                var q = ((search && search.value) || '').trim().toLowerCase();
                rows.forEach(function (row) {
                    var tOK = fType === 'all' || row.getAttribute('data-type') === fType;
                    var cOK = fCard === 'all' || row.getAttribute('data-card') === fCard;
                    var sOK = !q || (row.getAttribute('data-merchant') || '').indexOf(q) !== -1;
                    row.hidden = !(tOK && cOK && sOK);
                });
                groups.forEach(function (g) { g.hidden = !g.querySelector('.txn:not([hidden])'); });
            }
            function wire(chips, set) {
                chips.forEach(function (c) {
                    c.addEventListener('click', function () {
                        chips.forEach(function (x) { x.classList.remove('is-active'); });
                        c.classList.add('is-active'); set(c); apply();
                    });
                });
            }
            wire(typeChips, function (c) { fType = c.getAttribute('data-filter'); });
            wire(cardChips, function (c) { fCard = c.getAttribute('data-card'); });
            if (search) search.addEventListener('input', apply);
        })();
    </script>
</asp:Content>
