<%@ Page Title="Add Funds" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="txdeposit.aspx.cs" Inherits="QryptoCard.Dashboard.txdeposit" %>

<%@ MasterType VirtualPath="~/Site.Master" %>
<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" runat="server">
    <asp:HiddenField ID="hfAddress" runat="server" />

    <div class="dash-top">
        <div>
            <h1>Add funds</h1>
            <div class="sub">Top up your wallet by depositing USDT (TRC20).</div>
        </div>
    </div>

    <div class="dep-wrap">
        <!-- Available balance — server-returned (getBalance); never computed here. -->
        <section class="panel balance">
            <div class="dep-bal">
                <span class="lab">Available balance</span>
                <span class="val"><span runat="server" id="lblBalance">&mdash;</span><span class="cur">USDT</span></span>
            </div>
        </section>

        <section class="panel" runat="server" id="viewDeposit">
            <div class="panel-h"><h3>Your deposit address</h3></div>
            <p class="dep-hint">Send only <b>USDT (TRC20)</b> to the address below.</p>

            <!-- Click to copy (reads the hidden field, same as the old page) -->
            <div class="dep-addr" onclick="copyToClipboard();" title="Click to copy">
                <span runat="server" id="lbladdress">addr</span>
                <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.7"><rect x="9" y="9" width="11" height="11" rx="2" /><path d="M5 15H4a2 2 0 01-2-2V4a2 2 0 012-2h9a2 2 0 012 2v1" /></svg>
            </div>

            <div class="dep-qr">
                <asp:Image runat="server" ID="imgQR" alt="Deposit address QR" CssClass="dep-qr-img" Visible="false" />
            </div>

            <div class="dep-note">
                <span>Deposits credit your wallet balance after on-chain confirmation. If you send from an exchange, add the exchange fee on top so the full intended amount arrives.</span>
            </div>
        </section>

        <div runat="server" id="viewNoAddress" visible="false" class="otp-banner fail">
            <span runat="server" id="lblNoAddress">A deposit address is not available yet. Please try again shortly.</span>
        </div>

        <div>
            <a class="btn btn-line" href='<%= ResolveUrl("~/dashboard") %>'>Back to dashboard</a>
        </div>
    </div>
</asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderID="DrawerContent" runat="server">
</asp:Content>
<asp:Content ID="Content3" ContentPlaceHolderID="ModalContent" runat="server">
</asp:Content>
<asp:Content ID="Content4" ContentPlaceHolderID="ScriptContent" runat="server">
    <script type="text/javascript">
        function copyToClipboard() {
            var copyText = document.getElementById("<%= hfAddress.ClientID %>");
            if (!copyText) return;
            navigator.clipboard.writeText(copyText.value).then(function () { }).catch(function () { });
            alert("Copied to clipboard: " + copyText.value);
        }
    </script>
</asp:Content>
