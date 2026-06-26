<%@ Page Title="My Cards" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="mycardlist.aspx.cs" Inherits="QryptoCard.Dashboard.card.mycardlist" %>

<%@ MasterType VirtualPath="~/Site.Master" %>

<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" runat="server">
    <asp:HiddenField runat="server" ID="hfReferralCode" />
    <asp:HiddenField runat="server" ID="hfReferralLink" />

    <style>
        .mycards-grid { display: grid; grid-template-columns: repeat(auto-fill, minmax(320px, 1fr)); gap: 18px; margin-top: 20px; }
        .mycard-tile { cursor: pointer; border-radius: 18px; padding: 24px; color: #eaf6f8; aspect-ratio: 1.6 / 1; display: flex; flex-direction: column; justify-content: space-between; background: linear-gradient(135deg, #0c121a, #16222e 55%, #0b2b32); border: 1px solid var(--line-2); transition: border-color .2s, transform .2s; }
        .mycard-tile:hover { border-color: rgba(0, 230, 255, .35); transform: translateY(-2px); }
        .mycard-top { display: flex; justify-content: space-between; align-items: flex-start; }
        .mycard-brand { font-family: var(--font-mono); font-size: .7rem; letter-spacing: .14em; text-transform: uppercase; color: var(--ink-3); }
        .mycard-bal { font-family: var(--font-display); font-weight: 700; font-size: 1.35rem; }
        .mycard-num { font-family: var(--font-mono); letter-spacing: .14em; font-size: 1.15rem; color: var(--cyan-bright); }
        .mycard-foot { display: flex; justify-content: space-between; align-items: flex-end; }
        .mycard-label { font-family: var(--font-mono); font-size: .58rem; letter-spacing: .12em; text-transform: uppercase; color: var(--ink-faint); }
        .mycard-holder { font-weight: 600; font-size: .95rem; margin-top: 2px; }
        .mycard-foot img { height: 28px; }
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

    <div class="mycards-grid">
        <asp:Repeater ID="rptCard" runat="server">
            <ItemTemplate>
                <div class="mycard-tile" onclick="window.location.href='<%# Eval("DetailURL") %>';">
                    <div class="mycard-top">
                        <span class="mycard-brand">Kash Virtual</span>
                        <span class="mycard-bal"><%# Eval("Param5") %></span>
                    </div>
                    <div class="mycard-num"><%# Eval("CardNumber") %></div>
                    <div class="mycard-foot">
                        <div>
                            <div class="mycard-label">Card holder</div>
                            <div class="mycard-holder"><%# Eval("FirstName") %> <%# Eval("LastName") %></div>
                        </div>
                        <img src='<%# Eval("LogoURL") %>' alt="scheme" />
                    </div>
                </div>
            </ItemTemplate>
        </asp:Repeater>
    </div>
</asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderID="DrawerContent" runat="server">
</asp:Content>
<asp:Content ID="Content3" ContentPlaceHolderID="ModalContent" runat="server">
</asp:Content>
<asp:Content ID="Content4" ContentPlaceHolderID="ScriptContent" runat="server">
</asp:Content>
