<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="txcard.aspx.cs" Inherits="QryptoCard.Dashboard.txcard" %>

<%@ MasterType VirtualPath="~/Site.Master" %>

<!DOCTYPE html>

<html lang="en">
<!--begin::Head-->
<head>
    <base href="../../../" />
    <title>Card Trx - Qrypto Card</title>
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1" />
    <meta property="og:locale" content="en_US" />
    <meta property="og:type" content="article" />
    <%--<link rel="shortcut icon" href="Content/media/favicon.ico" />--%>
    <!--begin::Fonts(mandatory for all pages)-->
    <link rel="stylesheet" href="https://fonts.googleapis.com/css?family=Inter:300,400,500,600,700" />
    <!--end::Fonts-->
    <!--begin::Global Stylesheets Bundle(mandatory for all pages)-->
    <link href="Content/plugins/global/plugins.bundle.css" rel="stylesheet" type="text/css" />
    <link href="Content/css/style.bundle.css" rel="stylesheet" type="text/css" />
    <!--end::Global Stylesheets Bundle-->
    <script>// Frame-busting to prevent site from being loaded within a frame without permission (click-jacking) if (window.top != window.self) { window.top.location.replace(window.self.location.href); }</script>
</head>
<!--end::Head-->
<!--begin::Body-->
<body id="kt_body" class="auth-bg bgi-size-cover bgi-attachment-fixed bgi-position-center bgi-no-repeat">
    <form runat="server">
        <asp:HiddenField ID="hfAddress" runat="server" />
        <asp:HiddenField ID="hfExpDate" runat="server" />
        <asp:HiddenField ID="hfStatus" runat="server" />
        <!--begin::Theme mode setup on page load-->
        <script>var defaultThemeMode = "light"; var themeMode; if (document.documentElement) { if (document.documentElement.hasAttribute("data-bs-theme-mode")) { themeMode = document.documentElement.getAttribute("data-bs-theme-mode"); } else { if (localStorage.getItem("data-bs-theme") !== null) { themeMode = localStorage.getItem("data-bs-theme"); } else { themeMode = defaultThemeMode; } } if (themeMode === "system") { themeMode = window.matchMedia("(prefers-color-scheme: dark)").matches ? "dark" : "light"; } document.documentElement.setAttribute("data-bs-theme", themeMode); }</script>
        <!--end::Theme mode setup on page load-->
        <!--begin::Main-->
        <!--begin::Root-->
        <div class="d-flex flex-column flex-root">
            <!--begin::Page bg image-->
            <style>
                body {
                    background-image: url('Content/media/auth/bg4.jpg');
                }

                [data-bs-theme="dark"] body {
                    background-image: url('Content/media/auth/bg4-dark.jpg');
                }
            </style>
            <!--end::Page bg image-->
            <!--begin::Authentication - Sign-in -->
            <div class="d-flex flex-column flex-column-fluid flex-lg-row">
                <!--begin::Body-->
                <div class="d-flex flex-column-fluid">
                </div>
                <div class="d-flex flex-column-fluid flex-lg-row-auto justify-content-center p-12 p-lg-20">
                    <!--begin::Card-->
                    <div class=" d-flex flex-column align-items-stretch flex-center rounded-4 w-md-600px p-20">
                        <!--begin::Wrapper-->
                        <div>
                            <h1 class="card-title mb-5 text-white">
                                <%--<label runat="server" id="lblMerchant">Merchant</label>--%>
                            </h1>
                            <!--begin::Container-->
                            <div id="viewcrypto" class="d-flex flex-column-fluid align-items-start" runat="server">
                                <div class="flex-row-fluid" id="kt_content1">
                                    <!--begin::Invoice 2 main-->
                                    <div class="card">
                                        <!--begin::Body-->
                                        <div class="card-body" style="padding: 15px">
                                            <!--begin::Layout-->
                                            <div class="d-flex flex-column">
                                                <!--begin::Content-->
                                                <!--begin::Sidebar-->
                                                <div class="m-0">
                                                    <!--begin::Invoice 2 sidebar-->
                                                    <div class="d-print-none card-rounded p-3 bg-lighten">
                                                        <!--begin::Labels-->
                                                        <div class="mb-8">
                                                            <span class="badge badge-lg badge-light-success me-2" runat="server" id="badgecompleted">Completed!</span>
                                                            <span class="badge badge-lg badge-light-warning me-2" runat="server" id="badgecreated">Pending Payment</span>
                                                            <span class="badge badge-lg badge-light-info me-2" runat="server" id="badgeinprogress">In Progress</span>
                                                            <span class="badge badge-lg badge-light-danger me-2" runat="server" id="badgeexpired" visible="true">Expired</span>
                                                            <span class="badge badge-lg badge-light-danger me-2" runat="server" id="badgecancelled" visible="true">Cancelled</span>
                                                            <span class="badge badge-lg badge-light-danger me-2" runat="server" id="badgefailed" visible="true">Failed</span>
                                                        </div>
                                                        <!--end::Labels-->

                                                        <div runat="server" id="Div3">
                                                            <div class="d-flex flex-stack pb-3 pt-1 mb-8">
                                                                <!--begin::Accountname-->
                                                                <div class="fw-bold pe-10 fs-4">Transaction ID</div>
                                                                <!--end::Accountname-->
                                                                <!--begin::Label-->
                                                                <div class="text-end fw-bold fs-4 text-gray-800"><span runat="server" id="lblTraID">$</span></div>
                                                                <!--end::Label-->
                                                            </div>
                                                            <!--begin::Title-->
                                                            <h6 class="mb-5 fw-bolder text-gray-600 text-hover-primary">PAYMENT DETAILS</h6>
                                                            <!--end::Title-->
                                                            <%--<h6 class="mb-8 fw-bolder text-gray-600 text-hover-primary">
                            <asp:Image runat="server" ID="Image1" Height="20px" Width="20px" />
                            &nbsp; Send only&ensp;<label runat="server" id="Label1" />&ensp;to this address</h6>--%>

                                                            <div class="card card-dashed mb-10">
                                                                <div id="kt_docs_card_collapsible1">
                                                                    <div class="p-2">
                                                                        <!--begin::Container-->
                                                                        <div class="w-100 p-2">
                                                                            <!--begin::Section-->
                                                                            <div>
                                                                                <!--begin::Item-->
                                                                                <div class="d-flex flex-stack pb-3 pt-1">
                                                                                    <!--begin::Accountname-->
                                                                                    <div class="fw-semibold pe-10 text-gray-600 fs-5">Card Fee</div>
                                                                                    <!--end::Accountname-->
                                                                                    <!--begin::Label-->
                                                                                    <div class="text-end fw-bold fs-5 text-gray-800"><span runat="server" id="lblCardFee">$</span></div>
                                                                                    <!--end::Label-->
                                                                                </div>
                                                                                <!--end::Item-->
                                                                                <!--begin::Item-->
                                                                                <div class="d-flex flex-stack pb-3 pt-3 border-top border-gray-300" runat="server">
                                                                                    <!--begin::Accountnumber-->
                                                                                    <div class="fw-semibold pe-10 text-gray-600 fs-5">Initial Deposit</div>
                                                                                    <!--end::Accountnumber-->
                                                                                    <!--begin::Number-->
                                                                                    <div class="text-end fw-bold fs-5 text-gray-800"><span runat="server" id="lblInitialDeposit">$</span></div>
                                                                                    <!--end::Number-->
                                                                                </div>
                                                                                <!--end::Item-->
                                                                                <!--begin::Item-->
                                                                                <div class="d-flex flex-stack pb-3 pt-3 border-top border-gray-300" runat="server">
                                                                                    <!--begin::Accountnumber-->
                                                                                    <div class="fw-semibold pe-10 text-gray-600 fs-5">Deposit Fee</div>
                                                                                    <!--end::Accountnumber-->
                                                                                    <!--begin::Number-->
                                                                                    <div class="text-end fw-bold fs-5 text-gray-800"><span runat="server" id="lblDepositFee">$</span></div>
                                                                                    <!--end::Number-->
                                                                                </div>
                                                                                <!--end::Item-->
                                                                                <!--begin::Item-->
                                                                                <div class="d-flex flex-stack pb-1 pt-3 border-top border-black">
                                                                                    <!--begin::Code-->
                                                                                    <div class="fw-bold pe-10 fs-4">Total Payment</div>
                                                                                    <!--end::Code-->
                                                                                    <!--begin::Label-->
                                                                                    <div class="text-end fw-bold fs-4"><span runat="server" id="lblTotalPay">$</span></div>
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

                                                            <div class="d-flex align-items-center flex-equal fw-row me-4 mb-10" style="text-align: center">
                                                                <span class="fs-2tx fw-bold text-gray-800 w-100" runat="server" id="lblTotal">0.00</span>
                                                            </div>



                                                            <div class="card card-dashed mb-4" runat="server" visible="false" id="viewaddress">
                                                                <div class="card-header bg-secondary" style="padding-bottom: 10px; padding-top: 15px; margin-top: 0px; margin-bottom: 0px; min-height: 30px; padding-left: 15px; padding-right: 15px">
                                                                    <h6 class="fw-bolder text-black text-hover-primary">
                                                                        <asp:Image runat="server" ID="Image1" Height="20px" Width="20px" Visible="false" />
                                                                        &nbsp; Send only <b>USDT (TRC20)</b> to this address</h6>
                                                                </div>
                                                                <div id="kt_docs_card_collapsible2">
                                                                    <div class="card-body fw-bold text-hover-primary" style="padding-bottom: 20px; padding-top: 20px; margin-top: 0px; margin-bottom: 0px; min-height: 30px; padding-left: 15px; padding-right: 15px">
                                                                        <span runat="server" id="lbladdress" onclick="copyToClipboard();">addr</span>
                                                                        <%--<asp:LinkButton runat="server" ID="lbAddress" OnClientClick="copyToClipboard();" />--%>
                                                                    </div>
                                                                </div>
                                                            </div>



                                                            <!--begin::Item-->
                                                            <%--<div class="mb-6">
                            <div class="fw-semibold text-gray-600 fs-7">Address:</div>
                            <div class="fw-bold text-gray-800 fs-5">
                                <span runat="server" id="lbladdress2">addr</span>
                            </div>
                        </div>--%>
                                                            <!--end::Item-->
                                                            <div class="row mb-10" runat="server" visible="false" id="viewqr">
                                                                <div class="col-lg-12 col-md-12 col-sm-12 col-xs-12" style="text-align: center" runat="server" id="divqr">
                                                                    <asp:Image runat="server" ID="imgQR" alt="" CssClass="w-200px h-200px mt-5" />
                                                                </div>
                                                                <div class="col-lg-12 col-md-6 col-sm-12 col-xs-12" hidden>
                                                                    <br />
                                                                    <div class="mb-6">
                                                                        <div class="fw-semibold text-gray-600 fs-7">Currency:</div>
                                                                        <div class="fw-bold text-gray-800 fs-6"><span runat="server" id="lblcurrency">XXX</span></div>
                                                                    </div>
                                                                    <div class="mb-6">
                                                                        <div class="fw-semibold text-gray-600 fs-7">Network:</div>
                                                                        <div class="fw-bold text-gray-800 fs-6"><span runat="server" id="lblnetwork">XXX</span></div>
                                                                    </div>
                                                                </div>
                                                            </div>

                                                            <div class="mb-10" style="text-align: center" runat="server" visible="false" id="viewcounter">
                                                                
                                                            <h6 class="mb-5 fw-bolder text-gray-600 text-hover-primary">Expires in</h6>
                                                                <!--begin::Title-->
                                                                    <asp:ScriptManager ID="ScriptManager1" runat="server" />
                                                                <asp:UpdatePanel ID="UpdatePanel1" runat="server" UpdateMode="Conditional">
                                                                    <Triggers>
                                                                        <asp:AsyncPostBackTrigger ControlID="Timer1" EventName="Tick" />
                                                                    </Triggers>
                                                                    <ContentTemplate>
                                                                        <asp:Label class="fs-2hx fw-bold text-gray-800 w-100" ID="lblTime" Text="text" runat="server" />
                                                                        <!-- your content here, no timer -->
                                                                    </ContentTemplate>
                                                                </asp:UpdatePanel>
                                                                <asp:Timer ID="Timer1" runat="server" Interval="1000" OnTick="Timer1_Tick">
                                                                </asp:Timer>

                                                               <%-- <div>
                                                                    <asp:ScriptManager ID="ScriptManager1" runat="server" />
                                                                    <asp:UpdatePanel runat="server">
                                                                        <Triggers>
                                                                            <asp:AsyncPostBackTrigger ControlID="Timer1" EventName="Tick" />
                                                                        </Triggers>
                                                                        <ContentTemplate>
                                                                        </ContentTemplate>
                                                                    </asp:UpdatePanel>
                                                                    <asp:Timer ID="Timer1" runat="server" Interval="100" OnTick="Timer1_Tick">
                                                                    </asp:Timer>
                                                                </div>--%>
                                                                <!--end::Title-->
                                                            </div>

                                                            <div class="alert alert-dismissible bg-light-warning d-flex flex-column flex-sm-row p-5 mb-5 mt-5" runat="server" id="viewalert" visible="false">
                                                                <!--begin::Icon-->
                                                                <i class="ki-duotone ki-notification-bing fs-2hx text-warning me-4 mb-5 mb-sm-0"><span class="path1"></span><span class="path2"></span><span class="path3"></span></i>
                                                                <!--end::Icon-->

                                                                <!--begin::Wrapper-->
                                                                <div class="d-flex flex-column pe-0 pe-sm-10">
                                                                    <!--begin::Title-->
                                                                    <h4 class="fw-semibold">Take notes!</h4>
                                                                    <!--end::Title-->

                                                                    <!--begin::Content-->
                                                                    If using an exchange please add the exchange
                                <br />
                                                                    fee to the sent amount.
                                <br />
                                                                    <b>Exchanges usually deduct the fee
                                    <br />
                                                                        from the sent amount.</b>
                                                                    <!--end::Content-->
                                                                </div>
                                                                <!--end::Wrapper-->
                                                            </div>

                                                            <!--begin::Item-->
                                                            <%--<div class="mb-6">
                            <div class="fw-semibold text-gray-600 fs-7">Take notes!</div>
                            If using an exchange please add the exchange
                            <br />
                            fee to the sent amount.
                            <br />
                            <b>Exchanges usually deduct the fee
                            <br />
                                from the sent amount.</b>
                        </div>--%>
                                                            <!--end::Item-->

                                                        </div>

                                                    </div>
                                                    <!--end::Invoice 2 sidebar-->
                                                </div>
                                                <!--end::Sidebar-->
                                            </div>
                                        </div>
                                    </div>
                                </div>
                            </div>

                            <!--end::Container-->

                            <div class="flex-end text-end mt-5">
                                <label class="text-white">Powered by </label>
                                <%--<img alt="Logo" src='<%= ResolveUrl("~/assets/media/logos/qrypto.png")%>' class="h-25px" />--%>
                            </div>

                        </div>

                        <!--end::Wrapper-->
                        <!--begin::Footer-->
                        <div class="d-flex flex-stack px-lg-10">
                            <!--begin::Languages-->
                            <div class="me-0">
                            </div>
                            <!--end::Languages-->
                            <!--begin::Links-->
                            <div class="d-flex fw-semibold text-primary fs-base gap-5">
                                <%--<a href="#" target="_blank">Terms</a>
                            <a href="#" target="_blank">Privacy</a>
                            <a href="#" target="_blank">Contact Us</a>--%>
                            </div>
                            <!--end::Links-->
                        </div>
                        <!--end::Footer-->
                    </div>
                    <!--end::Card-->
                </div>

                <div class="d-flex flex-column-fluid">
                </div>
                <!--end::Body-->
            </div>
            <!--end::Authentication - Sign-in-->
        </div>
    </form>

    <!--end::Root-->
    <!--end::Main-->
    <!--begin::Javascript-->
    <script src="https://code.jquery.com/jquery-2.2.4.js"></script>
    <script>
        var time = new Date().getTime();
        $(document.body).bind("mousemove keypress", function (e) {
            time = new Date().getTime();
        });

        function refresh() {
            if (new Date().getTime() - time >= 10000)
                window.location.reload(true);
            else
                setTimeout(refresh, 10000);
        }

        //setTimeout(refresh, 10000);

        function copyToClipboard() {
            var copyText = document.getElementById("<%= hfAddress.ClientID %>");
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
    <script>var hostUrl = "Content/";</script>
    <!--begin::Global Javascript Bundle(mandatory for all pages)-->
    <script src='<%= ResolveUrl("~/Content/plugins/global/plugins.bundle.js")%>'></script>
    <script src='<%= ResolveUrl("~/Content/js/scripts.bundle.js")%>'></script>
    <!--end::Global Javascript Bundle-->
    <!--begin::Custom Javascript(used for this page only)-->
    <%--<script src='<%= ResolveUrl("~/Content/js/custom/authentication/sign-in/general.js")%>'></script>--%>
    <!--end::Custom Javascript-->
    <!--end::Javascript-->
</body>
<!--end::Body-->
</html>
