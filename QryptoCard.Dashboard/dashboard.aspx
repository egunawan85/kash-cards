<%@ Page Title="Dashboard" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="dashboard.aspx.cs" Inherits="QryptoCard.Dashboard.dashboard" %>

<%@ MasterType VirtualPath="~/Site.Master" %>
<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" runat="server">
    <asp:HiddenField runat="server" ID="hfReferralCode" />
    <asp:HiddenField runat="server" ID="hfReferralLink" />
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
                            <span>
                                <span class="fw-light">Welcome back</span>
                                <%--,&nbsp;
                                <span runat="server" id="lblName" /></span>--%>
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
                    <a href='<%= ResolveUrl("~/card/cardlist")%>' class="btn btn-sm btn-success d-flex flex-center ms-3 px-4 py-3">
                        <i class="ki-outline ki-plus-square fs-2"></i>
                        <span>Buy Card</span>
                    </a>
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
            <!--begin::Row-->
            <div class="row g-5 g-xl-10 mb-5 mb-xl-0">
                <!--begin::Col-->
                <div class="col-md-4 mb-xl-10">
                    <!--begin::Card widget 28-->
                    <div class="card card-flush">
                        <!--begin::Header-->
                        <div class="card-header pt-7">
                            <!--begin::Card title-->
                            <div class="card-title flex-stack flex-row-fluid">
                                <!--begin::Symbol-->
                                <div class="symbol symbol-45px me-5">
                                    <span class="symbol-label bg-light-info">
                                        <i class="ki-outline ki-dollar fs-2x text-gray-800"></i>
                                    </span>
                                </div>
                                <!--end::Symbol-->
                                <!--begin::Wrapper-->
                                <div class="me-n2">
                                    <!--begin::Badge-->
                                    <%--<span class="badge badge-light-success align-self-center fs-base">
                                    <i class="ki-outline ki-arrow-up fs-5 text-success ms-n1"></i>2.2%</span>--%>
                                    <!--end::Badge-->
                                </div>
                                <!--end::Wrapper-->
                            </div>
                            <!--end::Header-->
                        </div>
                        <!--end::Card title-->
                        <!--begin::Card body-->
                        <div class="card-body d-flex align-items-end">
                            <!--begin::Wrapper-->
                            <div class="d-flex flex-column">
                                <span class="fw-bolder fs-2x text-gray-900" runat="server" id="lblCommissionRate">-</span>
                                <span class="fw-bold fs-7 text-gray-500">Commission Rate</span>
                            </div>
                            <!--end::Wrapper-->
                        </div>
                        <!--end::Card body-->
                    </div>
                    <!--end::Card widget 28-->
                </div>
                <!--end::Col-->
                <!--begin::Col-->
                <div class="col-md-4 mb-xl-10">
                    <!--begin::Card widget 28-->
                    <div class="card card-flush">
                        <!--begin::Header-->
                        <div class="card-header pt-7">
                            <!--begin::Card title-->
                            <div class="card-title flex-stack flex-row-fluid">
                                <!--begin::Symbol-->
                                <div class="symbol symbol-45px me-5">
                                    <span class="symbol-label bg-light-info">
                                        <i class="ki-outline ki-dollar fs-2x text-gray-800"></i>
                                    </span>
                                </div>
                                <!--end::Symbol-->
                                <!--begin::Wrapper-->
                                <div class="me-n2">
                                    <!--begin::Badge-->
                                    <%--<span class="badge badge-light-danger align-self-center fs-base">
                                    <i class="ki-outline ki-arrow-down fs-5 text-danger ms-n1"></i>2.5%</span>--%>
                                    <!--end::Badge-->
                                </div>
                                <!--end::Wrapper-->
                            </div>
                            <!--end::Header-->
                        </div>
                        <!--end::Card title-->
                        <!--begin::Card body-->
                        <div class="card-body d-flex align-items-end">
                            <!--begin::Wrapper-->
                            <div class="d-flex flex-column">
                                <span class="fw-bolder fs-2x text-gray-900" runat="server" id="lblTotalCommission">-</span>
                                <span class="fw-bold fs-7 text-gray-500">Total Commission</span>
                            </div>
                            <!--end::Wrapper-->
                        </div>
                        <!--end::Card body-->
                    </div>
                    <!--end::Card widget 28-->
                </div>
                <!--end::Col-->
                <!--begin::Col-->
                <div class="col-md-4 mb-xl-10">
                    <!--begin::Card widget 28-->
                    <div class="card card-flush">
                        <!--begin::Header-->
                        <div class="card-header pt-7">
                            <!--begin::Card title-->
                            <div class="card-title flex-stack flex-row-fluid">
                                <!--begin::Symbol-->
                                <div class="symbol symbol-45px me-5">
                                    <span class="symbol-label bg-light-info">
                                        <i class="ki-outline ki-credit-cart fs-2x text-gray-800"></i>
                                    </span>
                                </div>
                                <!--end::Symbol-->
                                <!--begin::Wrapper-->
                                <div class="me-n2">
                                    <!--begin::Badge-->
                                    <%--<span class="badge badge-light-success align-self-center fs-base">
                                    <i class="ki-outline ki-arrow-up fs-5 text-success ms-n1"></i>2.7%</span>--%>
                                    <!--end::Badge-->
                                </div>
                                <!--end::Wrapper-->
                            </div>
                            <!--end::Header-->
                        </div>
                        <!--end::Card title-->
                        <!--begin::Card body-->
                        <div class="card-body d-flex align-items-end">
                            <!--begin::Wrapper-->
                            <div class="d-flex flex-column">
                                <span class="fw-bolder fs-2x text-gray-900" runat="server" id="lblTotalCards">-</span>
                                <span class="fw-bold fs-7 text-gray-500">Total Cards</span>
                            </div>
                            <!--end::Wrapper-->
                        </div>
                        <!--end::Card body-->
                    </div>
                    <!--end::Card widget 28-->
                </div>
                <!--end::Col-->
            </div>
            <!--end::Row-->
            <!--begin::Row-->
            <div class="row g-5 g-xl-10 mb-5 mb-xl-10">
                <!--begin::Col-->
                <div class="col-xl-12">
                    <!--begin::List widget 23-->
                    <div class="card card-flush h-xl-100">
                        <!--begin::Header-->
                        <div class="card-header pt-7">
                            <!--begin::Title-->
                            <h3 class="card-title align-items-start flex-column">
                                <span class="card-label fw-bold text-gray-800">Referral Program</span>
                                <span class="text-gray-500 mt-1 fw-semibold fs-6">Invite friends to earn commission rewards.</span>
                            </h3>
                            <!--end::Title-->
                            <!--begin::Toolbar-->
                            <div class="card-toolbar"></div>
                            <!--end::Toolbar-->
                        </div>
                        <!--end::Header-->
                        <!--begin::Body-->
                        <div class="card-body pt-5">
                            <!--begin::Items-->
                            <div class="row">
                                <!--begin::Item-->
                                <div class="col">
                                    <div class="border border-gray-300 border-dashed rounded py-3 px-4 me-6 mb-3" style="width: 100%">
                                        <label class="form-label">Referral Code </label>
                                        <div class="input-group">
                                            <asp:TextBox runat="server" ID="txtReferralCode" CssClass="form-control" Enabled="false" />
                                            <button class="btn btn-icon btn-primary" data-bs-toggle="tooltip" data-bs-placement="top" title="Copy Code" runat="server" onclick="copyReferralCode();" id="btnCopyReferralCode"><i class="fas fa-clone fs-4"></i></button>
                                        </div>
                                    </div>
                                </div>
                                <div class="col">
                                    <div class="border border-gray-300 border-dashed rounded py-3 px-4 me-6 mb-3" style="width: 100%">
                                        <label class="form-label">Referral Link </label>
                                        <div class="input-group">
                                            <asp:TextBox runat="server" ID="txtReferralLink" CssClass="form-control" Enabled="false" />
                                            <button class="btn btn-icon btn-primary" data-bs-toggle="tooltip" data-bs-placement="top" title="Copy Link" runat="server" onclick="copyReferralLink();" id="btnCopyReferralLink"><i class="fas fa-clone fs-4"></i></button>
                                        </div>
                                    </div>
                                </div>
                                <!--end::Item-->
                            </div>
                            <!--end::Items-->
                        </div>
                        <!--end: Card Body-->
                    </div>
                    <!--end::List widget 23-->
                </div>
                <!--end::Col-->
            </div>
            <!--end::Row-->


            <!--begin::Row-->
            <div class="row g-5 g-xl-10 mb-5 mb-xl-10">
                <!--begin::Col-->
                <div class="col-xl-6">
                    <!--begin::List widget 23-->
                    <div class="card card-flush h-xl-100">
                        <!--begin::Header-->
                        <div class="card-header pt-7">
                            <!--begin::Title-->
                            <h3 class="card-title align-items-start flex-column">
                                <span class="card-label fw-bold text-gray-800">Referral History</span>
                                <%--<span class="text-gray-500 mt-1 fw-semibold fs-6">Invite friends to earn commission rewards.</span>--%>
                            </h3>
                            <!--end::Title-->
                            <!--begin::Toolbar-->
                            <div class="card-toolbar"></div>
                            <!--end::Toolbar-->
                        </div>
                        <!--end::Header-->
                        <!--begin::Body-->
                        <div class="card-body pt-5">
                            <!--begin::Items-->
                            <div class="align-content-center" runat="server" id="divnoreferral">
                                <center>
                                    <asp:Label runat="server" ID="Label2" Text="No user joined at the moment." CssClass="fs-6" />
                                </center>
                            </div>
                            <asp:GridView CssClass="table table-borderless gs-7" ID="gvReferralList" HeaderStyle-CssClass="header-table" runat="server" ShowHeader="false" AutoGenerateColumns="false" DataKeyNames="UserID" AllowPaging="True" PageSize="50" AllowCustomPaging="False">
                                <PagerStyle HorizontalAlign="Center" CssClass="bs4-aspnet-pager" />
                                <Columns>
                                    <asp:TemplateField HeaderText="1" ItemStyle-VerticalAlign="Top" ItemStyle-HorizontalAlign="Left">
                                        <ItemTemplate>
                                            <asp:HiddenField runat="server" ID="hfID" Value='<%# Eval("UserID") %>' />
                                            <span class="form-label fs-3 fw-bold"><%# Eval("FirstName") %> <%# Eval("LastName") %></span>
                                            <br />
                                            <span class="form-label fs-7 fw-semibold"><%# Eval("DateJoin") %></span>
                                            
                                        </ItemTemplate>
                                    </asp:TemplateField>
                                    
                                </Columns>
                            </asp:GridView>

                            <!--end::Items-->
                        </div>
                        <!--end: Card Body-->
                    </div>
                    <!--end::List widget 23-->
                </div>
                <!--end::Col-->
                <!--begin::Col-->
                <div class="col-xl-6">
                    <!--begin::List widget 23-->
                    <div class="card card-flush h-xl-100">
                        <!--begin::Header-->
                        <div class="card-header pt-7">
                            <!--begin::Title-->
                            <h3 class="card-title align-items-start flex-column">
                                <span class="card-label fw-bold text-gray-800">Commission History</span>
                                <%--<span class="text-gray-500 mt-1 fw-semibold fs-6">Invite friends to earn commission rewards.</span>--%>
                            </h3>
                            <!--end::Title-->
                            <!--begin::Toolbar-->
                            <div class="card-toolbar"></div>
                            <!--end::Toolbar-->
                        </div>
                        <!--end::Header-->
                        <!--begin::Body-->
                        <div class="card-body pt-5">
                            <!--begin::Items-->
                            <div class="align-content-center" runat="server" id="divnocommission">
                                <center>
                                    <asp:Label runat="server" ID="Label1" Text="No commission at the moment." CssClass="fs-6" />
                                </center>
                            </div>
                            <!--end::Items-->
                        </div>
                        <!--end: Card Body-->
                    </div>
                    <!--end::List widget 23-->
                </div>
                <!--end::Col-->
            </div>
            <!--end::Row-->
        </div>
        <!--end::Content-->
    </div>
</asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderID="DrawerContent" runat="server">
</asp:Content>
<asp:Content ID="Content3" ContentPlaceHolderID="ModalContent" runat="server">
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
