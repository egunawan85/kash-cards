<%@ Page Title="Card History" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="cardhistory.aspx.cs" Inherits="QryptoCard.Dashboard.tx.cardhistory" %>

<%@ MasterType VirtualPath="~/Site.Master" %>

<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" runat="server">
    <asp:HiddenField runat="server" ID="hfReferralCode" />
    <asp:HiddenField runat="server" ID="hfID" />
    <asp:HiddenField runat="server" ID="hfReferralLink" />
    <div class="dash-top">
        <div>
            <h1>Card History</h1>
            <div class="sub">Your card purchases and their status.</div>
        </div>
    </div>
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
                                <asp:TemplateField ItemStyle-HorizontalAlign="Center" ItemStyle-VerticalAlign="Middle" HeaderStyle-Width="50px">
                                    <ItemTemplate>
                                        <div class="card-toolbar justify-content-end">
                                            <button class="btn btn-icon btn-color-gray-500 btn-active-color-primary justify-content-end" style="height: 0" data-kt-menu-trigger="click" data-kt-menu-placement="bottom-end" data-kt-menu-overflow="true">
                                                <i class="ki-duotone ki-dots-square fs-1 text-gray-500 me-n1">
                                                    <span class="path1"></span>
                                                    <span class="path2"></span>
                                                    <span class="path3"></span>
                                                    <span class="path4"></span>
                                                </i>
                                            </button>
                                            <div class="menu menu-sub menu-sub-dropdown menu-column menu-rounded menu-gray-800 menu-state-bg-light-primary fw-semibold w-200px" data-kt-menu="true">
                                                <div class="menu-item px-3">
                                                    <div class="menu-content fs-6 text-gray-900 fw-bold px-3 py-4">Actions</div>
                                                </div>
                                                <div class="separator mb-3 opacity-75"></div>
                                                <div class="menu-item px-3">
                                                    <button runat="server" id="btnInfo" onserverclick="btnInfo_ServerClick" class="btn btn-active-light-primary btn-sm px-3 text-start" style="width: 100%">More detail</button>
                                                </div>
                                                <div class="menu-item px-3" runat="server" visible='<%# IsStatusCreatedBadge((string)Eval("Status")) %>'>
                                                    <button runat="server" id="btnCancel" onserverclick="btnCancel_ServerClick" class="btn btn-active-light-danger btn-sm px-3 text-start" style="width: 100%">Cancel</button>
                                                </div>
                                                <div class="menu-item px-3" runat="server" visible='<%# IsStatusCreatedBadge((string)Eval("Status")) %>'>
                                                    <button runat="server" id="btnLink" onserverclick="btnLink_ServerClick" class="btn btn-active-light-warning btn-sm px-3 text-start" style="width: 100%">Open payment link</button>
                                                </div>
                                                <div class="separator mt-3 opacity-75"></div>
                                            </div>
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
    <div class="modal fade" tabindex="-1" id="successModal" aria-hidden="true">
        <div class="modal-dialog">
            <div class="modal-content">
                <div class="modal-header">
                    <h3 class="modal-title">Success</h3>

                    <!--begin::Close-->
                    <div class="btn btn-icon btn-sm btn-active-light-primary ms-2" data-bs-dismiss="modal" aria-label="Close">
                        <i class="ki-duotone ki-cross fs-1"><span class="path1"></span><span class="path2"></span></i>
                    </div>
                    <!--end::Close-->
                </div>

                <div class="modal-body">
                    <label class="form-label" runat="server" id="lblSuccess"></label>
                </div>

                <div class="modal-footer">
                    <button class="btn btn-danger" type="button" data-bs-dismiss="modal">Close</button>
                </div>
            </div>
        </div>
    </div>

    <div class="modal fade" tabindex="-1" id="alertModal" aria-hidden="true">
        <div class="modal-dialog">
            <div class="modal-content">
                <div class="modal-header">
                    <h3 class="modal-title">Error</h3>

                    <!--begin::Close-->
                    <div class="btn btn-icon btn-sm btn-active-light-primary ms-2" data-bs-dismiss="modal" aria-label="Close">
                        <i class="ki-duotone ki-cross fs-1"><span class="path1"></span><span class="path2"></span></i>
                    </div>
                    <!--end::Close-->
                </div>

                <div class="modal-body">
                    <label class="form-label" runat="server" id="lblalert"></label>
                </div>

                <div class="modal-footer">
                    <button class="btn btn-danger" type="button" data-bs-dismiss="modal">Close</button>
                </div>
            </div>
        </div>
    </div>

    <div class="modal fade" tabindex="-1" id="myModalCancel" aria-hidden="true" data-backdrop="static">
        <div class="modal-dialog">
            <div class="modal-content">
                <div class="modal-header">
                    <h3 class="modal-title">Cancel Card Transaction</h3>

                    <div class="btn btn-icon btn-sm btn-active-light-primary ms-2" data-bs-dismiss="modal" aria-label="Close">
                        <i class="ki-duotone ki-cross fs-1"><span class="path1"></span><span class="path2"></span></i>
                    </div>
                </div>

                <div class="modal-body">
                    <div class="alert alert-dismissible bg-light-danger d-flex flex-column flex-sm-row p-5 mb-10" runat="server" id="divfailedcancel" visible="false">
                        <i class="ki-duotone ki-notification-bing fs-2hx text-danger me-4 mb-5 mb-sm-0"><span class="path1"></span><span class="path2"></span><span class="path3"></span></i>
                        <div class="d-flex flex-column pe-0 pe-sm-10">
                            <h4 class="fw-semibold">Failed</h4>
                            <span>
                                <asp:Label runat="server" ID="lblFailedCancel" Text="Error message" /></span>
                        </div>
                        <button type="button" class="position-absolute position-sm-relative m-2 m-sm-0 top-0 end-0 btn btn-icon ms-sm-auto" runat="server" id="btndfailedcancel" onserverclick="btndfailedcancel_ServerClick">
                            <i class="ki-duotone ki-cross fs-1 text-danger"><span class="path1"></span><span class="path2"></span></i>
                        </button>
                    </div>

                    <div class="text-muted text-start fw-semibold fs-5 mb-5">Are you sure you want to cancel this card transaction?</div>
                    <br />


                </div>

                <div class="modal-footer">
                    <button class="btn btn-danger" type="button" data-bs-dismiss="modal">Close</button>
                    <asp:Button CssClass="btn btn-warning" runat="server" ID="btnCancelExec" Text="Cancel" OnClick="btnCancelExec_Click" />
                </div>
            </div>
        </div>
    </div>
