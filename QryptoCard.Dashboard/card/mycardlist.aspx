<%@ Page Title="My Cards" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="mycardlist.aspx.cs" Inherits="QryptoCard.Dashboard.card.mycardlist" %>

<%@ MasterType VirtualPath="~/Site.Master" %>

<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" runat="server">
    <asp:HiddenField runat="server" ID="hfReferralCode" />
    <asp:HiddenField runat="server" ID="hfReferralLink" />

    <style>
        .mycards-grid { display: grid; grid-template-columns: repeat(auto-fill, minmax(300px, 1fr)); gap: 22px; margin-top: 20px; }
        .cards-alert { border-radius: 12px; padding: .85em 1.1em; margin-top: 16px; font-size: .9rem; font-weight: 500; color: #ff8585; background: rgba(255, 70, 70, .07); border: 1px solid rgba(255, 90, 90, .32); }
    </style>

    <!--begin::Page header-->
    <div class="dash-top">
        <div>
            <h1>My Cards</h1>
            <div class="sub">Your active cards.</div>
        </div>
        <div class="dash-top-actions">
            <a class="btn btn-cyan" href='<%= ResolveUrl("~/card/cardlist") %>'>Buy a card
                <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M5 12h14M13 6l6 6-6 6" /></svg>
            </a>
        </div>
    </div>
    <!--end::Page header-->

    <asp:Panel runat="server" ID="pnlAlert" CssClass="cards-alert" Visible="false">
        <asp:Label runat="server" ID="lblAlert" />
    </asp:Panel>

    <asp:HiddenField runat="server" ID="hfID" />
    <asp:Panel runat="server" ID="pnlMsg" Visible="false" CssClass="hist-banner err">
        <span runat="server" id="lblMsg"></span>
    </asp:Panel>

    <div class="cards-sum">
        <div class="cards-sum-it"><div class="n"><asp:Literal runat="server" ID="litTotalCards" Text="0" /></div><div class="l">Total cards</div></div>
        <div class="cards-sum-sep"></div>
        <div class="cards-sum-it"><div class="n"><asp:Literal runat="server" ID="litActiveCards" Text="0" /></div><div class="l">Active</div></div>
        <div class="cards-sum-sep"></div>
        <div class="cards-sum-it"><div class="n"><asp:Literal runat="server" ID="litAvailBal" Text="&#8212;" /></div><div class="l"><asp:Literal runat="server" ID="litAvailBalLab" Text="Available balance" /></div></div>
    </div>

    <asp:Literal runat="server" ID="litInProgress" />

    <div class="mycards-grid">
        <asp:Repeater ID="rptCard" runat="server">
            <ItemTemplate>
                <div class="card3d-wrap" onclick="window.location.href='<%# Eval("DetailURL") %>';" style="cursor: pointer;">
                    <div class="card-3d">
                        <div class="qcard">
                            <div class="qcard-inner">
                                <div class="qcard-top">
                                    <div class="qcard-brand">K<b>ash</b></div>
                                    <%# CardBrandMark((string)Eval("Organization")) %>
                                </div>
                                <div><div class="qcard-chip"></div></div>
                                <div class="qcard-num"><%# Eval("CardNumber") %></div>
                                <div class="qcard-bottom">
                                    <div><div class="lab">Card holder</div><div class="val"><%# Eval("FirstName") %> <%# Eval("LastName") %></div></div>
                                    <div><div class="lab">Balance</div><div class="val"><%# Eval("Param5") %></div></div>
                                </div>
                            </div>
                        </div>
                    </div>
                </div>
            </ItemTemplate>
        </asp:Repeater>
        <a class="card-add" href='<%= ResolveUrl("~/card/cardlist") %>'>
            <span class="card-add-plus"><svg width="22" height="22" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M12 5v14M5 12h14" /></svg></span>
            <b>Add a new card</b>
            <span>Choose a BIN with the right merchant fit</span>
        </a>
    </div>

    <!--begin::Card orders (all states) — purchase orders + pay-link / Cancel, relocated from the
        Transactions page so that page can be a pure spend feed. -->
    <div class="dash-top" style="margin-top: 30px;">
        <div>
            <h2 style="font-size: 1.25rem; letter-spacing: -.02em;">Card orders</h2>
            <div class="sub">Your card purchases and their status.</div>
        </div>
    </div>
    <div>
        <div class="panel">
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
        // 3D tilt + idle float for the virtual cards (.card-3d in .card3d-wrap). Reduced-motion safe.
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
