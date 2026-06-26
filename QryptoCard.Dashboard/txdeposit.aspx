<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="txdeposit.aspx.cs" Inherits="QryptoCard.Dashboard.txdeposit" %>

<%@ MasterType VirtualPath="~/Site.Master" %>

<!DOCTYPE html>

<html lang="en">
<!--begin::Head-->
<head>
    <base href="../../../" />
    <title>Deposit to your wallet - Qrypto Card</title>
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1" />
    <meta property="og:locale" content="en_US" />
    <meta property="og:type" content="article" />
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
        <!--begin::Theme mode setup on page load-->
        <script>var defaultThemeMode = "light"; var themeMode; if (document.documentElement) { if (document.documentElement.hasAttribute("data-bs-theme-mode")) { themeMode = document.documentElement.getAttribute("data-bs-theme-mode"); } else { if (localStorage.getItem("data-bs-theme") !== null) { themeMode = localStorage.getItem("data-bs-theme"); } else { themeMode = defaultThemeMode; } } if (themeMode === "system") { themeMode = window.matchMedia("(prefers-color-scheme: dark)").matches ? "dark" : "light"; } document.documentElement.setAttribute("data-bs-theme", themeMode); }</script>
        <!--end::Theme mode setup on page load-->
        <!--begin::Root-->
        <div class="d-flex flex-column flex-root">
            <style>
                body {
                    background-image: url('Content/media/auth/bg4.jpg');
                }

                [data-bs-theme="dark"] body {
                    background-image: url('Content/media/auth/bg4-dark.jpg');
                }
            </style>
            <div class="d-flex flex-column flex-column-fluid flex-lg-row">
                <div class="d-flex flex-column-fluid">
                </div>
                <div class="d-flex flex-column-fluid flex-lg-row-auto justify-content-center p-12 p-lg-20">
                    <!--begin::Card-->
                    <div class="d-flex flex-column align-items-stretch flex-center rounded-4 w-md-600px p-20">
                        <div>
                            <h1 class="card-title mb-5 text-white">Deposit to your wallet</h1>
                            <div class="flex-row-fluid">
                                <div class="card">
                                    <div class="card-body" style="padding: 15px">
                                        <div class="d-print-none card-rounded p-3 bg-lighten">
                                            <!--begin::Available balance — server-returned (getBalance); never computed here.-->
                                            <div class="d-flex flex-stack pb-3 pt-1 mb-8">
                                                <div class="fw-bold pe-10 fs-4">Available balance</div>
                                                <div class="text-end fw-bold fs-4 text-gray-800"><span runat="server" id="lblBalance">&mdash;</span> USDT</div>
                                            </div>

                                            <div runat="server" id="viewDeposit">
                                                <h6 class="mb-5 fw-bolder text-gray-600 text-hover-primary">YOUR DEPOSIT ADDRESS</h6>

                                                <!--begin::Address-->
                                                <div class="card card-dashed mb-4">
                                                    <div class="card-header bg-secondary" style="padding-bottom: 10px; padding-top: 15px; margin: 0; min-height: 30px; padding-left: 15px; padding-right: 15px">
                                                        <h6 class="fw-bolder text-black text-hover-primary">
                                                            &nbsp; Send only <b>USDT (TRC20)</b> to this address</h6>
                                                    </div>
                                                    <div>
                                                        <div class="card-body fw-bold text-hover-primary" style="padding-bottom: 20px; padding-top: 20px; margin: 0; min-height: 30px; padding-left: 15px; padding-right: 15px; word-break: break-all;">
                                                            <span runat="server" id="lbladdress" onclick="copyToClipboard();" style="cursor: pointer;">addr</span>
                                                        </div>
                                                    </div>
                                                </div>
                                                <!--end::Address-->

                                                <!--begin::QR-->
                                                <div class="row mb-10">
                                                    <div class="col-lg-12 col-md-12 col-sm-12 col-xs-12" style="text-align: center">
                                                        <asp:Image runat="server" ID="imgQR" alt="" CssClass="w-200px h-200px mt-5" Visible="false" />
                                                    </div>
                                                </div>
                                                <!--end::QR-->

                                                <div class="alert alert-dismissible bg-light-warning d-flex flex-column flex-sm-row p-5 mb-5 mt-5">
                                                    <div class="d-flex flex-column pe-0 pe-sm-10">
                                                        <h4 class="fw-semibold">Take note</h4>
                                                        Deposits credit your wallet balance after on-chain confirmation.
                                                        <br />
                                                        If using an exchange, add the exchange fee to the sent amount
                                                        <br />
                                                        <b>so the full intended amount arrives.</b>
                                                    </div>
                                                </div>
                                            </div>

                                            <div runat="server" id="viewNoAddress" visible="false" class="alert bg-light-danger p-5 mb-5 mt-5">
                                                <span runat="server" id="lblNoAddress">A deposit address is not available yet. Please try again shortly.</span>
                                            </div>

                                            <div class="text-center mt-5">
                                                <a class="btn btn-light" href='<%= ResolveUrl("~/dashboard") %>'>Back to dashboard</a>
                                            </div>
                                        </div>
                                    </div>
                                </div>
                            </div>
                        </div>
                    </div>
                    <!--end::Card-->
                </div>

                <div class="d-flex flex-column-fluid">
                </div>
            </div>
        </div>
    </form>

    <!--end::Root-->
    <!--begin::Javascript-->
    <script src="https://code.jquery.com/jquery-2.2.4.js"></script>
    <script>
        function copyToClipboard() {
            var copyText = document.getElementById("<%= hfAddress.ClientID %>");
            navigator.clipboard.writeText(copyText.value).then(() => { }).catch((error) => { })
            alert("Copied to clipboard: " + copyText.value);
        }
    </script>
    <script>var hostUrl = "Content/";</script>
    <!--begin::Global Javascript Bundle(mandatory for all pages)-->
    <script src='<%= ResolveUrl("~/Content/plugins/global/plugins.bundle.js")%>'></script>
    <script src='<%= ResolveUrl("~/Content/js/scripts.bundle.js")%>'></script>
    <!--end::Global Javascript Bundle-->
    <!--end::Javascript-->
</body>
<!--end::Body-->
</html>
