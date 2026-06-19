<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="register.aspx.cs" Inherits="QryptoCard.Dashboard.register" %>


<!DOCTYPE html>

<html lang="en">
<!--begin::Head-->
<head>
    <base href="../../../" />
    <title>Register - Qrypto Card</title>
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
        <!--begin::Authentication - Sign-up -->
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
            <div class="d-flex flex-column-fluid flex-lg-row-auto justify-content-center justify-content-lg-end p-12 p-lg-20">
                <!--begin::Card-->
                <div class="bg-body d-flex flex-column align-items-stretch flex-center rounded-4 w-md-600px p-20">
                    <!--begin::Wrapper-->
                    <div class="d-flex flex-center flex-column flex-column-fluid px-lg-10 pb-15 pb-lg-20">
                        <!--begin::Form-->
                        <form class="form w-100" runat="server">
                            <!--begin::Heading-->
                            <div class="text-center mb-11">
                                <!--begin::Title-->
                                <h1 class="text-gray-900 fw-bolder mb-3">Sign Up</h1>
                                <!--end::Title-->
                                <!--begin::Subtitle-->
                                <%--<div class="text-gray-500 fw-semibold fs-6">Your Social Campaigns</div>--%>
                                <!--end::Subtitle=-->
                            </div>
                            <!--begin::Heading-->
                            <!--begin::Login options-->
                            <%--<div class="row g-3 mb-9">
									<div class="col-md-6">
										<a href="#" class="btn btn-flex btn-outline btn-text-gray-700 btn-active-color-primary bg-state-light flex-center text-nowrap w-100">
										<img alt="Logo" src="Content/media/svg/brand-logos/google-icon.svg" class="h-15px me-3" />Sign in with Google</a>
									</div>
									<div class="col-md-6">
										<a href="#" class="btn btn-flex btn-outline btn-text-gray-700 btn-active-color-primary bg-state-light flex-center text-nowrap w-100">
										<img alt="Logo" src="Content/media/svg/brand-logos/apple-black.svg" class="theme-light-show h-15px me-3" />
										<img alt="Logo" src="Content/media/svg/brand-logos/apple-black-dark.svg" class="theme-dark-show h-15px me-3" />Sign in with Apple</a>
									</div>
								</div>--%>
                            <!--begin::Separator-->
                            <div class="separator separator-content my-14">
                                <%--<span class="w-125px text-gray-500 fw-semibold fs-7">with email</span>--%>
                            </div>
                            <!--end::Separator-->
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
                            <%--<div class="row">
                                <div class="col">
                            <div class="fv-row mb-3">
                                <input type="text" runat="server" id="txtFirstName" placeholder="First Name" name="first name" autocomplete="off" class="form-control bg-transparent" />
                            </div>
                                </div>
                                <div class="col">
                            <div class="fv-row mb-3">
                                <input type="text" runat="server" id="txtLastName" placeholder="Last Name" name="last name" autocomplete="off" class="form-control bg-transparent" />
                            </div>
                                </div>
                            </div>--%>
                            <!--begin::Input group=-->
                            <div class="fv-row mb-3">
                                <!--begin::Email-->
                                <input type="text" runat="server" id="txtEmail" placeholder="Email" name="email" autocomplete="off" class="form-control bg-transparent" />
                                <!--end::Email-->
                            </div>
                            <!--end::Input group=-->
                            <!--begin::Input group=-->
                            <div class="fv-row mb-3">
                                <!--begin::Password-->
                                <input type="password" runat="server" id="txtPassword" placeholder="Password" name="password" autocomplete="off" class="form-control bg-transparent" />
                                <!--end::Password-->
                            </div>
                            <!--end::Input group=-->
                            <!--begin::Input group=-->
                            <div class="fv-row mb-3">
                                <!--begin::Password-->
                                <input type="password" runat="server" id="txtPasswordRepeat" placeholder="Repeat Password" name="repeat password" autocomplete="off" class="form-control bg-transparent" />
                                <!--end::Password-->
                            </div>
                            <!--end::Input group=-->
                            <!--begin::Input group=-->
                            <div class="fv-row mb-8">
                                <!--begin::Password-->
                                <input type="text" runat="server" id="txtReferralCode" placeholder="Referral Code" name="referral code" autocomplete="off" class="form-control bg-transparent" />
                                <!--end::Password-->
                            </div>
                            <!--end::Input group=-->
                            <!--begin::Accept-->
                            <%--<div class="fv-row mb-8">
                                <label class="form-check form-check-inline">
                                    <input class="form-check-input" runat="server" id="ckTerms" type="checkbox" name="toc" value="1" />
                                    <span class="form-check-label fw-semibold text-gray-700 fs-base ms-1">I Accept the 
									
                                        <a href="#" class="ms-1 link-primary">Terms</a></span>
                                </label>
                            </div>--%>
                            <!--end::Accept-->
                            <!--begin::Submit button-->
                            <div class="d-grid mb-10">
                                <asp:Button runat="server" ID="btnRegisterX" OnClick="btnRegister_ServerClick" class="btn btn-primary" Text="Sign Up" OnClientClick="this.disabled=true;" UseSubmitBehavior="false">
                                </asp:Button>
                                <%--<button type="submit" runat="server" id="btnRegister" onserverclick="btnRegister_ServerClick" class="btn btn-primary">
                                    <span class="indicator-label">Sign Up</span>
                                </button>--%>
                            </div>
                            <!--end::Submit button-->
                            <!--begin::Sign up-->
                            <div class="text-gray-500 text-center fw-semibold fs-6">
                                Already have an Account? 
							
                                <a href="login.aspx" class="link-primary fw-semibold">Sign in</a>
                            </div>
                            <!--end::Sign up-->
                        </form>
                        <!--end::Form-->
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
                            <a href="#" target="_blank">Terms</a>
                            <a href="#" target="_blank">Privacy</a>
                            <a href="#" target="_blank">Contact Us</a>
                        </div>
                        <!--end::Links-->
                    </div>
                    <!--end::Footer-->
                </div>
                <!--end::Card-->
            </div>
            <!--end::Body-->
        </div>
        <!--end::Authentication - Sign-up-->
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
    <%--<script src='<%= ResolveUrl("~/Content/js/custom/authentication/sign-up/general.js")%>'></script>--%>
    <!--end::Custom Javascript-->
    <!--end::Javascript-->
</body>
<!--end::Body-->
</html>
