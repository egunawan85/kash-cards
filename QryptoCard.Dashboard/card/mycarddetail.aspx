<%@ Page Title="Card Detail" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="mycarddetail.aspx.cs" Inherits="QryptoCard.Dashboard.card.mycarddetail" EnableEventValidation="false" %>

<%@ MasterType VirtualPath="~/Site.Master" %>

<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" runat="server">
    <asp:HiddenField runat="server" ID="hfIsHolderNeeded" />
    <asp:HiddenField runat="server" ID="hfCardID" />
    <asp:HiddenField runat="server" ID="hfCardNo" />
    <asp:HiddenField runat="server" ID="hfReferralLink" />
    <asp:HiddenField runat="server" ID="hfCardTypeID" />
    <asp:HiddenField runat="server" ID="hfHolderID" />
    <asp:HiddenField runat="server" ID="hfDepositFeeRate" />
    <asp:HiddenField runat="server" ID="hfMinDeposit" />
    <asp:HiddenField runat="server" ID="hfMaxDeposit" />
    <asp:HiddenField runat="server" ID="hfCardNumber" />
    <asp:HiddenField runat="server" ID="hfCVV" />
    <asp:HiddenField runat="server" ID="hfExpDate" />
    <asp:HiddenField runat="server" ID="hfCVVDecr" />
    <asp:HiddenField runat="server" ID="hfExpDateDecr" />

    
    <asp:HiddenField runat="server" ID="hfCardholder" />
    <asp:HiddenField runat="server" ID="hfPhone" />
    <asp:HiddenField runat="server" ID="hfEmail" />
    <asp:HiddenField runat="server" ID="hfAddress" />
    <asp:HiddenField runat="server" ID="hfCity" />
    <asp:HiddenField runat="server" ID="hfState" />
    <asp:HiddenField runat="server" ID="hfCountry" />
    <asp:HiddenField runat="server" ID="hfZipCode" />
    
    <asp:HiddenField runat="server" ID="hfOTPID" />
    <div class="d-flex flex-column flex-column-fluid">
        <!--begin::Toolbar-->
        <div id="kt_app_toolbar" class="app-toolbar d-flex pb-3 pb-lg-5">
            <!--begin::Toolbar container-->
            <div class="d-flex flex-stack flex-row-fluid">
                <!--begin::Toolbar container-->
                <div class="d-flex flex-column flex-row-fluid">
                    <!--begin::Toolbar wrapper-->
                    <!--begin::Page title-->
                    <div class="page-title d-flex align-items-center me-3">
                        <!--begin::Title-->
                        <h1 class="page-heading d-flex flex-column justify-content-center text-gray-900 fw-bold fs-lg-2x gap-2">
                            <span>Card Detail</span>
                            <!--begin::Description-->
                            <%--<span class="page-desc text-gray-600 fs-base fw-semibold">You are logged in as a Cloud Owner</span>--%>
                            <!--end::Description-->
                        </h1>
                        <!--end::Title-->
                    </div>
                    <!--end::Page title-->
                </div>
                <!--end::Toolbar container-->
                <!--begin::Actions-->
                <div class="d-flex align-self-center flex-center flex-shrink-0">
                    <%--<a href='<%= ResolveUrl("~/card/cardlist")%>' class="btn btn-sm btn-success d-flex flex-center ms-3 px-4 py-3">
                        <i class="ki-outline ki-plus-square fs-2"></i>
                        <span>Buy Card</span>
                    </a>--%>
                    <%--<a href="#" class="btn btn-sm btn-dark ms-3 px-4 py-3" data-bs-toggle="modal" data-bs-target="#kt_modal_new_target">Create 
						
                    <span class="d-none d-sm-inline">Target</span></a>--%>
                </div>
                <!--end::Actions-->
            </div>
            <!--end::Toolbar container-->
        </div>
        <!--end::Toolbar-->
        <!--begin::Content-->
        <div id="kt_app_content" class="app-content flex-column-fluid">
            <!--begin::Content-->
            <div class="row">
                <div class="col-xs-12 col-sm-12 col-md-12 col-lg-6 col-xl-6 mb-5">
                    <%--flex-lg-row-fluid mb-10 mb-lg-0 me-lg-7 me-xl-10--%>
                    <!--begin::Card-->
                    <div class="card">
                        <!--begin::Card body-->
                        <div class="card-body p-12">
                            <!--begin::Form-->
                            <!-- Classic Card -->
                            <div class="col-xs-12 col-sm-12 col-md-12 clearfix mb-10" style="cursor: pointer;">
                                <div class="credit-card text-white p-4 d-flex flex-column justify-content-between" style="background-image: url(../Content/media/card-bg.png); background-size:100%">
                                    <div class="d-flex justify-content-between align-items-center mb-5">
                                        <h1><span runat="server" id="lblCardPrice" visible="false">Qrypto Card</span></h1>
                                        <%--<i class="fab fa-cc-mastercard fa-2x"></i>--%>
                                    </div>
                                    <div class="card-number my-3 mt-10">
                                        <span runat="server" id="lblCardNo" class="fs-2">0000</span>
                                    </div>

                                    <div class="d-flex justify-content-between mt-3">
                                        <div>
                                            <div class="card-holder"><span runat="server" id="lblCardname">Qrypto</span></div>
                                            <div></div>
                                            <%--<div>Deposit Limit : <span runat="server" id="lblDepositLimit">0 USD</span></div>--%>
                                        </div>
                                        <img runat="server" id="imgOrg" alt="Chip" width="40">
                                        <%--<img src="https://www.svgrepo.com/show/508703/mastercard.svg" alt="Chip" width="40">--%>
                                        <%--<img src="https://www.svgrepo.com/show/362035/visa-3.svg" alt="Chip" width="40">--%>
                                    </div>
                                </div>
                            </div>
                            <div class="row mb-10" style="text-align: center;">
                                <div class="col">
                                    <button class="btn btn-icon btn-secondary mb-2" style="text-align: center;" runat="server" id="btnRecharge" onserverclick="btnRecharge_ServerClick"><i class="bi bi-download fs-4"></i></button>
                                    <br />
                                    <span>Recharge</span>
                                </div>
                                <div class="col">
                                    <button class="btn btn-icon btn-secondary mb-2" runat="server" id="btnDetail" onserverclick="btnDetail_ServerClick" onclick="this.disabled=true;"><i class="fas fa-credit-card-alt fs-4"></i></button>
                                    <br />
                                    <span>Details</span>
                                </div>
                                <div class="col">
                                    <button class="btn btn-icon btn-secondary mb-2" runat="server" id="btnInfo" onserverclick="btnInfo_ServerClick"><i class="fas fa-pencil fs-4"></i></button>
                                    <br />
                                    <span>Info</span>
                                </div>
                            </div>
                            <!-- Classic Card -->
                            <div class="card card-dashed mb-10">
                                <div class="p-2">
                                    <!--begin::Container-->
                                    <div class=" w-100 p-2">
                                        <!--begin::Section-->
                                        <div>
                                            <!--begin::Item-->
                                            <div class="d-flex flex-stack pb-2 pt-2">
                                                <!--begin::Accountname-->
                                                <div class="fw-semibold pe-10 text-gray-600 fs-4">Balance</div>
                                                <!--end::Accountname-->
                                                <!--begin::Label-->
                                                <div class="text-end fw-bold fs-4 text-gray-800"><span runat="server" id="lblCardBalance">$</span></div>
                                                <!--end::Label-->
                                            </div>
                                        </div>
                                    </div>
                                </div>
                            </div>
                            
                            <div class="card card-dashed mb-10" runat="server" id="viewdetails" visible="false">
                                <div class="p-2">
                                    <!--begin::Container-->
                                    <div class="w-100 p-2">
                                        <div class="d-flex flex-stack border-bottom border-gray-300 mb-3 pb-3">
                                            <span class="card-label fw-bold text-gray-800 fs-4">Details</span>
                                        </div>
                                        <!--begin::Section-->
                                        <div>
                                            <!--begin::Item-->
                                            <div class="d-flex flex-stack pb-2 pt-2">
                                                <!--begin::Accountname-->
                                                <div class="fw-semibold pe-10 text-gray-600 fs-6">Card Number</div>
                                                <!--end::Accountname-->
                                                <!--begin::Label-->
                                                <div class="text-end fw-bold fs-6 text-gray-800">
                                                    <span runat="server" id="lblCardNumber">0000 0000 0000 0000</span>
                                                    &nbsp;
                                                    <button class="btn btn-icon btn-secondary btn-sm mb-2" runat="server" id="Button1" onclick="copyCardNumber();"><i class="fas fa-paste fs-4"></i></button>
                                                </div>
                                                <!--end::Label-->
                                            </div>
                                            <!--end::Item-->
                                            <!--begin::Item-->
                                            <div class="d-flex flex-stack pb-2 pt-2 border-top border-gray-300">
                                                <!--begin::Accountname-->
                                                <div class="fw-semibold pe-10 text-gray-600 fs-6">CVV</div>
                                                <!--end::Accountname-->
                                                <!--begin::Label-->
                                                <div class="text-end fw-bold fs-6 text-gray-800">
                                                    <span runat="server" id="lblCVV">000</span>
                                                    &nbsp;
                                                    <button class="btn btn-icon btn-secondary btn-sm mb-2" runat="server" id="Button2" onclick="copyCVV();"><i class="fas fa-paste fs-4"></i></button>
                                                </div>
                                                <!--end::Label-->
                                            </div>
                                            <!--end::Item-->
                                            <!--begin::Item-->
                                            <div class="d-flex flex-stack pb-2 pt-2 border-top border-gray-300">
                                                <!--begin::Accountname-->
                                                <div class="fw-semibold pe-10 text-gray-600 fs-6">Exp Date</div>
                                                <!--end::Accountname-->
                                                <!--begin::Label-->
                                                <div class="text-end fw-bold fs-6 text-gray-800">
                                                    <span runat="server" id="lblExpDate">00/00</span>
                                                    &nbsp;
                                                    <button class="btn btn-icon btn-secondary btn-sm mb-2" runat="server" id="Button3" onclick="copyExpDate();"><i class="fas fa-paste fs-4"></i></button>
                                                </div>
                                                <!--end::Label-->
                                            </div>
                                            <!--end::Item-->

                                        </div>
                                    </div>
                                </div>
                            </div>
                            <div class="card card-dashed mb-10" runat="server" id="viewholder" visible="false">
                                <div class="p-2">
                                    <!--begin::Container-->
                                    <div class="w-100 p-2">
                                        <div class="d-flex flex-stack border-bottom border-gray-300 mb-3 pb-3">
                                            <span class="card-label fw-bold text-gray-800 fs-4">Cardholder</span>
                                        </div>
                                        <!--begin::Section-->
                                        <div>
                                            <!--begin::Item-->
                                            <div class="d-flex flex-stack pb-2 pt-2">
                                                <!--begin::Accountname-->
                                                <div class="fw-semibold pe-10 text-gray-600 fs-6">Cardholder</div>
                                                <!--end::Accountname-->
                                                <!--begin::Label-->
                                                <div class="text-end fw-bold fs-6 text-gray-800">
                                                    <span runat="server" id="lblCardholder">-</span>
                                                    &nbsp;
                                                    <button class="btn btn-icon btn-secondary btn-sm mb-2" runat="server" id="Button4" onclick="copyCardholder();"><i class="fas fa-paste fs-4"></i></button>
                                                </div>
                                                <!--end::Label-->
                                            </div>
                                            <!--end::Item-->
                                            <!--begin::Item-->
                                            <div class="d-flex flex-stack pb-2 pt-2 border-top border-gray-300">
                                                <!--begin::Accountname-->
                                                <div class="fw-semibold pe-10 text-gray-600 fs-6">Phone</div>
                                                <!--end::Accountname-->
                                                <!--begin::Label-->
                                                <div class="text-end fw-bold fs-6 text-gray-800">
                                                    <span runat="server" id="lblPhone">000</span>
                                                    &nbsp;
                                                    <button class="btn btn-icon btn-secondary btn-sm mb-2" runat="server" id="Button5" onclick="copyPhone();"><i class="fas fa-paste fs-4"></i></button>
                                                </div>
                                                <!--end::Label-->
                                            </div>
                                            <!--end::Item-->
                                            <!--begin::Item-->
                                            <div class="d-flex flex-stack pb-2 pt-2 border-top border-gray-300">
                                                <!--begin::Accountname-->
                                                <div class="fw-semibold pe-10 text-gray-600 fs-6">Email</div>
                                                <!--end::Accountname-->
                                                <!--begin::Label-->
                                                <div class="text-end fw-bold fs-6 text-gray-800">
                                                    <span runat="server" id="lblEmail">000</span>
                                                    &nbsp;
                                                    <button class="btn btn-icon btn-secondary btn-sm mb-2" runat="server" id="Button11" onclick="copyEmail();"><i class="fas fa-paste fs-4"></i></button>
                                                </div>
                                                <!--end::Label-->
                                            </div>
                                            <!--end::Item-->
                                            <!--begin::Item-->
                                            <div class="d-flex flex-stack pb-2 pt-2 border-top border-gray-300">
                                                <!--begin::Accountname-->
                                                <div class="fw-semibold pe-10 text-gray-600 fs-6">Address</div>
                                                <!--end::Accountname-->
                                                <!--begin::Label-->
                                                <div class="text-end fw-bold fs-6 text-gray-800">
                                                    <span runat="server" id="lblAddress">000</span>
                                                    &nbsp;
                                                    <button class="btn btn-icon btn-secondary btn-sm mb-2" runat="server" id="Button10" onclick="copyAddress();"><i class="fas fa-paste fs-4"></i></button>
                                                </div>
                                                <!--end::Label-->
                                            </div>
                                            <!--end::Item-->
                                            <!--begin::Item-->
                                            <div class="d-flex flex-stack pb-2 pt-2 border-top border-gray-300">
                                                <!--begin::Accountname-->
                                                <div class="fw-semibold pe-10 text-gray-600 fs-6">City</div>
                                                <!--end::Accountname-->
                                                <!--begin::Label-->
                                                <div class="text-end fw-bold fs-6 text-gray-800">
                                                    <span runat="server" id="lblCity">00/00</span>
                                                    &nbsp;
                                                    <button class="btn btn-icon btn-secondary btn-sm mb-2" runat="server" id="Button6" onclick="copyCity();"><i class="fas fa-paste fs-4"></i></button>
                                                </div>
                                                <!--end::Label-->
                                            </div>
                                            <!--end::Item-->
                                            <!--begin::Item-->
                                            <div class="d-flex flex-stack pb-2 pt-2 border-top border-gray-300">
                                                <!--begin::Accountname-->
                                                <div class="fw-semibold pe-10 text-gray-600 fs-6">State</div>
                                                <!--end::Accountname-->
                                                <!--begin::Label-->
                                                <div class="text-end fw-bold fs-6 text-gray-800">
                                                    <span runat="server" id="lblState">00/00</span>
                                                    &nbsp;
                                                    <button class="btn btn-icon btn-secondary btn-sm mb-2" runat="server" id="Button7" onclick="copyState();"><i class="fas fa-paste fs-4"></i></button>
                                                </div>
                                                <!--end::Label-->
                                            </div>
                                            <!--end::Item-->
                                            <!--begin::Item-->
                                            <div class="d-flex flex-stack pb-2 pt-2 border-top border-gray-300">
                                                <!--begin::Accountname-->
                                                <div class="fw-semibold pe-10 text-gray-600 fs-6">Country</div>
                                                <!--end::Accountname-->
                                                <!--begin::Label-->
                                                <div class="text-end fw-bold fs-6 text-gray-800">
                                                    <span runat="server" id="lblCountry">00/00</span>
                                                    &nbsp;
                                                    <button class="btn btn-icon btn-secondary btn-sm mb-2" runat="server" id="Button8" onclick="copyCountry();"><i class="fas fa-paste fs-4"></i></button>
                                                </div>
                                                <!--end::Label-->
                                            </div>
                                            <!--end::Item-->
                                            <!--begin::Item-->
                                            <div class="d-flex flex-stack pb-2 pt-2 border-top border-gray-300">
                                                <!--begin::Accountname-->
                                                <div class="fw-semibold pe-10 text-gray-600 fs-6">Zip Code</div>
                                                <!--end::Accountname-->
                                                <!--begin::Label-->
                                                <div class="text-end fw-bold fs-6 text-gray-800">
                                                    <span runat="server" id="lblZipCode">00/00</span>
                                                    &nbsp;
                                                    <button class="btn btn-icon btn-secondary btn-sm mb-2" runat="server" id="Button9" onclick="copyZipCode();"><i class="fas fa-paste fs-4"></i></button>
                                                </div>
                                                <!--end::Label-->
                                            </div>
                                            <!--end::Item-->

                                        </div>
                                    </div>
                                </div>
                            </div>
                        </div>
                        <!--end::Card body-->
                    </div>
                    <!--end::Card-->
                </div>
                <!--end::Content-->
                <!--begin::Sidebar-->
                <div class="col-xs-12 col-sm-12 col-md-12 col-lg-6 col-xl-6">
                    <%-- d-none d-lg-block--%>
                    <!--begin::Col-->
                    <div class="col-xl-12">
                        <!--begin::List widget 23-->
                        <div class="card card-flush h-xl-100">
                            <!--begin::Header-->
                            <%--<div class="card-header pt-7">
                                <h3 class="card-title align-items-start flex-column">
                                    
                                </h3>
                                <div class="card-toolbar"></div>
                            </div>--%>
                            <!--end::Header-->
                            <!--begin::Body-->
                            <div class="card-body pt-5 fs-4">
                                <div class="mb-10">
                                    <ul class="nav nav-tabs nav-line-tabs nav-line-tabs-2x mb-5 fs-6">
                                        <li class="nav-item">
                                            <a class="nav-link active" data-bs-toggle="tab" href="#kt_tab_pane_4">Transaction</a>
                                        </li>
                                        <li class="nav-item">
                                            <a class="nav-link" data-bs-toggle="tab" href="#kt_tab_pane_5">Deposit</a>
                                        </li>
                                    </ul>

                                </div>
                                <div class="tab-content" id="myTabContent">
                                    <div class="tab-pane fade show active" id="kt_tab_pane_4" role="tabpanel">
                                        <div class="align-content-center" runat="server" id="divnotrx">
                                            <center>
                                                <asp:Label runat="server" ID="lblNoRow" Text="No transaction at the moment." CssClass="fs-6" />
                                            </center>
                                        </div>
                                        <asp:GridView CssClass="table table-borderless gs-7" ID="gvTrxList" HeaderStyle-CssClass="header-table" runat="server" ShowHeader="false" AutoGenerateColumns="false" DataKeyNames="ID" AllowPaging="True" PageSize="50" AllowCustomPaging="False">
                                            <PagerStyle HorizontalAlign="Center" CssClass="bs4-aspnet-pager" />
                                            <Columns>
                                                <asp:TemplateField HeaderText="1" ItemStyle-VerticalAlign="Top" ItemStyle-HorizontalAlign="Left">
                                                    <ItemTemplate>
                                                        <asp:HiddenField runat="server" ID="hfID" Value='<%# Eval("ID") %>' />
                                                        <span class="form-label fs-3 fw-bold"><%# Eval("MerchantName") %></span>
                                                        <br />
                                                        <span class="form-label fs-7 fw-semibold"><%# Eval("TransactionTime") %></span>
                                                    </ItemTemplate>
                                                </asp:TemplateField>
                                                <asp:TemplateField HeaderText="2" ItemStyle-VerticalAlign="Top" ItemStyle-HorizontalAlign="Left">
                                                    <ItemTemplate>
                                                        <div class="row">
                                                            <div class="col d-flex flex-grow-1 justify-content-end align-items-center">
                                                                <%--<span class="badge badge-light-warning" runat="server" visible='<%# IsStatusCreated((string)Eval("Status")) %>'>Pending Payment</span>
                                                                <span class="badge badge-light-danger" runat="server" visible='<%# IsStatusExpired((string)Eval("Status")) %>'>Expired</span>
                                                                <span class="badge badge-light-danger" runat="server" visible='<%# IsStatusFailed((string)Eval("Status")) %>'>Failed</span>
                                                                <span class="badge badge-light-danger" runat="server" visible='<%# IsStatusCancelled((string)Eval("Status")) %>'>Cancelled</span>
                                                                <span class="badge badge-light-success" runat="server" visible='<%# IsStatusCompleted((string)Eval("Status")) %>'>Completed</span>
                                                                <span class="badge badge-light-info" runat="server" visible='<%# IsStatusInProgress((string)Eval("Status")) %>'>In Progress</span>--%>

                                                                <span class="badge badge-light-info fs-3 fw-bold"><%# Eval("AuthorizedAmount") %> USD</span>
                                                            </div>
                                                        </div>
                                                    </ItemTemplate>
                                                </asp:TemplateField>

                                            </Columns>
                                        </asp:GridView>
                                    </div>
                                    <div class="tab-pane fade" id="kt_tab_pane_5" role="tabpanel">
                                        <div class="align-content-center" runat="server" id="divnodepo">
                                            <center>
                                                <asp:Label runat="server" ID="Label2" Text="No deposit at the moment." CssClass="fs-6" />
                                            </center>
                                        </div>
                                        <asp:GridView CssClass="table table-borderless gs-7" ID="gvDepositList" HeaderStyle-CssClass="header-table" runat="server" ShowHeader="false" AutoGenerateColumns="false" DataKeyNames="ID" AllowPaging="True" PageSize="50" AllowCustomPaging="False" OnRowDataBound="OnRowDataBound">
                                            <PagerStyle HorizontalAlign="Center" CssClass="bs4-aspnet-pager" />
                                            <Columns>
                                                <asp:TemplateField HeaderText="1" ItemStyle-VerticalAlign="Top" ItemStyle-HorizontalAlign="Left">
                                                    <ItemTemplate>
                                                        <asp:HiddenField runat="server" ID="hfID" Value='<%# Eval("ID") %>' />
                                                        <span class="form-label fs-3 fw-bold"><%# Eval("Total") %> USD</span>
                                                        <br />
                                                        <span class="form-label fs-7 fw-semibold"><%# Eval("DateTransaction") %></span>
                                                        <%--<div class="row">
                                                            <div class="col-md-3 col-sm-3 col-xs-12">
                                                                <span>Total : <%# Eval("Total") %></span>
                                                            </div>
                                                            <div class="col-md-9 col-sm-9 col-xs-12 d-flex flex-grow-1 justify-content-start align-items-center">
                                                                <span> Deposit received : <%# Eval("Amount") %></span>
                                                            </div>
                                                        </div>--%>
                                                    </ItemTemplate>
                                                </asp:TemplateField>
                                                <asp:TemplateField HeaderText="2" ItemStyle-VerticalAlign="Top" ItemStyle-HorizontalAlign="Left">
                                                    <ItemTemplate>
                                                        <div class="row">
                                                            <div class="col d-flex flex-grow-1 justify-content-end align-items-center">
                                                                <span class="badge badge-light-warning" runat="server" visible='<%# IsStatusCreated((string)Eval("Status")) %>'>Pending Payment</span>
                                                                <span class="badge badge-light-danger" runat="server" visible='<%# IsStatusExpired((string)Eval("Status")) %>'>Expired</span>
                                                                <span class="badge badge-light-danger" runat="server" visible='<%# IsStatusFailed((string)Eval("Status")) %>'>Failed</span>
                                                                <span class="badge badge-light-danger" runat="server" visible='<%# IsStatusCancelled((string)Eval("Status")) %>'>Cancelled</span>
                                                                <span class="badge badge-light-success" runat="server" visible='<%# IsStatusCompleted((string)Eval("Status")) %>'>Completed</span>
                                                                <span class="badge badge-light-info" runat="server" visible='<%# IsStatusInProgress((string)Eval("Status")) %>'>In Progress</span>
                                                            </div>
                                                        </div>
                                                    </ItemTemplate>
                                                </asp:TemplateField>
                                                
                                            </Columns>
                                        </asp:GridView>


                                        <!--end::Table-->
                                    </div>
                                </div>
                                <!--end: Card Body-->
                            </div>
                            <!--end::List widget 23-->
                        </div>
                        <!--end::Col-->
                    </div>
                    <!--end::Card-->
                </div>
                <!--end::Sidebar-->
            </div>
            <!--end::Content-->
        </div>
    </div>
</asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderID="DrawerContent" runat="server">
</asp:Content>
<asp:Content ID="Content3" ContentPlaceHolderID="ModalContent" runat="server">
    <div class="modal fade" tabindex="-1" id="infoModal" aria-hidden="true">
        <div class="modal-dialog">
            <div class="modal-content">
                <div class="modal-header">
                    <h3 class="modal-title">Card Info</h3>

                    <!--begin::Close-->
                    <div class="btn btn-icon btn-sm btn-active-light-primary ms-2" data-bs-dismiss="modal" aria-label="Close">
                        <i class="ki-duotone ki-cross fs-1"><span class="path1"></span><span class="path2"></span></i>
                    </div>
                    <!--end::Close-->
                </div>

                <div class="modal-body">
                    <!--begin::Items-->
                    <div class="mb-5">
                        <span class="card-label fw-bold text-gray-800">Supported Usage Scenarios</span>
                        <br />
                        <span class="text-gray-500 mt-1 fw-semibold fs-6" runat="server" id="lblUsage"></span>
                    </div>
                    <!--end::Items-->
                    <!--begin::Items-->
                    <div class="mb-5">
                        <span class="card-label fw-bold text-gray-800">Fees</span>
                        <br />
                        <span class="text-gray-500 mt-1 fw-semibold fs-6" runat="server" id="lblDepositFee"></span>
                    </div>
                    <!--end::Items-->
                    <!--begin::Items-->
                    <div class="mb-5">
                        <span class="card-label fw-bold text-gray-800">Credit Limits</span>
                        <br />
                        <ol type="1">
                            <li class="text-gray-500 mt-1 fw-semibold fs-6"><span runat="server" id="lblMinDeposit"></span></li>
                            <li class="text-gray-500 mt-1 fw-semibold fs-6"><span runat="server" id="lblMaxDeposit"></span></li>
                        </ol>

                    </div>
                    <!--end::Items-->
                    <!--begin::Items-->
                    <div class="mb-5">
                        <span class="card-label fw-bold text-gray-800">Card Usage Notice</span>
                        <br />
                        <p>
                            <ol type="1">
                                <li class="text-gray-500 mt-1 fw-semibold fs-6">If the card issuer detects malicious activities such as bulk refunds, cancellations, failures, or chargebacks during card usage, the card will be automatically frozen, and a fee of 10 USD per occurrence will be deducted.</li>
                                <li class="text-gray-500 mt-1 fw-semibold fs-6">If the overall transaction failure rate exceeds 20%, the card will be automatically frozen.</li>
                                <li class="text-gray-500 mt-1 fw-semibold fs-6">If the card has insufficient balance and accumulates up to 3 consecutive authorization failures, the card will be automatically canceled.</li>
                                <li class="text-gray-500 mt-1 fw-semibold fs-6">If the card is frozen, please contact customer service to apply for unfreezing. </li>
                                <li class="text-gray-500 mt-1 fw-semibold fs-6">The card is valid for 3 years.</li>
                            </ol>
                        </p>
                    </div>
                    <!--end::Items-->
                </div>

                <div class="modal-footer">
                    <button class="btn btn-danger" type="button" data-bs-dismiss="modal">Close</button>
                </div>
            </div>
        </div>
    </div>

    
    <div class="modal fade" tabindex="-1" id="rechargeModal" aria-hidden="true">
        <div class="modal-dialog">
            <div class="modal-content">
                <div class="modal-header">
                    <h3 class="modal-title">Deposit Card</h3>

                    <!--begin::Close-->
                    <div class="btn btn-icon btn-sm btn-active-light-primary ms-2" data-bs-dismiss="modal" aria-label="Close">
                        <i class="ki-duotone ki-cross fs-1"><span class="path1"></span><span class="path2"></span></i>
                    </div>
                    <!--end::Close-->
                </div>

                <div class="modal-body">
                    <div class="alert alert-dismissible bg-light-danger d-flex flex-column flex-sm-row p-5 mb-10" runat="server" id="divfaileddeposit" visible="false">
                        <i class="ki-duotone ki-notification-bing fs-2hx text-danger me-4 mb-5 mb-sm-0"><span class="path1"></span><span class="path2"></span><span class="path3"></span></i>
                        <div class="d-flex flex-column pe-0 pe-sm-10">
                            <h4 class="fw-semibold">Failed</h4>
                            <span>
                                <span runat="server" id="lblfaileddeposit">Error message</span></span>
                        </div>
                        <button type="button" class="position-absolute position-sm-relative m-2 m-sm-0 top-0 end-0 btn btn-icon ms-sm-auto" runat="server" id="btndfaileddeposit" onserverclick="btndfaileddeposit_ServerClick">
                            <i class="ki-duotone ki-cross fs-1 text-danger"><span class="path1"></span><span class="path2"></span></i>
                        </button>
                    </div>


                    <div class="form-label mb-3">Deposit amount</div>
                    <div class="input-group mb-5">
                        <span class="input-group-text">USD</span>
                        <asp:TextBox class="form-control" runat="server" id="txtDepositAmount" aria-label="Amount (to the nearest dollar)" TextMode="Number" AutoPostBack="true" OnTextChanged="txtDepositAmount_TextChanged"  />
                        <%--<span class="input-group-text">.00</span>--%>
                    </div>
                    <div class="row mb-10">
                        <div class="col" runat="server" id="viewrc20">
                            <button class="btn btn-light-primary w-100 mb-3" id="rc20" runat="server" onserverclick="rc20_ServerClick">$20</button>
                        </div>
                        <div class="col" runat="server" id="viewrc30">
                            <button class="btn btn-light-primary w-100 mb-3" id="rc30" runat="server" onserverclick="rc30_ServerClick">$30</button>
                        </div>
                        <div class="col">
                            <button class="btn btn-light-primary w-100 mb-3" id="rc50" runat="server" onserverclick="rc50_ServerClick">$50</button>
                        </div>
                        <div class="col">
                            <button class="btn btn-light-primary w-100 mb-3" id="rc100" runat="server" onserverclick="rc100_ServerClick">$100</button>
                        </div>
                        <div class="col">
                            <button class="btn btn-light-primary w-100 mb-3" id="rc200" runat="server" onserverclick="rc200_ServerClick">$200</button>
                        </div>
                    </div>

                    <div runat="server" id="viewnetwork" visible="true" class="mb-10">
                        <h6 class="form-label">Crypto Transfer</h6>
                        <div class="mb-6">
                            <%--<div class="form-label">Network :</div>--%>
                            <div class="position-relative d-flex align-items-center">
                                <asp:DropDownList runat="server" ID="ddlPayment" placeholder="Select currency" CssClass="form-control form-select-solid">
                                    <asp:ListItem Value="TRC20" Text="USDT (TRC20)" />
                                </asp:DropDownList>
                                <i class="ki-duotone ki-down fs-4 position-absolute end-0 ms-4" style="margin-right: 10px"></i>
                            </div>
                        </div>
                    </div>

                    <div class="card card-dashed mb-5">
                        <div class="p-2">
                            <!--begin::Container-->
                            <div class=" w-100 p-2">
                                <!--begin::Section-->
                                <div>
                                    <!--begin::Item-->
                                    <div class="d-flex flex-stack pb-2 pt-2 mb-2">
                                        <!--begin::Accountnumber-->
                                        <div class="fw-bold pe-10 fs-4">You need to pay</div>
                                        <!--end::Accountnumber-->
                                        <!--begin::Number-->
                                        <div class="text-end fw-bold fs-4 text-gray-800"><span runat="server" id="lblTotal">0 USD</span></div>
                                        <!--end::Number-->
                                    </div>
                                    <!--end::Item-->
                                    <!--begin::Item-->
                                    <div class="d-flex flex-stack pb-2 pt-2 border-top border-gray-300">
                                        <!--begin::Code-->
                                        <div class="fw-semibold pe-10 text-gray-600 fs-7 mt-3">you will deposit</div>
                                        <!--end::Code-->
                                        <!--begin::Label-->
                                        <div class="text-end fw-bold fs-7 text-gray-800 mt-3"><span runat="server" id="lblDepositAmount">0 USD</span></div>
                                        <!--end::Label-->
                                    </div>
                                    <!--end::Item-->
                                    <!--begin::Item-->
                                    <div class="d-flex flex-stack">
                                        <!--begin::Code-->
                                        <div class="fw-semibold pe-10 text-gray-600 fs-7">Deposit Fee</div>
                                        <!--end::Code-->
                                        <!--begin::Label-->
                                        <div class="text-end fw-bold fs-7 text-gray-800"><span runat="server" id="lblFee">0 USD</span></div>
                                        <!--end::Label-->
                                    </div>
                                    <!--end::Item-->
                                    
                                </div>
                                <!--end::Section-->
                            </div>
                            <!--end::Container-->

                        </div>
                    </div>
                </div>

                <div class="modal-footer">
                    <asp:Button CssClass="btn btn-primary" runat="server" ID="btnDepositConfirm" Text="Confirm Deposit" OnClick="btnDepositConfirm_Click" OnClientClick="this.disabled=true;" UseSubmitBehavior="false" />
                    <button class="btn btn-danger" type="button" data-bs-dismiss="modal">Close</button>
                </div>
            </div>
        </div>
    </div>

    <div class="modal fade" tabindex="-1" id="myModalDetail" aria-hidden="true" data-backdrop="static">
    <div class="modal-dialog">
        <div class="modal-content">
            <div class="modal-header">
                <h3 class="modal-title">Detail Card</h3>
                <div class="btn btn-icon btn-sm btn-active-light-primary ms-2" data-bs-dismiss="modal" aria-label="Close">
                    <i class="ki-duotone ki-cross fs-1"><span class="path1"></span><span class="path2"></span></i>
                </div>
            </div>

            <div class="modal-body">
                <div style="margin-bottom: 10px">
                    <div class="alert alert-dismissible bg-light-danger d-flex flex-column flex-sm-row p-5 mb-10" runat="server" id="divfaileddetail" visible="false">
                        <i class="ki-duotone ki-notification-bing fs-2hx text-danger me-4 mb-5 mb-sm-0"><span class="path1"></span><span class="path2"></span><span class="path3"></span></i>
                        <div class="d-flex flex-column pe-0 pe-sm-10">
                            <h4 class="fw-semibold">Failed</h4>
                            <span>
                                <asp:Label runat="server" ID="lblErrorDetail" Text="Error message" /></span>
                        </div>
                        <button type="button" class="position-absolute position-sm-relative m-2 m-sm-0 top-0 end-0 btn btn-icon ms-sm-auto" runat="server" id="btnfaileddetail" onserverclick="btnfaileddetail_ServerClick">
                            <i class="ki-duotone ki-cross fs-1 text-danger"><span class="path1"></span><span class="path2"></span></i>
                        </button>
                    </div>
                    <div class="text-muted text-center fw-semibold fs-5 mb-5">To view card detail, enter the verification code we sent to your email.</div>
                    <div class="fw-bold text-center text-gray-900 fs-6 mb-1 ms-1">Type your 6 digit security code</div>
                    <br />
                    <div class="d-flex flex-wrap flex-stack" style="margin-left: 30px; margin-right: 30px; justify-content: center;">
                        <input type="text" runat="server" id="icode1" name="code_2" data-inputmask="'mask': '9', 'placeholder': ''" maxlength="1" class="form-control bg-transparent h-40px w-40px fs-2 text-center mx-1 my-2" value="" />
                        <input type="text" runat="server" id="icode2" name="code_2" data-inputmask="'mask': '9', 'placeholder': ''" maxlength="1" class="form-control bg-transparent h-40px w-40px fs-2 text-center mx-1 my-2" value="" />
                        <input type="text" runat="server" id="icode3" name="code_3" data-inputmask="'mask': '9', 'placeholder': ''" maxlength="1" class="form-control bg-transparent h-40px w-40px fs-2 text-center mx-1 my-2" value="" />
                        <input type="text" runat="server" id="icode4" name="code_4" data-inputmask="'mask': '9', 'placeholder': ''" maxlength="1" class="form-control bg-transparent h-40px w-40px fs-2 text-center mx-1 my-2" value="" />
                        <input type="text" runat="server" id="icode5" name="code_5" data-inputmask="'mask': '9', 'placeholder': ''" maxlength="1" class="form-control bg-transparent h-40px w-40px fs-2 text-center mx-1 my-2" value="" />
                        <input type="text" runat="server" id="icode6" name="code_6" data-inputmask="'mask': '9', 'placeholder': ''" maxlength="1" class="form-control bg-transparent h-40px w-40px fs-2 text-center mx-1 my-2" value="" />
                    </div>
                </div>
            </div>
            <div class="modal-footer">
                <button class="btn btn-danger" type="button" data-bs-dismiss="modal">Close</button>
                <asp:Button CssClass="btn btn-success" runat="server" ID="btnDetailX" Text="View Detail" OnClick="btnDetailX_Click" OnClientClick="this.disabled=true;" UseSubmitBehavior="false" />
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
                    <label class="form-label" runat="server" id="Label1"></label>
                </div>

                <div class="modal-footer">
                    <button class="btn btn-danger" type="button" data-bs-dismiss="modal">Close</button>
                </div>
            </div>
        </div>
    </div>
