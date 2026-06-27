<%@ Page Title="Settings" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="settings.aspx.cs" Inherits="QryptoCard.Dashboard.settings" %>

<%@ MasterType VirtualPath="~/Site.Master" %>
<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" runat="server">
    <link rel="stylesheet" href='<%= ResolveUrl("~/Content/css/settings.css") %>' />

    <asp:HiddenField runat="server" ID="hfEmailOtpId" />
    <asp:HiddenField runat="server" ID="hfNewEmail" />

    <div class="dash-top">
        <div>
            <h1>Settings</h1>
            <div class="sub">Manage your account.</div>
        </div>
    </div>

    <div class="set-col">

        <%-- Status feedback (success / error) for every action on the page --%>
        <asp:Panel runat="server" ID="pnlMsg" Visible="false">
            <asp:Label runat="server" ID="lblMsg" />
        </asp:Panel>

        <!-- PROFILE -->
        <section class="panel">
            <div class="panel-h"><h3>Profile</h3></div>
            <div class="set-form">
                <div class="set-row-2">
                    <div class="set-field">
                        <label for="<%= txtFirstName.ClientID %>">First name</label>
                        <asp:TextBox runat="server" ID="txtFirstName" autocomplete="given-name" />
                    </div>
                    <div class="set-field">
                        <label for="<%= txtLastName.ClientID %>">Last name</label>
                        <asp:TextBox runat="server" ID="txtLastName" autocomplete="family-name" />
                    </div>
                </div>
                <div class="set-actions">
                    <asp:Button runat="server" ID="btnSaveProfile" CssClass="btn btn-cyan" Text="Save changes" OnClick="btnSaveProfile_Click" UseSubmitBehavior="false" />
                </div>
            </div>
        </section>

        <!-- EMAIL -->
        <section class="panel">
            <div class="panel-h"><h3>Email address</h3></div>
            <div class="set-form">
                <div class="set-field">
                    <label for="<%= txtCurrentEmail.ClientID %>">Current email</label>
                    <asp:TextBox runat="server" ID="txtCurrentEmail" Enabled="false" />
                </div>
                <div class="set-field">
                    <label for="<%= txtNewEmail.ClientID %>">New email address</label>
                    <asp:TextBox runat="server" ID="txtNewEmail" TextMode="Email" placeholder="you@email.com" autocomplete="email" />
                </div>
                <div class="set-actions">
                    <asp:Button runat="server" ID="btnSendEmailOtp" CssClass="btn btn-line" Text="Send verification code" OnClick="btnSendEmailOtp_Click" UseSubmitBehavior="false" />
                </div>

                <%-- Step 2: shown only after a code has been sent to the new address --%>
                <asp:Panel runat="server" ID="pnlEmailOtp" Visible="false">
                    <div class="set-divide"></div>
                    <h4 class="set-subh">Confirm your new email</h4>
                    <div class="set-field">
                        <label for="<%= txtEmailOtp.ClientID %>">Verification code</label>
                        <asp:TextBox runat="server" ID="txtEmailOtp" placeholder="Enter the 6-digit code" autocomplete="one-time-code" />
                    </div>
                    <div class="set-actions">
                        <asp:Button runat="server" ID="btnConfirmEmail" CssClass="btn btn-cyan" Text="Confirm new email" OnClick="btnConfirmEmail_Click" UseSubmitBehavior="false" />
                    </div>
                    <%-- Resend reuses the send handler; styled as a subdued link, not a primary action --%>
                    <div class="otp-resend">Didn't get a code? <asp:LinkButton runat="server" ID="lnkResendEmailOtp" Text="Resend" OnClick="btnSendEmailOtp_Click" CausesValidation="false" /></div>
                </asp:Panel>
            </div>
        </section>

        <!-- SECURITY -->
        <section class="panel">
            <div class="panel-h"><h3>Security</h3></div>
            <div class="set-form">
                <h4 class="set-subh">Change password</h4>
                <div class="set-field">
                    <label for="<%= txtCurrentPw.ClientID %>">Current password</label>
                    <asp:TextBox runat="server" ID="txtCurrentPw" TextMode="Password" placeholder="Enter your current password" autocomplete="current-password" />
                </div>
                <div class="set-row-2">
                    <div class="set-field">
                        <label for="<%= txtNewPw.ClientID %>">New password</label>
                        <asp:TextBox runat="server" ID="txtNewPw" TextMode="Password" placeholder="At least 12 chars, mixed types" autocomplete="new-password" />
                    </div>
                    <div class="set-field">
                        <label for="<%= txtConfirmPw.ClientID %>">Confirm new password</label>
                        <asp:TextBox runat="server" ID="txtConfirmPw" TextMode="Password" placeholder="Re-enter new password" autocomplete="new-password" />
                    </div>
                </div>
                <div class="set-actions">
                    <asp:Button runat="server" ID="btnChangePw" CssClass="btn btn-line" Text="Update password" OnClick="btnChangePw_Click" UseSubmitBehavior="false" />
                </div>
            </div>
        </section>

        <!-- REFERRAL -->
        <section class="panel">
            <div class="panel-h"><h3>Referral</h3></div>
            <div class="set-form">
                <div class="set-inline">
                    <div class="set-field">
                        <label for="<%= txtReferralCode.ClientID %>">Referral code</label>
                        <asp:TextBox runat="server" ID="txtReferralCode" Enabled="false" />
                    </div>
                    <button type="button" class="set-copy" onclick="copyField('<%= txtReferralCode.ClientID %>'); return false;">Copy</button>
                </div>
                <div class="set-inline">
                    <div class="set-field">
                        <label for="<%= txtReferralLink.ClientID %>">Referral link</label>
                        <asp:TextBox runat="server" ID="txtReferralLink" Enabled="false" />
                    </div>
                    <button type="button" class="set-copy" onclick="copyField('<%= txtReferralLink.ClientID %>'); return false;">Copy</button>
                </div>
            </div>
        </section>

    </div>
</asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderID="DrawerContent" runat="server">
</asp:Content>
<asp:Content ID="Content3" ContentPlaceHolderID="ModalContent" runat="server">
</asp:Content>
<asp:Content ID="Content4" ContentPlaceHolderID="ScriptContent" runat="server">
    <script type="text/javascript">
        // Read-only referral fields: copy to clipboard without a postback (type="button").
        function copyField(id) {
            var el = document.getElementById(id);
            if (!el) return;
            navigator.clipboard.writeText(el.value).then(function () { }).catch(function () { });
        }
    </script>
</asp:Content>
