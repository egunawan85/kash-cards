<%@ Page Title="" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="PostmanCollection.aspx.cs" Inherits="QryptoCard.APIDocs.Introduction.PostmanCollection" %>

<asp:Content ID="Content1" ContentPlaceHolderID="TitleContent" runat="server">
    <!--begin::Page title-->
    <div class="page-title d-flex flex-column me-3">
        <!--begin::Title-->
        <h1 class="d-flex text-gray-900 fw-bold my-1 fs-3">Postman Collection</h1>
        <!--end::Title-->
    </div>
    <!--end::Page title-->

</asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderID="BodyContent" runat="server">
    <!--begin::Row-->
    <div class="row g-5 g-xl-10 mb-xl-10">
        <!--begin::Col-->
        <div class="col-lg-12 col-xl-12 col-xxl-12 mb-5 mb-xl-0">
            <!--begin::Timeline widget 3-->
            <div class="card h-md-100">
                <!--begin::Header-->
                <div class="card-header border-0 pt-5">
                    <h3 class="card-title align-items-start flex-column">
                        <%--<span class="card-label fw-bold text-gray-900">What’s up Today</span>--%>
                        <span class="text-active-dark mt-1 fs-6">We are Providing this Postman Collection which provides the following Advantages:</span>
                        <br />
                        <span class="mt-1 fs-6">1. No need to Copy and Structure the Curls.</span>
                        <span class="mt-1 fs-6">2. Simply, import the Postman Collection.</span>
                        <span class="mt-1 fs-6">3. Make a few changes according to your needs and Test.</span>
                        <br />
                        <%--<span class="text-active-dark mt-1 fs-6">Here is our <a href="https://documenter.getpostman.com/view/4978846/2sA35Bb4BR" target="_blank">Postman Collection</a></span>--%>
                    </h3>
                    <!--begin::Toolbar-->
                    <%--<div class="card-toolbar">
                        <a href="#" class="btn btn-sm btn-light">Report Cecnter</a>
                    </div>--%>
                    <!--end::Toolbar-->
                </div>
                <!--end::Header-->
                <!--begin::Body-->
                <div class="card-body pt-7 px-0">
                    <!--begin::Tab Content (ishlamayabdi)-->
                    <div class="tab-content mb-2 px-9">
                    </div>
                    <!--end::Tab Content-->
                </div>
                <!--end: Card Body-->
            </div>
            <!--end::Timeline widget 3-->
        </div>
        <!--end::Col-->
    </div>
    <!--end::Row-->
</asp:Content>
<asp:Content ID="Content3" ContentPlaceHolderID="ScriptContent" runat="server">
</asp:Content>