</asp:Content>
<asp:Content ID="Content4" ContentPlaceHolderID="ScriptContent" runat="server">
    
    <script type="text/javascript">
        $(document).ready(function () {
            $('input[type="text"]').bind('paste', function (e) {
                e.preventDefault();
                var text = e.originalEvent.clipboardData.getData('text');
                var textArray = text.split('');

                $('input[type="text"]').each(function (index, element) {
                    $(element).val(textArray[index]);
                });
            });
        });

        $(window).load(function () {
            var n = document.getElementById('<%=icode1.ClientID%>');
            var i = document.getElementById('<%=icode2.ClientID%>');
            var o = document.getElementById('<%=icode3.ClientID%>');
            var u = document.getElementById('<%=icode4.ClientID%>');
            var r = document.getElementById('<%=icode5.ClientID%>');
            var c = document.getElementById('<%=icode6.ClientID%>');
            console.log(n);
            //n.focus(),
            n.addEventListener("keyup", (function () {
                1 === this.value.length && i.focus()
            }
            )),
                i.addEventListener("keyup", (function () {
                    1 === this.value.length && o.focus()
                }
                )),
                o.addEventListener("keyup", (function () {
                    1 === this.value.length && u.focus()
                }
                )),
                u.addEventListener("keyup", (function () {
                    1 === this.value.length && r.focus()
                }
                )),
                r.addEventListener("keyup", (function () {
                    1 === this.value.length && c.focus()
                }
                )),
                c.addEventListener("keyup", (function () {
                    1 === this.value.length && c.blur()
                }
                ))
        });

    </script>
    <script type="text/javascript">

        <%--document.getElementById('rc20').addEventListener('click', function () {
            var x = document.getElementById("<%= txtRechargeAmount.ClientID %>");
            x.value = '20';
            $('#rechargeModal').modal('show');
            window.isModalRecharge = true;
        });--%>

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
            $('#infoModal').modal({ backdrop: 'static', keyboard: false });
            if (window.isModalInfo) {
                $('#infoModal').modal('show');
            }
            else {
                $('#infoModal').modal('hide');
            }
            console.log(window.isModalInfo);
            console.log('this is another debugging message');
        });

        $(window).load(function () {
            $('#rechargeModal').modal({ backdrop: 'static', keyboard: false });
            if (window.isModalRecharge) {
                $('#rechargeModal').modal('show');
            }
            else {
                $('#rechargeModal').modal('hide');
            }
            console.log(window.isModalRecharge);
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

        <%--function copyReferralCode() {
            var copyText = document.getElementById("<%= hfReferralCode.ClientID %>");
            //copyText.select();
            //document.execCommand("copy");
            navigator.clipboard.writeText(copyText.value).then(() => { }).catch((error) => { })
            console.log("copytext =>" + copyText.value);
            alert("Copied to clipboard: " + copyText.value);
        }--%>
        function copyCardNumber() {
            var copyText = document.getElementById("<%= hfCardNumber.ClientID %>");
            //copyText.select();
            //document.execCommand("copy");
            navigator.clipboard.writeText(copyText.value).then(() => { }).catch((error) => { })
            console.log("copytext =>" + copyText.value);
            alert("Copied to clipboard: " + copyText.value);
        }
        function copyCVV() {
            var copyText = document.getElementById("<%= hfCVVDecr.ClientID %>");
            //copyText.select();
            //document.execCommand("copy");
            navigator.clipboard.writeText(copyText.value).then(() => { }).catch((error) => { })
            console.log("copytext =>" + copyText.value);
            alert("Copied to clipboard: " + copyText.value);
        }
        function copyExpDate() {
            var copyText = document.getElementById("<%= hfExpDateDecr.ClientID %>");
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




        function copyCardholder() {
            var copyText = document.getElementById("<%= hfCardholder.ClientID %>");
            //copyText.select();
            //document.execCommand("copy");
            navigator.clipboard.writeText(copyText.value).then(() => { }).catch((error) => { })
            console.log("copytext =>" + copyText.value);
            alert("Copied to clipboard: " + copyText.value);
        }

        function copyPhone() {
            var copyText = document.getElementById("<%= hfPhone.ClientID %>");
            //copyText.select();
            //document.execCommand("copy");
            navigator.clipboard.writeText(copyText.value).then(() => { }).catch((error) => { })
            console.log("copytext =>" + copyText.value);
            alert("Copied to clipboard: " + copyText.value);
        }

        function copyEmail() {
            var copyText = document.getElementById("<%= hfEmail.ClientID %>");
            //copyText.select();
            //document.execCommand("copy");
            navigator.clipboard.writeText(copyText.value).then(() => { }).catch((error) => { })
            console.log("copytext =>" + copyText.value);
            alert("Copied to clipboard: " + copyText.value);
        }

        function copyAddress() {
            var copyText = document.getElementById("<%= hfAddress.ClientID %>");
            //copyText.select();
            //document.execCommand("copy");
            navigator.clipboard.writeText(copyText.value).then(() => { }).catch((error) => { })
            console.log("copytext =>" + copyText.value);
            alert("Copied to clipboard: " + copyText.value);
        }

        function copyCity() {
            var copyText = document.getElementById("<%= hfCity.ClientID %>");
            //copyText.select();
            //document.execCommand("copy");
            navigator.clipboard.writeText(copyText.value).then(() => { }).catch((error) => { })
            console.log("copytext =>" + copyText.value);
            alert("Copied to clipboard: " + copyText.value);
        }

        function copyState() {
            var copyText = document.getElementById("<%= hfState.ClientID %>");
            //copyText.select();
            //document.execCommand("copy");
            navigator.clipboard.writeText(copyText.value).then(() => { }).catch((error) => { })
            console.log("copytext =>" + copyText.value);
            alert("Copied to clipboard: " + copyText.value);
        }

        function copyCountry() {
            var copyText = document.getElementById("<%= hfCountry.ClientID %>");
            //copyText.select();
            //document.execCommand("copy");
            navigator.clipboard.writeText(copyText.value).then(() => { }).catch((error) => { })
            console.log("copytext =>" + copyText.value);
            alert("Copied to clipboard: " + copyText.value);
        }

        function copyZipCode() {
            var copyText = document.getElementById("<%= hfZipCode.ClientID %>");
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
