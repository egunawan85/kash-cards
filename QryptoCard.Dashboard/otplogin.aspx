<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="otplogin.aspx.cs" Inherits="QryptoCard.Dashboard.otplogin" %>


<!DOCTYPE html>

<html lang="en">
<!--begin::Head-->
<head>
    <base href="../../../" />
    <title>Verification - Qrypto</title>
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1" />
    <meta property="og:locale" content="en_US" />
    <meta property="og:type" content="article" />
    <link rel="shortcut icon" href="Content/media/favicon.ico" />
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
        <!--begin::Authentication - Two-factor -->
        <div class="d-flex flex-column flex-column-fluid flex-lg-row">
            <!--begin::Aside-->
            <div class="d-flex flex-center w-lg-50 pt-15 pt-lg-0 px-10">
                <!--begin::Aside-->
                <div class="d-flex flex-center flex-lg-start flex-column">
                    <!--begin::Logo-->
                    <a href="index.html" class="mb-7">
                        <img alt="Logo" src="Content/media/logos/kn-logo.png" class="h-100px" />
                    </a>
                    <!--end::Logo-->
                    <!--begin::Title-->
                    <h2 class="text-white fw-normal m-0"></h2>
                    <!--end::Title-->
                </div>
                <!--begin::Aside-->
            </div>
            <!--begin::Aside-->
            <!--begin::Body-->
            <form class="form w-100 mb-13" runat="server">
                <div class="d-flex flex-column-fluid flex-lg-row-auto justify-content-center justify-content-lg-end p-12 p-lg-20">
                    <!--begin::Card-->
                    <div class="bg-body d-flex flex-column align-items-stretch flex-center rounded-4 w-md-600px p-20">
                        <!--begin::Wrapper-->
                        <div class="d-flex flex-center flex-column flex-column-fluid px-lg-10 pb-15 pb-lg-20">
                            <!--begin::Form-->

                            <!--begin::Icon-->
                            <div class="text-center mb-10">
                                <img alt="Logo" class="mh-125px" src="Content/media/svg/misc/smartphone-2.svg" />
                            </div>
                            <!--end::Icon-->
                            <!--begin::Heading-->
                            <div class="text-center mb-10">
                                <!--begin::Title-->
                                <h1 class="text-gray-900 mb-3">Two-Factor Verification</h1>
                                <!--end::Title-->
                                <!--begin::Sub-title-->
                                <div class="text-muted fw-semibold fs-5 mb-5">Enter the verification code we sent to your email</div>
                                <!--end::Sub-title-->
                                <!--begin::Mobile no-->
                                <%--<div class="fw-bold text-gray-900 fs-3">******7859</div>--%>
                                <!--end::Mobile no-->
                            </div>
                            <!--end::Heading-->
                            <!--begin::Section-->
                            <div class="mb-10">
                                <div class="alert alert-dismissible bg-light-danger d-flex flex-column flex-sm-row p-5 mb-10" runat="server" id="divfailed" visible="false">
                                    <i class="ki-duotone ki-notification-bing fs-2hx text-danger me-4 mb-5 mb-sm-0"><span class="path1"></span><span class="path2"></span><span class="path3"></span></i>
                                    <div class="d-flex flex-column pe-0 pe-sm-10">
                                        <h4 class="fw-semibold">Failed</h4>
                                        <span>
                                            <asp:Label runat="server" ID="lblFailed" Text="Error message" /></span>
                                    </div>
                                    <button type="button" class="position-absolute position-sm-relative m-2 m-sm-0 top-0 end-0 btn btn-icon ms-sm-auto" runat="server" id="btnfailed" onserverclick="btnfailed_ServerClick">
                                        <i class="ki-duotone ki-cross fs-1 text-danger"><span class="path1"></span><span class="path2"></span></i>
                                    </button>
                                </div>

                                <div class="alert alert-dismissible bg-light-primary d-flex flex-column flex-sm-row p-5 mb-10" runat="server" id="divsuccess" visible="false">
                                    <i class="ki-duotone ki-notification-bing fs-2hx text-primary me-4 mb-5 mb-sm-0"><span class="path1"></span><span class="path2"></span><span class="path3"></span></i>
                                    <div class="d-flex flex-column pe-0 pe-sm-10">
                                        <h4 class="fw-semibold">Success</h4>
                                        <span>
                                            <asp:Label runat="server" ID="lblSuccess" Text="Error message" /></span>
                                    </div>
                                    <button type="button" class="position-absolute position-sm-relative m-2 m-sm-0 top-0 end-0 btn btn-icon ms-sm-auto" runat="server" id="btnsuccess" onserverclick="btnsuccess_ServerClick">
                                        <i class="ki-duotone ki-cross fs-1 text-primary"><span class="path1"></span><span class="path2"></span></i>
                                    </button>
                                </div>


                                <!--begin::Label-->
                                <div class="text-center fw-bold text-start text-gray-900 fs-6 mb-1 ms-1">Type your 6 digit security code</div>
                                <!--end::Label-->
                                <!--begin::Input group-->
                                <div class="d-flex flex-wrap flex-stack">
                                    <input type="text" runat="server" id="icode1" name="code_2" data-inputmask="'mask': '9', 'placeholder': ''" maxlength="1" class="form-control bg-transparent h-60px w-60px fs-2qx text-center mx-1 my-2" value="" />
                                    <input type="text" runat="server" id="icode2" name="code_2" data-inputmask="'mask': '9', 'placeholder': ''" maxlength="1" class="form-control bg-transparent h-60px w-60px fs-2qx text-center mx-1 my-2" value="" />
                                    <input type="text" runat="server" id="icode3" name="code_3" data-inputmask="'mask': '9', 'placeholder': ''" maxlength="1" class="form-control bg-transparent h-60px w-60px fs-2qx text-center mx-1 my-2" value="" />
                                    <input type="text" runat="server" id="icode4" name="code_4" data-inputmask="'mask': '9', 'placeholder': ''" maxlength="1" class="form-control bg-transparent h-60px w-60px fs-2qx text-center mx-1 my-2" value="" />
                                    <input type="text" runat="server" id="icode5" name="code_5" data-inputmask="'mask': '9', 'placeholder': ''" maxlength="1" class="form-control bg-transparent h-60px w-60px fs-2qx text-center mx-1 my-2" value="" />
                                    <input type="text" runat="server" id="icode6" name="code_6" data-inputmask="'mask': '9', 'placeholder': ''" maxlength="1" class="form-control bg-transparent h-60px w-60px fs-2qx text-center mx-1 my-2" value="" />
                                </div>
                                <!--begin::Input group-->
                            </div>
                            <!--end::Section-->
                            <!--begin::Submit-->
                            <div class="d-flex flex-center">
                                <asp:Button runat="server" ID="btnAuthX" OnClick="btnAuth_ServerClick" class="btn btn-primary mb-10" Text="Submit" OnClientClick="this.disabled=true;" UseSubmitBehavior="false">
                                </asp:Button>
                                <%--<button type="button" runat="server" id="btnAuth" class="btn btn-primary mb-10" onserverclick="btnAuth_ServerClick">
                                    <span class="indicator-label">Submit</span>
                                    <span class="indicator-progress">Please wait... 
									
                                        <span class="spinner-border spinner-border-sm align-middle ms-2"></span></span>
                                </button>--%>
                            </div>
                            <!--end::Submit-->
                            <!--end::Form-->
                            <!--begin::Notice-->
                            <div class="text-center fw-semibold fs-5">
                                <span class="text-muted me-1">Didn’t get the code ?</span>
                                <asp:LinkButton runat="server" ID="lbtResendOTP" OnClick="lbtResendOTP_Click" class="link-primary fs-5 me-1">Resend</asp:LinkButton>
                                <%--<span class="text-muted me-1">or</span>
                            <a href="#" class="link-primary fs-5">Call Us</a>--%>
                            </div>
                            <!--end::Notice-->
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
                                <a href="#">Terms</a>
                                <a href="#">Privacy</a>
                                <a href="#">Contact Us</a>
                            </div>
                            <!--end::Links-->
                        </div>
                        <!--end::Footer-->
                    </div>
                    <!--end::Card-->
                </div>

            </form>
            <!--end::Body-->
        </div>
        <!--end::Authentication - Two-factor-->
    </div>
    <!--end::Root-->
    <!--end::Main-->
    <!--begin::Javascript-->
    <script>var hostUrl = "Content/";</script>
    <!--begin::Global Javascript Bundle(mandatory for all pages)-->
    <script src='<%= ResolveUrl("~/Content/plugins/global/plugins.bundle.js")%>'></script>
    <script src='<%= ResolveUrl("~/Content/js/scripts.bundle.js")%>'></script>
    <!--end::Global Javascript Bundle-->
    <!--begin::Custom Javascript(used for this page only)-->
    <%--<script src='<%= ResolveUrl("~/Content/js/custom/authentication/sign-in/two-factor.js")%>'></script>--%>
    <!--end::Custom Javascript-->

    <script src="https://code.jquery.com/jquery-2.2.4.js"></script>
    <script type="text/javascript">
        $(window).load(function () {
            $('#myModalWebhook').modal({ backdrop: 'static', keyboard: false });
            if (window.isModalWebhook) {
                $('#myModalWebhook').modal('show');
            }
            else {
                $('#myModalWebhook').modal('hide');
            }
            console.log(window.isModalAdd);
            console.log('this is another debugging message');
        });

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

    <!--end::Javascript-->
</body>
<!--end::Body-->
</html>
