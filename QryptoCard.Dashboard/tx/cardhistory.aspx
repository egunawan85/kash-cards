<%@ Page Title="Transactions" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="cardhistory.aspx.cs" Inherits="QryptoCard.Dashboard.tx.cardhistory" %>

<%@ MasterType VirtualPath="~/Site.Master" %>

<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" runat="server">
    <asp:HiddenField runat="server" ID="hfID" />
    <div class="dash-top">
        <div>
            <h1>Transactions</h1>
            <div class="sub">All your card activity.</div>
        </div>
    </div>

    <asp:Panel runat="server" ID="pnlMsg" Visible="false" CssClass="hist-banner err">
        <span runat="server" id="lblalert"></span>
    </asp:Panel>

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

    <!--begin::Card orders (purchase orders + pay-link / cancel). TRANSITIONAL — relocating to
        My Cards in the follow-up change; kept here so order management is never lost between PRs. -->
    <div class="dash-top" style="margin-top: 30px;">
        <div>
            <h2 style="font-size: 1.25rem; letter-spacing: -.02em;">Card orders</h2>
            <div class="sub">Your card purchases and their status.</div>
        </div>
    </div>
    <div>
        <div class="panel">
            <div>
                <div class="table-responsive">
                    <asp:GridView CssClass="data-table" ID="gvListItem" HeaderStyle-CssClass="header-table" runat="server" AutoGenerateColumns="false" DataKeyNames="ID" AllowPaging="True" PageSize="50" AllowCustomPaging="False" OnPageIndexChanging="gvListItem_PageIndexChanging" OnRowCreated="gvListItem_RowCreated">
                        <PagerStyle HorizontalAlign="Center" CssClass="bs4-aspnet-pager" />
                        <Columns>
                            <asp:TemplateField HeaderText="No." ItemStyle-HorizontalAlign="Center" ItemStyle-VerticalAlign="Middle" HeaderStyle-Width="50px">
                                <ItemTemplate>
                                    <asp:HiddenField runat="server" ID="hfID" Value='<%# Eval("ID") %>' />
                                    <asp:HiddenField runat="server" ID="hfURL" Value='<%# Eval("DetailURL") %>' />
                                    <%# Container.DataItemIndex + 1 %>
                                </ItemTemplate>
                            </asp:TemplateField>
                            <asp:BoundField DataField="CardName" HeaderText="Card Bin" ItemStyle-HorizontalAlign="Center" ItemStyle-VerticalAlign="Middle" HeaderStyle-Width="50px" />
                            <asp:BoundField DataField="Organization" HeaderText="Provider" ItemStyle-HorizontalAlign="Center" ItemStyle-VerticalAlign="Middle" HeaderStyle-Width="50px" />
                            <asp:BoundField DataField="FirstName" HeaderText="Cardholder" ItemStyle-HorizontalAlign="Center" ItemStyle-VerticalAlign="Middle" HeaderStyle-Width="50px" />
                            <asp:BoundField DataField="Currency" HeaderText="Currency" ItemStyle-HorizontalAlign="Center" ItemStyle-VerticalAlign="Middle" HeaderStyle-Width="50px" />
                            <asp:BoundField DataField="Price" HeaderText="Price" ItemStyle-HorizontalAlign="Center" ItemStyle-VerticalAlign="Middle" HeaderStyle-Width="50px" />
                            <asp:BoundField DataField="InitialDeposit" HeaderText="Initial Deposit" ItemStyle-HorizontalAlign="Center" ItemStyle-VerticalAlign="Middle" HeaderStyle-Width="50px" />
                            <asp:BoundField DataField="Total" HeaderText="Total" ItemStyle-HorizontalAlign="Center" ItemStyle-VerticalAlign="Middle" HeaderStyle-Width="50px" />
                            <asp:TemplateField HeaderText="Status" ItemStyle-HorizontalAlign="Center" ItemStyle-VerticalAlign="Middle" HeaderStyle-Width="100px">
                                <ItemTemplate>
                                    <center>
                                        <span class="badge badge-light-warning" runat="server" visible='<%# IsStatusCreated((string)Eval("Status")) %>'>Pending Payment</span>
                                        <span class="badge badge-light-success" runat="server" visible='<%# IsStatusCompleted((string)Eval("Status")) %>'>Completed!</span>
                                        <span class="badge badge-light-info" runat="server" visible='<%# IsStatusInProgress((string)Eval("Status")) %>'>In Progress</span>
                                        <span class="badge badge-light-info" runat="server" visible='<%# IsStatusInProgress((string)Eval("Status")) %>'>Paid</span>
                                        <span class="badge badge-light-danger" runat="server" visible='<%# IsStatusCancelled((string)Eval("Status")) %>'>Cancelled</span>
                                        <span class="badge badge-light-danger" runat="server" visible='<%# IsStatusExpired((string)Eval("Status")) %>'>Expired</span>
                                    </center>
                                </ItemTemplate>
                            </asp:TemplateField>
                            <asp:TemplateField HeaderText="Actions" ItemStyle-HorizontalAlign="Center" ItemStyle-VerticalAlign="Middle" HeaderStyle-Width="170px">
                                <ItemTemplate>
                                    <div class="row-actions" runat="server" visible='<%# IsStatusCreatedBadge((string)Eval("Status")) %>'>
                                        <a class="btn btn-line btn-sm" href='<%# Eval("DetailURL") %>' target="_blank" rel="noopener">Payment link</a>
                                        <button runat="server" id="btnCancel" onserverclick="btnCancel_ServerClick" class="btn btn-danger btn-sm">Cancel</button>
                                    </div>
                                </ItemTemplate>
                            </asp:TemplateField>
                        </Columns>
                    </asp:GridView>
                </div>

                <div class="data-empty" runat="server" id="divnorow">
                    <center>
                        <asp:Label runat="server" ID="lblNoRow" Text="No card orders at the moment." /></center>
                </div>
            </div>
        </div>
    </div>
    <!--end::Card orders-->
</asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderID="DrawerContent" runat="server">
</asp:Content>
<asp:Content ID="Content3" ContentPlaceHolderID="ModalContent" runat="server">
    <%-- Cancel confirmation. Server-rendered overlay (Visible toggled in code-behind). --%>
    <asp:Panel runat="server" ID="pnlCancelConfirm" Visible="false" CssClass="hist-modal">
        <div class="hist-modal-card">
            <h3>Cancel card transaction</h3>
            <p class="hist-modal-text">Are you sure you want to cancel this card transaction? This can't be undone.</p>
            <div class="hist-modal-actions">
                <asp:Button CssClass="btn btn-line" runat="server" ID="btnCloseConfirm" Text="Keep it" OnClick="btnCloseConfirm_Click" CausesValidation="false" />
                <asp:Button CssClass="btn btn-danger" runat="server" ID="btnCancelExec" Text="Yes, cancel" OnClick="btnCancelExec_Click" />
            </div>
        </div>
    </asp:Panel>
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