</asp:Content>
<asp:Content ID="Content4" ContentPlaceHolderID="ScriptContent" runat="server">
    <script type="text/javascript">
    $(window).load(function () {

        $("input[id*='txtduedate]").datepicker({
            format: "yyyy/mm/dd"

        });
        //$("input[id*='txtStartDate]").datepicker({
        //    format: "yyyy/mm/dd"

        //});
    });

        $(window).load(function () {
            $('#myModalAdd').modal({ backdrop: 'static', keyboard: false });
            if (window.isModalAdd) {
                $('#myModalAdd').modal('show');
            }
            else {
                $('#myModalAdd').modal('hide');
            }
            console.log(window.isModalAdd);
            console.log('this is another debugging message');
        });

        $(window).load(function () {
            $('#myModalCancel').modal({ backdrop: 'static', keyboard: false });
            if (window.isModalCancel) {
                $('#myModalCancel').modal('show');
            }
            else {
                $('#myModalCancel').modal('hide');
            }
            console.log(window.isModalCancel);
            console.log('this is another debugging message');
        });


        $(window).load(function () {
            //$('#alertModal').modal({ backdrop: 'static', keyboard: false });
            if (window.isModalAlert) {
                $('#alertModal').modal('show');
            }
            else {
                $('#alertModal').modal('hide');
            }
            console.log(window.isModalAlert);
            console.log('this is another debugging message');
        });


        $(window).load(function () {
            //$('#alertModal').modal({ backdrop: 'static', keyboard: false });
            if (window.isModalSuccess) {
                $('#successModal').modal('show');
            }
            else {
                $('#successModal').modal('hide');
            }
            console.log(window.isModalSuccess);
            console.log('this is another debugging message');
        });
    

    $(window).load(function () {
        if (window.isModalPopup) {
            $('#popupModal').modal('show');
            var myModal = $('#popupModal');
            clearTimeout(myModal.data('hideInterval'));
            myModal.data('hideInterval', setTimeout(function () {
                myModal.modal('hide');
            }, 1000));
        }
        else {
            $('#popupModal').modal('hide');
        }
        console.log(window.isModalPopup);
        console.log('this is another debugging for popup');
    });

        function copyReferralCode() {
            var copyText = document.getElementById("<%= hfReferralCode.ClientID %>");
        //copyText.select();
        //document.execCommand("copy");
        navigator.clipboard.writeText(copyText.value).then(() => { }).catch((error) => { })
        console.log("copytext =>" + copyText.value);
        alert("Copied to clipboard: " + copyText.value);
        }

        function copyReferralLink() {
            var copyText = document.getElementById("<%= hfReferralLink.ClientID %>");
        //copyText.select();
        //document.execCommand("copy");
        navigator.clipboard.writeText(copyText.value).then(() => { }).catch((error) => { })
        console.log("copytext =>" + copyText.value);
        alert("Copied to clipboard: " + copyText.value);
    }


    function copyAddress(addr) {
        navigator.clipboard.writeText(addr).then(() => { }).catch((error) => { })
    }
    </script>
</asp:Content>