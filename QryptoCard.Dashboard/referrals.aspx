<%@ Page Title="Referrals" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="referrals.aspx.cs" Inherits="QryptoCard.Dashboard.referrals" %>

<%@ MasterType VirtualPath="~/Site.Master" %>
<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" runat="server">
    <asp:HiddenField runat="server" ID="hfReferralCode" />
    <asp:HiddenField runat="server" ID="hfReferralLink" />

    <!--begin::Page header-->
    <div class="dash-top">
        <div>
            <h1>Referrals</h1>
            <div class="sub">Share your link, track who joined, and see what you've earned.</div>
        </div>
    </div>
    <!--end::Page header-->

    <div class="dash-grid">
        <!--begin::Refer panel (live)-->
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
        <!--end::Refer panel-->

        <!--begin::Stats (live)-->
        <section class="stat-row">
            <div class="stat">
                <div class="ic"><svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.7"><path d="M12 1v22M17 5H9.5a3.5 3.5 0 000 7h5a3.5 3.5 0 010 7H6" /></svg></div>
                <div class="n"><asp:Literal runat="server" ID="lblCommissionRate" Text="-" /></div>
                <div class="l">Commission Rate</div>
            </div>
            <div class="stat">
                <div class="ic"><svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.7"><circle cx="12" cy="12" r="9" /><path d="M12 8v8M8 12h8" /></svg></div>
                <div class="n"><asp:Literal runat="server" ID="lblTotalCommission" Text="-" /></div>
                <div class="l">Total Commission</div>
            </div>
            <div class="stat">
                <div class="ic"><svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.7"><path d="M16 21v-2a4 4 0 00-4-4H6a4 4 0 00-4 4v2" /><circle cx="9" cy="7" r="4" /><path d="M22 21v-2a4 4 0 00-3-3.87M16 3.13a4 4 0 010 7.75" /></svg></div>
                <div class="n"><asp:Literal runat="server" ID="lblTotalReferrals" Text="-" /></div>
                <div class="l">Total Referrals</div>
            </div>
        </section>
        <!--end::Stats-->

        <!--begin::Referral history (live)-->
        <section class="panel" style="grid-column: 1 / -1;">
            <div class="panel-h"><h3>Referral history</h3></div>
            <div runat="server" id="divnoreferral" style="color: var(--ink-3); font-size: .95rem; padding: 14px 0;">
                <asp:Label runat="server" ID="lblNoReferral" Text="No one has joined with your link yet." />
            </div>
            <asp:GridView CssClass="data-table" ID="gvReferralList" runat="server" ShowHeader="true" AutoGenerateColumns="false" DataKeyNames="UserID" AllowPaging="True" PageSize="50" GridLines="None" OnPageIndexChanging="gvReferralList_PageIndexChanging">
                <PagerStyle HorizontalAlign="Center" />
                <Columns>
                    <asp:TemplateField HeaderText="Referral" ItemStyle-HorizontalAlign="Left" HeaderStyle-HorizontalAlign="Left">
                        <ItemTemplate><%# RefName(Container.DataItem) %></ItemTemplate>
                    </asp:TemplateField>
                    <asp:TemplateField HeaderText="Joined">
                        <ItemTemplate><%# RefJoined(Container.DataItem) %></ItemTemplate>
                    </asp:TemplateField>
                    <asp:TemplateField HeaderText="Earned">
                        <ItemTemplate><%# RefEarned(Container.DataItem) %></ItemTemplate>
                    </asp:TemplateField>
                    <asp:TemplateField HeaderText="Status">
                        <ItemTemplate><%# RefStatus(Container.DataItem) %></ItemTemplate>
                    </asp:TemplateField>
                </Columns>
            </asp:GridView>
        </section>
        <!--end::Referral history-->

        <!--begin::Commission history (live)-->
        <section class="panel" style="grid-column: 1 / -1;">
            <div class="panel-h"><h3>Commission history</h3></div>
            <div runat="server" id="divnocommission" style="color: var(--ink-3); font-size: .95rem; padding: 14px 0;">
                <asp:Label runat="server" ID="lblNoCommission" Text="No commission yet." />
            </div>
            <asp:GridView CssClass="data-table" ID="gvCommissionList" runat="server" ShowHeader="true" AutoGenerateColumns="false" AllowPaging="True" PageSize="50" GridLines="None" OnPageIndexChanging="gvCommissionList_PageIndexChanging">
                <PagerStyle HorizontalAlign="Center" />
                <Columns>
                    <asp:TemplateField HeaderText="Date">
                        <ItemTemplate><%# CommWhen(Container.DataItem) %></ItemTemplate>
                    </asp:TemplateField>
                    <asp:TemplateField HeaderText="From referral" ItemStyle-HorizontalAlign="Left" HeaderStyle-HorizontalAlign="Left">
                        <ItemTemplate><%# CommReferral(Container.DataItem) %></ItemTemplate>
                    </asp:TemplateField>
                    <asp:TemplateField HeaderText="Commission">
                        <ItemTemplate><%# CommAmount(Container.DataItem) %></ItemTemplate>
                    </asp:TemplateField>
                </Columns>
            </asp:GridView>
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
</asp:Content>
