<%@ Page Title="Buy a card" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="cardlist.aspx.cs" Inherits="QryptoCard.Dashboard.card.cardlist" %>

<%@ MasterType VirtualPath="~/Site.Master" %>

<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" runat="server">
    <asp:HiddenField runat="server" ID="hfReferralCode" />
    <asp:HiddenField runat="server" ID="hfReferralLink" />

    <style>
        .cards-intro { color: var(--ink-3); font-size: .95rem; line-height: 1.55; }
        .cards-grid { display: grid; grid-template-columns: repeat(auto-fill, minmax(300px, 1fr)); gap: 18px; margin-top: 20px; }
        .card-tile { cursor: pointer; border-radius: 18px; padding: 22px; color: #eaf6f8; background: linear-gradient(135deg, #0c121a, #16222e 55%, #0b2b32); border: 1px solid var(--line-2); transition: border-color .2s, transform .2s; }
        .card-tile:hover { border-color: rgba(0, 230, 255, .35); transform: translateY(-2px); }
        .card-tile-top { display: flex; justify-content: space-between; align-items: flex-start; min-height: 28px; }
        .card-tile-price { font-family: var(--font-display); font-weight: 700; font-size: 1.5rem; }
        .card-tile-bin { font-family: var(--font-mono); letter-spacing: .08em; font-size: 1rem; color: var(--cyan-bright); margin: 20px 0; }
        .card-tile-meta { display: flex; justify-content: space-between; align-items: flex-end; font-size: .82rem; color: var(--ink-3); }
        .card-tile-logos { text-align: right; }
        .card-tile-logos img { height: 26px; margin-left: 6px; vertical-align: middle; }
    </style>

    <!--begin::Page header-->
    <div class="dash-top">
        <div>
            <h1>Buy a card</h1>
            <div class="sub">Choose a card — each accepts a different set of merchants.</div>
        </div>
    </div>
    <!--end::Page header-->

    <section class="panel">
        <div class="panel-h"><h3>Notice</h3></div>
        <p class="cards-intro">
            The card is applicable to most online consumption scenarios (ChatGPT, CloudFlare, TikTok, MidJourney,
            WeChat/AliPay, Telegram, Facebook, GoDaddy&hellip;). The categories below are distinguished by where each
            card performs best.
        </p>

        <div class="cards-grid">
            <asp:Repeater ID="rptCard" runat="server">
                <ItemTemplate>
                    <div class="card-tile" onclick="window.location.href='<%# Eval("DetailURL") %>';">
                        <div class="card-tile-top">
                            <span class="card-tile-price"><%# Eval("CardPrice") %></span>
                        </div>
                        <div class="card-tile-bin"><%# Eval("BankCardBin") %></div>
                        <div class="card-tile-meta">
                            <div>
                                <div>Deposit Fee <%# Eval("RechargeFeeRate") %></div>
                                <div>First time: <%# Eval("NeedDeposit") %></div>
                            </div>
                            <div class="card-tile-logos">
                                <%# Eval("TypeStr") %>
                                <%# Eval("Status") %>
                                <img src='<%# Eval("LogoURL") %>' alt="scheme" />
                            </div>
                        </div>
                    </div>
                </ItemTemplate>
            </asp:Repeater>
        </div>
    </section>
</asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderID="DrawerContent" runat="server">
</asp:Content>
<asp:Content ID="Content3" ContentPlaceHolderID="ModalContent" runat="server">
</asp:Content>
<asp:Content ID="Content4" ContentPlaceHolderID="ScriptContent" runat="server">
</asp:Content>
