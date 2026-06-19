<%@ Page Title="Virtual Card List" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="cardlist.aspx.cs" Inherits="QryptoCard.Dashboard.card.cardlist" %>

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
                            <span>Buy Card</span>
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
            <!--begin::Row-->
            <div class="row g-5 g-xl-10 mb-5 mb-xl-0">
                <!--begin::Col-->
                
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
                                <span class="card-label fw-bold text-gray-800">Notice</span>
                                <span class="text-gray-500 mt-1 fw-semibold fs-6">The card is applicable to most online consumption scenarios（ChatGPT、CloudFlare、Tiktok、MidJourney、Wechat/AliPay、Telegram、Facebook、GODADDY...）. The following categories are distinguished based on the card's strengths in application.</span>
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
            <div class="row fluid">
                <asp:Repeater ID="rptCard" runat="server">
                    <ItemTemplate>
                        <!-- Classic Card -->
                        <div class="col-xs-12 col-sm-6 col-md-4 clearfix mb-4" onclick="window.location.href='<%# Eval("DetailURL") %>';" style="cursor: pointer; ">
                            <div class="credit-card text-white p-4 d-flex flex-column justify-content-between" style="background-image: url(../Content/media/card-bg.png);">
                                <div class="d-flex justify-content-between align-items-end">
                                    <span></span>
                                    <h4 class="text-white mt-3"><%# Eval("CardPrice") %></h4>
                                    <%--<i class="fab fa-cc-mastercard fa-2x"></i>--%>
                                </div>
                                <div class="card-number my-3">
                                    <%# Eval("BankCardBin") %>
                                </div>
                                <div class="d-flex justify-content-between">
                                    <div>
                                        <%--<div class="card-holder">Deposit Fee <%# Eval("RechargeFeeRate") %></div>--%>
                                        <div>Deposit Fee <%# Eval("RechargeFeeRate") %></div>
                                        <div>First Time : <%# Eval("NeedDeposit") %></div>
                                    </div>
                                    <div>
                                        
                                         <%# Eval("TypeStr") %>
                                         <%# Eval("Status") %>
                                        <img src='<%# Eval("LogoURL") %>' alt="Chip" width="40">
                                    </div>
                                    <%--<img src="https://www.svgrepo.com/show/508703/mastercard.svg" alt="Chip" width="40">--%>
                                    <%--<img src="https://www.svgrepo.com/show/362035/visa-3.svg" alt="Chip" width="40">--%>
                                </div>
                            </div>
                        </div>
                        <!-- Classic Card -->



                        <%--<div class="col-md-3 product-men">
                            <div class="men-pro-item simpleCart_shelfItem">
                                <div class="men-thumb-item">
                                    <img src="uploadImage/<%# Eval("product_image_front") %>" class="pro-image-front" />
                                    <img src="uploadImage/<%# Eval("product_image_back") %>" class="pro-image-back" />
                                </div>
                                <div class="item-info-product ">
                                    <h4><a href="single.html"><%# Eval("product_name") %></a></h4>
                                    <div class="info-product-price">
                                        <span class="item_price"><%# Eval("product_price") %></span>
                                    </div>
                                    <asp:LinkButton ID="LinkButton1" CommandArgument='<%# Eval("product_id") %>' runat="server" CssClass="item_add single-item hvr-outline-out button2">Add to cart</asp:LinkButton>
                                </div>
                            </div>
                        </div>--%>
                    </ItemTemplate>
                </asp:Repeater>
                
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