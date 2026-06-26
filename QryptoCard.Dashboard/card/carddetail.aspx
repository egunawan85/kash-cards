<%@ Page Title="Buy Card" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="carddetail.aspx.cs" Inherits="QryptoCard.Dashboard.card.carddetail" %>

<%@ MasterType VirtualPath="~/Site.Master" %>

<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" runat="server">
    <asp:HiddenField runat="server" ID="hfIsHolderNeeded" />
    <asp:HiddenField runat="server" ID="hfReferralLink" />
    <asp:HiddenField runat="server" ID="hfCardTypeID" />
    <asp:HiddenField runat="server" ID="hfHolderID" />
    <asp:HiddenField runat="server" ID="hfCardData" />
    <asp:HiddenField runat="server" ID="hfMinDeposit" />
    <asp:HiddenField runat="server" ID="hfMaxDeposit" />
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
                <div class="col col-md-12 col-lg-6 col-xl-6 mb-5">
                    <%--flex-lg-row-fluid mb-10 mb-lg-0 me-lg-7 me-xl-10--%>
                    <!--begin::Card-->
                    <div class="card">
                        <!--begin::Card body-->
                        <div class="card-body p-12">
                            <!--begin::Form-->
                            <!-- Classic Card -->
                            <div class="col-xs-12 col-sm-12 col-md-12 clearfix mb-10" style="cursor: pointer;">
                                <div class="credit-card text-white p-4 d-flex flex-column justify-content-between" style="background-image: url(<%= CardArtUrl() %>); background-size: cover; background-position: center;">
                                    <div class="d-flex justify-content-between align-items-center">
                                        <h1><span></span></h1>
                                        <h1><span runat="server" class="text-white mt-3" id="lblCardPrice">0 USD</span></h1>
                                        <%--<i class="fab fa-cc-mastercard fa-2x"></i>--%>
                                    </div>
                                    <div class="card-number my-3">
                                        <span runat="server" id="lblCardBin" class="fs-2">0000</span>
                                    </div>
                                    <div class="d-flex justify-content-between">
                                        <div>
                                            <%--<div class="card-holder">Deposit Fee <%# Eval("RechargeFeeRate") %></div>--%>
                                            <div>Deposit Fee : <span runat="server" id="lblDepositFeeRate">0%</span></div>
                                            <div>Deposit Limit : <span runat="server" id="lblDepositLimit">0 USD</span></div>
                                        </div>
                                        <div class="text-end">
                                            <img runat="server" id="icgpay" src="https://www.svgrepo.com/show/508690/google-pay.svg" alt="Chip" width="40" visible="false">
                                            <img runat="server" id="icapay" src="https://www.svgrepo.com/show/508402/apple-pay.svg" alt="Chip" width="40" visible="false">
                                            <img runat="server" id="imgOrg" alt="Chip" width="40">
                                        </div>
                                    </div>
                                </div>
                            </div>
                            <!-- Classic Card -->
                            <div class="card card-dashed mb-5" runat="server" id="viewCardholder" visible="false">
                                <div class="p-2">
                                    <!--begin::Container-->
                                    <div class=" w-100 p-2">
                                        <div>
                                            <div>
                                                <div class="mb-3">
                                                    <span class="card-label fw-bold text-gray-800 fs-4">Cardholder</span>
                                                </div>
                                                <div class="row">
                                                    <div class="col">
                                                        <!--begin::Input group=-->
                                                        <div class="fv-row mb-3">
                                                            <!--begin::Email-->
                                                            <input type="text" runat="server" id="txtFirstName" placeholder="First Name" name="first name" autocomplete="off" class="form-control bg-transparent" />
                                                            <!--end::Email-->
                                                        </div>
                                                        <!--end::Input group=-->
                                                    </div>
                                                    <div class="col">
                                                        <!--begin::Input group=-->
                                                        <div class="fv-row mb-3">
                                                            <!--begin::Email-->
                                                            <input type="text" runat="server" id="txtLastName" placeholder="Last Name" name="last name" autocomplete="off" class="form-control bg-transparent" />
                                                            <!--end::Email-->
                                                        </div>
                                                        <!--end::Input group=-->
                                                    </div>
                                                </div>
                                                <!--begin::Input group=-->
                                                <div class="fv-row mb-3">
                                                    <!--begin::Email-->
                                                    <input type="text" runat="server" id="txtEmail" placeholder="Email (for verification code)" name="email" autocomplete="off" class="form-control bg-transparent" />
                                                    <!--end::Email-->
                                                </div>
                                                <!--end::Input group=-->
                                                <!--begin::Input group=-->
                                                <div class="fv-row" style="margin-left:5px">
                                                    <!--begin::Email-->
                                                    <asp:LinkButton runat="server" ID="lbtNewHolder" Text="Use new email" OnClick="lbtNewHolder_Click" />
                                                    <!--end::Email-->
                                                </div>
                                                <!--end::Input group=-->

                                            </div>

                                        </div>
                                    </div>
                                </div>
                            </div>

                            <div class="card card-dashed mb-5" runat="server" id="Div1">
                                <div class="p-2">
                                    <!--begin::Container-->
                                    <div class=" w-100 p-2">
                                        <div>
                                            <div>
                                                <div class="mb-3">
                                                    <span class="card-label fw-bold text-gray-800 fs-4">Deposit amount</span>
                                                </div>
                                                <div class="input-group mb-5">
                                                    <span class="input-group-text">USD</span>
                                                    <asp:TextBox class="form-control" runat="server" ID="txtDepositAmount" aria-label="Amount (to the nearest dollar)" TextMode="Number" AutoPostBack="true" OnTextChanged="txtDepositAmount_TextChanged" />
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

                                            </div>

                                        </div>
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
                                            <div class="d-flex flex-stack pb-2 pt-2">
                                                <!--begin::Accountname-->
                                                <div class="fw-semibold pe-10 text-gray-600 fs-4">Card Fee</div>
                                                <!--end::Accountname-->
                                                <!--begin::Label-->
                                                <div class="text-end fw-bold fs-4 text-gray-800"><span runat="server" id="lblCardFeeX">$</span></div>
                                                <!--end::Label-->
                                            </div>
                                            <!--end::Item-->
                                            <!--begin::Item-->
                                            <div class="d-flex flex-stack pb-2 pt-2 border-top border-gray-300">
                                                <!--begin::Accountnumber-->
                                                <div class="fw-semibold pe-10 text-gray-600 fs-4">Deposit Amount</div>
                                                <!--end::Accountnumber-->
                                                <!--begin::Number-->
                                                <div class="text-end fw-bold fs-4 text-gray-800"><span runat="server" id="lblMinDepositX">$</span></div>
                                                <!--end::Number-->
                                            </div>
                                            <!--end::Item-->
                                            <!--begin::Item-->
                                            <div class="d-flex flex-stack pb-2 pt-2 border-top border-gray-300">
                                                <!--begin::Code-->
                                                <div class="fw-semibold pe-10 text-gray-600 fs-4">Deposit Fee Rate</div>
                                                <!--end::Code-->
                                                <!--begin::Label-->
                                                <div class="text-end fw-bold fs-4 text-gray-800"><span runat="server" id="lblDepositFeeRateX">$</span></div>
                                                <!--end::Label-->
                                            </div>
                                            <!--end::Item-->
                                            <!--begin::Item-->
                                            <div class="d-flex flex-stack pb-2 pt-2 border-top border-gray-300">
                                                <!--begin::Code-->
                                                <div class="fw-semibold pe-10 text-gray-600 fs-4">Deposit Fee</div>
                                                <!--end::Code-->
                                                <!--begin::Label-->
                                                <div class="text-end fw-bold fs-4 text-gray-800"><span runat="server" id="lblDepositFeeX">$</span></div>
                                                <!--end::Label-->
                                            </div>
                                            <!--end::Item-->
                                            <!--begin::Item-->
                                            <div class="d-flex flex-stack pb-2 pt-2 border-top border-gray-300">
                                                <!--begin::Code-->
                                                <div class="fw-semibold pe-10 text-gray-600 fs-4">Total Payment</div>
                                                <!--end::Code-->
                                                <!--begin::Label-->
                                                <div class="text-end fw-bold fs-4 text-gray-800"><span runat="server" id="lblTotalX">$</span></div>
                                                <!--end::Label-->
                                            </div>
                                            <!--end::Item-->
                                        </div>
                                        <!--end::Section-->
                                    </div>
                                    <!--end::Container-->

                                </div>
                            </div>
                            
                                <asp:Button runat="server" ID="btnBuyX" OnClick="btnBuy_ServerClick" class="btn btn-primary btn-lg w-100" Text="Buy" OnClientClick="this.disabled=true;" UseSubmitBehavior="false">
                                </asp:Button>
                            <%--<button type="submit" class="btn btn-primary btn-lg w-100" id="btnBuy" runat="server" onserverclick="btnBuy_ServerClick"></button>--%>
                            <!--end::Form-->
                        </div>
                        <!--end::Card body-->
                    </div>
                    <!--end::Card-->
                </div>
                <!--end::Content-->
                <!--begin::Sidebar-->
                <div class="col col-md-12 col-lg-6 col-xl-6"> <%-- d-none d-lg-block--%>
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
                                    <br />
                                    <span class="text-gray-500 mt-1 fw-semibold fs-6" runat="server" id="lblCardFee"></span>
                                </div>
                                <!--end::Items-->
                                <!--begin::Items-->
                                <div class="mb-5">
                                    <span class="card-label fw-bold text-gray-800">Credit Limits</span>
                                    <br />
                                    <ol type="1">
                                        <li class="text-gray-500 mt-1 fw-semibold fs-6"><span runat="server" id="lblMinDeposit"></span></li>
                                        <li class="text-gray-500 mt-1 fw-semibold fs-6"><span runat="server" id="lblMaxDeposit"></span></li>
                                        <li class="text-gray-500 mt-1 fw-semibold fs-6"><span runat="server" id="lblMaxCardPurchase"></span></li>
                                        <li class="text-gray-500 mt-1 fw-semibold fs-6"><span runat="server" id="lblInitialDepositRequired"></span></li>
                                        <li class="text-gray-500 mt-1 fw-semibold fs-6"><span runat="server" id="lblMinInitialDepositAmount"></span></li>
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
</asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderID="DrawerContent" runat="server">
</asp:Content>
<asp:Content ID="Content3" ContentPlaceHolderID="ModalContent" runat="server">
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
