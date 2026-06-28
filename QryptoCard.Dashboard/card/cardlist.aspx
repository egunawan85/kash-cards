<%@ Page Title="Buy a card" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="cardlist.aspx.cs" Inherits="QryptoCard.Dashboard.card.cardlist" %>

<%@ MasterType VirtualPath="~/Site.Master" %>

<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" runat="server">
    <asp:HiddenField runat="server" ID="hfReferralCode" />
    <asp:HiddenField runat="server" ID="hfReferralLink" />

    <style>
        .cards-intro { color: var(--ink-3); font-size: .95rem; line-height: 1.55; }
        .cards-grid { display: grid; grid-template-columns: repeat(auto-fill, minmax(300px, 1fr)); gap: 18px; margin-top: 20px; }
        .card-tile { cursor: pointer; border-radius: 18px; padding: 22px; color: #eaf6f8; background: linear-gradient(135deg, #0c121a, #16222e 55%, #0b2b32); background-size: cover; background-position: center; border: 1px solid var(--line-2); transition: border-color .2s, transform .2s; text-shadow: 0 1px 3px rgba(0, 0, 0, .55); }
        .card-tile:hover { border-color: rgba(0, 230, 255, .35); transform: translateY(-2px); }
        .card-tile-top { display: flex; justify-content: space-between; align-items: flex-start; min-height: 28px; }
        .card-tile-price { font-family: var(--font-display); font-weight: 700; font-size: 1.5rem; }
        .card-tile-bin { font-family: var(--font-mono); letter-spacing: .08em; font-size: 1rem; color: var(--cyan-bright); margin: 20px 0; }
        .card-tile-meta { display: flex; justify-content: space-between; align-items: flex-end; font-size: .82rem; color: var(--ink-3); }
        .card-tile-logos { text-align: right; }
        .card-tile-logos img { height: 26px; margin-left: 6px; vertical-align: middle; }
        .cards-alert { border-radius: 12px; padding: .85em 1.1em; margin-top: 16px; font-size: .9rem; font-weight: 500; color: #ff8585; background: rgba(255, 70, 70, .07); border: 1px solid rgba(255, 90, 90, .32); }
    </style>

    <!--begin::Page header-->
    <div class="dash-top">
        <div>
            <h1>Buy a card</h1>
            <div class="sub">Choose a card — each accepts a different set of merchants.</div>
        </div>
    </div>
    <!--end::Page header-->

    <asp:Panel runat="server" ID="pnlAlert" CssClass="cards-alert" Visible="false">
        <asp:Label runat="server" ID="lblAlert" />
    </asp:Panel>

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
                    <div class="card3d-wrap" onclick="window.location.href='<%# Eval("DetailURL") %>';" style="cursor: pointer;">
                        <div class="card-3d">
                            <div class="qcard">
                                <div class="qcard-inner">
                                    <div class="qcard-top">
                                        <div class="qcard-brand">K<b>ash</b></div>
                                        <%# CardBrandMark((string)Eval("Organization")) %>
                                    </div>
                                    <div><div class="qcard-chip"></div></div>
                                    <div class="qcard-num"><%#: Eval("BankCardBin") %></div>
                                    <div class="qcard-bottom">
                                        <div><div class="lab">Price</div><div class="val"><%# Eval("CardPrice") %></div></div>
                                        <div><div class="lab">Deposit Fee</div><div class="val"><%# Eval("RechargeFeeRate") %></div></div>
                                    </div>
                                </div>
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
    <script type="text/javascript">
        // 3D tilt + idle float for the buy-a-card tiles (.card-3d in .card3d-wrap). Reduced-motion safe.
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
