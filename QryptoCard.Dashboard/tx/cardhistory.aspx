<%@ Page Title="Card History" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="cardhistory.aspx.cs" Inherits="QryptoCard.Dashboard.tx.cardhistory" %>

<%@ MasterType VirtualPath="~/Site.Master" %>

<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" runat="server">
    <asp:HiddenField runat="server" ID="hfID" />
    <div class="dash-top">
        <div>
            <h1>Card History</h1>
            <div class="sub">Your card purchases and their status.</div>
        </div>
    </div>

    <%-- Inline result banner (NewDesign): server-rendered so it works WITHOUT the Bootstrap/
         jQuery the NewDesign shell doesn't load — this is the SF-11 carddetail pattern. The old
         success/error Bootstrap modals here silently no-op'd (no Metronic JS), so cancel results
         were invisible. --%>
    <asp:Panel runat="server" ID="pnlMsg" Visible="false" CssClass="hist-banner err">
        <span runat="server" id="lblalert"></span>
    </asp:Panel>

    <!--begin::Content-->
    <div>
            <div class="panel">
                <!--begin::Row-->
                <div>
                    <!--begin::Table container-->
                    <div class="table-responsive">
                        <!--begin::Table-->

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
                                        <%-- Inline NewDesign row actions. The Metronic data-kt-menu dropdown never opened
                                             in the NewDesign shell (no Metronic JS loaded). Payment link is now a direct
                                             anchor (no popup-blocker-prone window.open); Cancel posts back to a server-
                                             rendered confirm. "More detail" was dropped — it had no detail view even
                                             before the re-skin. --%>
                                        <div class="row-actions" runat="server" visible='<%# IsStatusCreatedBadge((string)Eval("Status")) %>'>
                                            <a class="btn btn-line btn-sm" href='<%# Eval("DetailURL") %>' target="_blank" rel="noopener">Payment link</a>
                                            <button runat="server" id="btnCancel" onserverclick="btnCancel_ServerClick" class="btn btn-danger btn-sm">Cancel</button>
                                        </div>
                                    </ItemTemplate>
                                </asp:TemplateField>
                            </Columns>
                        </asp:GridView>


                        <!--end::Table-->
                    </div>
                    <!--end::Table container-->

                    <div class="data-empty" runat="server" id="divnorow">
                        <center>
                            <asp:Label runat="server" ID="lblNoRow" Text="No card transaction at the moment." /></center>
                    </div>
                </div>
                <!--end::Row-->
            </div>
        </div>
        <!--end::Content-->
</asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderID="DrawerContent" runat="server">
</asp:Content>
<asp:Content ID="Content3" ContentPlaceHolderID="ModalContent" runat="server">
    <%-- Cancel confirmation. Server-rendered overlay (Visible toggled in code-behind) so it
         needs no Bootstrap/jQuery — shown by btnCancel, dismissed by either button. The actual
         cancel (btnCancelExec) is a normal postback; the result shows in the inline banner. --%>
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
</asp:Content>
