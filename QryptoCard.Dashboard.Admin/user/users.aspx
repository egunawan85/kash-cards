<%@ Page Title="" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="users.aspx.cs" Inherits="QryptoCard.Dashboard.Admin.user.users" %>


<%@ MasterType VirtualPath="~/Site.Master" %>
<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" runat="server">
    <asp:HiddenField runat="server" ID="hfMID" />
    <asp:HiddenField runat="server" ID="hfTID" />
    <asp:HiddenField runat="server" ID="hfCID" />
    <asp:HiddenField runat="server" ID="hfStatus" />

    <div id="kt_app_content" class="app-content">
        <!--begin::Post-->
        <div class="content flex-row-fluid" id="kt_content">

            <!--begin::Alert-->
            <div class="alert alert-dismissible bg-light-danger d-flex flex-column flex-sm-row p-5 mb-10" runat="server" id="divfailed" visible="false">
                <!--begin::Icon-->
                <i class="ki-duotone ki-notification-bing fs-2hx text-danger me-4 mb-5 mb-sm-0"><span class="path1"></span><span class="path2"></span><span class="path3"></span></i>
                <!--end::Icon-->

                <!--begin::Wrapper-->
                <div class="d-flex flex-column pe-0 pe-sm-10">
                    <!--begin::Title-->
                    <h4 class="fw-semibold">Failed</h4>
                    <!--end::Title-->

                    <!--begin::Content-->
                    <span>
                        <asp:Label runat="server" ID="lblError" Text="Error message" /></span>
                    <!--end::Content-->
                </div>
                <!--end::Wrapper-->

                <!--begin::Close-->
                <button type="button" class="position-absolute position-sm-relative m-2 m-sm-0 top-0 end-0 btn btn-icon ms-sm-auto" runat="server" id="btnFailedClose" onserverclick="btnFailedClose_ServerClick">
                    <i class="ki-duotone ki-cross fs-1 text-danger"><span class="path1"></span><span class="path2"></span></i>
                </button>
                <!--end::Close-->
            </div>
            <!--end::Alert-->

            <!--begin::Alert-->
            <div class="alert alert-dismissible bg-light-primary d-flex flex-column flex-sm-row p-5 mb-10" runat="server" id="divsuccess" visible="false">
                <!--begin::Icon-->
                <i class="ki-duotone ki-notification-bing fs-2hx text-primary me-4 mb-5 mb-sm-0"><span class="path1"></span><span class="path2"></span><span class="path3"></span></i>
                <!--end::Icon-->

                <!--begin::Wrapper-->
                <div class="d-flex flex-column pe-0 pe-sm-10">
                    <!--begin::Title-->
                    <h4 class="fw-semibold">Success</h4>
                    <!--end::Title-->

                    <!--begin::Content-->
                    <span>
                        <asp:Label runat="server" ID="lblSuccess" Text="Error message" /></span>
                    <!--end::Content-->
                </div>
                <!--end::Wrapper-->

                <!--begin::Close-->
                <button type="button" class="position-absolute position-sm-relative m-2 m-sm-0 top-0 end-0 btn btn-icon ms-sm-auto" runat="server" id="btnSuccessClose" onserverclick="btnSuccessClose_ServerClick">
                    <i class="ki-duotone ki-cross fs-1 text-primary"><span class="path1"></span><span class="path2"></span></i>
                </button>
                <!--end::Close-->
            </div>
            <!--end::Alert-->

            <!--begin::Tables Widget 13-->
            <div class="card mb-5 mb-xl-8">
                <!--begin::Header-->
                <div class="card-header border-0 pt-5">
                    <h3 class="card-title align-items-start flex-column">
                        <span class="card-label fw-bold fs-3 mb-1">User List</span>
                        <%--<span class="text-muted mt-1 fw-semibold fs-7">Over 500 orders</span>--%>
                    </h3>
                    <div class="card-toolbar">
                        <!--begin::Menu-->
                        <button type="button" class="btn btn-sm btn-icon btn-color-primary btn-active-light-primary" data-kt-menu-trigger="click" data-kt-menu-placement="bottom-end">
                            <i class="ki-duotone ki-category fs-6">
                                <span class="path1"></span>
                                <span class="path2"></span>
                                <span class="path3"></span>
                                <span class="path4"></span>
                            </i>
                        </button>
                        <!--begin::Menu 2-->
                        <div class="menu menu-sub menu-sub-dropdown menu-column menu-rounded menu-gray-800 menu-state-bg-light-primary fw-semibold w-200px" data-kt-menu="true">
                            <!--begin::Menu item-->
                            <div class="menu-item px-3">
                                <div class="menu-content fs-6 text-gray-900 fw-bold px-3 py-4">Actions</div>
                            </div>
                            <!--end::Menu item-->
                            <!--begin::Menu separator-->
                            <div class="separator mb-3 opacity-75"></div>
                            <!--end::Menu separator-->
                            <!--begin::Menu item-->
                            <div class="menu-item px-3">
                                <asp:Button runat="server" ID="btnExportExcel" CssClass="btn btn-active-light-info btn-sm px-3" Text="Export to Excel" />
                                <%--<a href="#" class="menu-link px-3">Export to Excel</a>--%>
                            </div>
                            <!--end::Menu item-->
                            <!--begin::Menu separator-->
                            <div class="separator mt-3 opacity-75"></div>
                            <!--end::Menu separator-->

                            <%--<!--begin::Menu item-->
                <div class="menu-item px-3">
                    <div class="menu-content px-3 py-3">
                        <a class="btn btn-primary btn-sm px-4" href="#">Generate Reports</a>
                    </div>
                </div>
                <!--end::Menu item-->--%>
                        </div>
                        <!--end::Menu 2-->
                        <!--end::Menu-->
                    </div>
                </div>
                <!--end::Header-->
                <!--begin::Body-->
                <div class="card-body py-3">

                    <!--begin::Table container-->
                    <div class="table-responsive">
                        <!--begin::Table-->

                        <asp:GridView CssClass="table table-striped gy-5 gs-7" ID="gvListItem" HeaderStyle-CssClass="header-table" runat="server" AutoGenerateColumns="false" DataKeyNames="UserID" AllowPaging="True" PageSize="50" AllowCustomPaging="False" OnPageIndexChanging="gvListItem_PageIndexChanging" OnRowCreated="gvListItem_RowCreated">
                            <PagerStyle HorizontalAlign="Center" CssClass="bs4-aspnet-pager" />
                            <Columns>
                                <asp:TemplateField HeaderText="No." ItemStyle-HorizontalAlign="Center" ItemStyle-VerticalAlign="Middle" HeaderStyle-Width="50px">
                                    <ItemTemplate>
                                        <asp:HiddenField runat="server" ID="hfID" Value='<%# Eval("UserID") %>' />
                                        <%--<asp:HiddenField runat="server" ID="hfURL" Value='<%# Eval("URL") %>' />--%>
                                        <%# Container.DataItemIndex + 1 %>
                                    </ItemTemplate>
                                </asp:TemplateField>
                                <asp:BoundField DataField="UserID" HeaderText="User ID" ItemStyle-HorizontalAlign="Center" ItemStyle-VerticalAlign="Middle" HeaderStyle-Width="100px" />
                                <asp:BoundField DataField="Email" HeaderText="Email" ItemStyle-HorizontalAlign="Center" ItemStyle-VerticalAlign="Middle" HeaderStyle-Width="100px" />
                                <asp:BoundField DataField="TotalCard" HeaderText="Card Prurchased" ItemStyle-HorizontalAlign="Center" ItemStyle-VerticalAlign="Middle" HeaderStyle-Width="100px" />
                                <asp:BoundField DataField="DateJoin" HeaderText="Join Date" ItemStyle-HorizontalAlign="Center" ItemStyle-VerticalAlign="Middle" HeaderStyle-Width="100px" />
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

                    <div class="align-content-center" style="margin-bottom: 20px" runat="server" id="divnorow">
                        <center>
                            <asp:Label runat="server" ID="lblNoRow" Text="No card at the moment." /></center>
                    </div>
                    <%--<div class="float-end" style="margin-bottom: 10px; margin-top: 10px">
                <asp:Button CssClass="btn btn-primary" runat="server" ID="btnGenerate" Text="Invite Admin" OnClick="btnGenerate_Click" Visible="false" />
            </div>--%>
                </div>
                <!--begin::Body-->
            </div>
            <!--end::Tables Widget 13-->
        </div>
        <!--end::Post-->
    </div>


</asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderID="DrawerContent" runat="server">
</asp:Content>
<asp:Content ID="Content3" ContentPlaceHolderID="ModalContent" runat="server">
    <div class="modal fade" tabindex="-1" id="myModalAdd" aria-hidden="true" data-backdrop="static">
        <div class="modal-dialog modal-lg">
            <div class="modal-content">
                <div class="modal-header">
                    <h3 class="modal-title">Invite Admin</h3>

                    <div class="btn btn-icon btn-sm btn-active-light-primary ms-2" data-bs-dismiss="modal" aria-label="Close">
                        <i class="ki-duotone ki-cross fs-1"><span class="path1"></span><span class="path2"></span></i>
                    </div>
                </div>

                <div class="modal-body">
                    <div class="alert alert-dismissible bg-light-danger d-flex flex-column flex-sm-row p-5 mb-10" runat="server" id="divfailedadd" visible="false">
                        <i class="ki-duotone ki-notification-bing fs-2hx text-danger me-4 mb-5 mb-sm-0"><span class="path1"></span><span class="path2"></span><span class="path3"></span></i>
                        <div class="d-flex flex-column pe-0 pe-sm-10">
                            <h4 class="fw-semibold">Failed</h4>
                            <span>
                                <asp:Label runat="server" ID="lblFailedAdd" Text="Error message" /></span>
                        </div>
                        <button type="button" class="position-absolute position-sm-relative m-2 m-sm-0 top-0 end-0 btn btn-icon ms-sm-auto" runat="server" id="btndfailedadd" onserverclick="btndfailedadd_ServerClick">
                            <i class="ki-duotone ki-cross fs-1 text-danger"><span class="path1"></span><span class="path2"></span></i>
                        </button>
                    </div>

                    <%--<div class="text-muted text-start fw-semibold fs-5 mb-5">You will get static address for your merchant that have no expiration time.</div>--%>

                    <label class="form-label">First Name : </label>
                    <div class="input-group">
                        <asp:TextBox runat="server" ID="txtFirstNameAdd" CssClass="form-control" />
                    </div>
                    <br />
                    <label class="form-label">Last Name : </label>
                    <div class="input-group">
                        <asp:TextBox runat="server" ID="txtLastNameAdd" CssClass="form-control" />
                    </div>
                    <br />
                    <label class="form-label">Role : </label>
                    <div class="input-group">
                        <asp:DropDownList runat="server" ID="ddlRoleAdd" CssClass="form-control" AutoPostBack="true" OnSelectedIndexChanged="ddlRoleAdd_SelectedIndexChanged" CausesValidation="false">
                            <asp:ListItem Text="Admin" Value="Admin"></asp:ListItem>
                            <%--<asp:ListItem Text="Approver" Value="Approver"></asp:ListItem>
                        <asp:ListItem Text="Signer" Value="Signer"></asp:ListItem>--%>
                            <asp:ListItem Text="Viewer" Value="Viewer"></asp:ListItem>
                        </asp:DropDownList>
                    </div>
                    <br />
                    <label class="form-label">Email : </label>
                    <div class="input-group">
                        <asp:TextBox runat="server" ID="txtEmailAdd" CssClass="form-control" TextMode="Email" />
                    </div>
                    <br />

                </div>

                <div class="modal-footer">
                    <button class="btn btn-danger" type="button" data-bs-dismiss="modal">Close</button>
                    <asp:Button CssClass="btn btn-success" runat="server" ID="btnCreateExec" Text="Invite" OnClick="btnCreateExec_Click" />
                </div>
            </div>
        </div>
    </div>

    <div class="modal fade" tabindex="-1" id="myModalDetail" aria-hidden="true" data-backdrop="static">
        <div class="modal-dialog modal-lg">
            <div class="modal-content">
                <div class="modal-header">
                    <h3 class="modal-title">Card Detail</h3>

                    <div class="btn btn-icon btn-sm btn-active-light-primary ms-2" data-bs-dismiss="modal" aria-label="Close">
                        <i class="ki-duotone ki-cross fs-1"><span class="path1"></span><span class="path2"></span></i>
                    </div>
                </div>

                <div class="modal-body">
                    <div class="alert alert-dismissible bg-light-danger d-flex flex-column flex-sm-row p-5 mb-10" runat="server" id="divfaileddetail" visible="false">
                        <i class="ki-duotone ki-notification-bing fs-2hx text-danger me-4 mb-5 mb-sm-0"><span class="path1"></span><span class="path2"></span><span class="path3"></span></i>
                        <div class="d-flex flex-column pe-0 pe-sm-10">
                            <h4 class="fw-semibold">Failed</h4>
                            <span>
                                <asp:Label runat="server" ID="lblFailedDetail" Text="Error message" /></span>
                        </div>
                        <button type="button" class="position-absolute position-sm-relative m-2 m-sm-0 top-0 end-0 btn btn-icon ms-sm-auto" runat="server" id="btndfaileddetail" onserverclick="btndfaileddetail_ServerClick">
                            <i class="ki-duotone ki-cross fs-1 text-danger"><span class="path1"></span><span class="path2"></span></i>
                        </button>
                    </div>


                    <label class="form-label">Organization </label>
                    <div class="input-group">
                        <asp:TextBox runat="server" ID="txtOrganizationDetail" Enabled="false" CssClass="form-control" />
                    </div>
                    <br />
                    <label class="form-label">Bank Card Bin </label>
                    <div class="input-group">
                        <asp:TextBox runat="server" ID="txtBankCardBinDetail" Enabled="false" CssClass="form-control" />
                    </div>
                    <br />
                    <label class="form-label">Description/Support </label>
                    <div class="input-group">
                        <asp:TextBox runat="server" ID="txtCardDescDetail" Enabled="false" CssClass="form-control" TextMode="MultiLine" Rows="4" />
                    </div>
                    <br />
                    <label class="form-label">Price </label>
                    <div class="input-group">
                        <asp:TextBox runat="server" ID="txtCardPriceDetail" Enabled="false" CssClass="form-control" />
                    </div>
                    <br />
                    <label class="form-label">Recharge Fee Rate </label>
                    <div class="input-group">
                        <asp:TextBox runat="server" ID="txtRechargeFeeRateDetail" Enabled="false" CssClass="form-control" />
                    </div>
                    <br />
                    <label class="form-label">Need Cardholder? </label>
                    <div class="input-group">
                        <span class="badge badge-light-success badge-lg" runat="server" id="badgeCardholder" visible="false">yes</span>
                        <span class="badge badge-light-danger badge-lg" runat="server" id="badgeCardholderNot" visible="false">no</span>
                    </div>
                    <br />
                    <label class="form-label">Minimum Deposit </label>
                    <div class="input-group">
                        <asp:TextBox runat="server" ID="txtMinDepositDetail" CssClass="form-control" Enabled="false" />
                    </div>
                    <br />
                    <label class="form-label">Maximum Deposit </label>
                    <div class="input-group">
                        <asp:TextBox runat="server" ID="txtMaxDepositDetail" CssClass="form-control" Enabled="false" />
                    </div>
                    <br />
                    <%--<label class="form-label">Invited By </label>
                <div class="input-group">
                    <asp:TextBox runat="server" ID="txtInvitedByDetail" Enabled="false" CssClass="form-control" />
                </div>
                <br />--%>
                </div>

                <div class="modal-footer">
                    <button class="btn btn-danger" type="button" data-bs-dismiss="modal">Close</button>
                </div>
            </div>
        </div>
    </div>

    


    <div class="modal fade" id="popupModal" tabindex="-1" aria-hidden="true" data-backdrop="static">
        <div class="modal-dialog">
            <div class="modal-content">
                <div class="modal-body">
                    <asp:Label runat="server" ID="Label2" Text="Copied to clipboard" />
                </div>
            </div>
        </div>
    </div>
</asp:Content>


<asp:Content ID="Content4" ContentPlaceHolderID="ScriptContent" runat="server">
    <script type="text/javascript">

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
            $('#myModalDetail').modal({ backdrop: 'static', keyboard: false });
            if (window.isModalDetail) {
                $('#myModalDetail').modal('show');
            }
            else {
                $('#myModalDetail').modal('hide');
            }
            console.log(window.isModalDetail);
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


        function copyAddress(addr) {
            navigator.clipboard.writeText(addr).then(() => { }).catch((error) => { })
        }
    </script>
    <style type="text/css">
        .header-table {
            text-align: center;
            /*color: white;*/
        }
    </style>
</asp:Content>
