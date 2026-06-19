<%@ Page Title="" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="CommonResponse.aspx.cs" Inherits="QryptoCard.APIDocs.Introduction.CommonResponse" %>

<asp:Content ID="Content1" ContentPlaceHolderID="TitleContent" runat="server">
    <!--begin::Page title-->
    <div class="page-title d-flex flex-column me-3">
        <!--begin::Title-->
        <h1 class="d-flex text-gray-900 fw-bold my-1 fs-3">Common Response</h1>
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
                        <span class="text-active-dark mt-1 fw-semibold fs-6">All of response in this API would be same except not authorization status code. And here is the detail :</span>
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
                        <div class="card card-dashed mb-4">
                            <div class="card-header collapsible cursor-pointer rotate" data-bs-toggle="collapse" data-bs-target="#kt_docs_card_collapsible1">
                                <h3 class="card-title"><span>RESPONSE</span></h3>
                                <div class="card-toolbar rotate-180">
                                    <i class="ki-duotone ki-down fs-1"></i>
                                </div>
                            </div>
                            <div id="kt_docs_card_collapsible1" class="collapse show">
                                <div class="card-body">
                                    <div class="row">
                                        <div class="col-lg-9">
                                            <span class="text-active-dark mt-1 fw-semibold fs-6">Status</span>&nbsp;&nbsp;<span class="text-gray-500 mt-1 fw-semibold fs-6">string</span>&nbsp;&nbsp;<span class="text-danger mt-1 fs-6">required</span>
                                            <br />
                                            <span class="text-active-dark mt-1 fs-7">Status of the request</span>

                                        </div>
                                        <div class="col-lg-3  text-end">
                                            <select class="form-select" aria-label="Select example">
                                                <option>Open this for value option</option>
                                                <option value="1">success = your request is success</option>
                                                <option value="2">error = something error with your request body</option>
                                                <option value="3">failed = a request is failed</option>
                                            </select>
                                        </div>
                                    </div>
                                    <hr />

                                    <span class="text-active-dark mt-1 fw-semibold fs-6">Message</span>&nbsp;&nbsp;<span class="text-gray-500 mt-1 fw-semibold fs-6">string</span>&nbsp;&nbsp;<span class="text-danger mt-1 fs-6">required</span>
                                    <br />
                                    <span class="text-active-dark mt-1 fs-7">Contains message of the status</span>
                                    <br />
                                    <hr />

                                    <span class="text-active-dark mt-1 fw-semibold fs-6">Data</span>&nbsp;&nbsp;<span class="text-gray-500 mt-1 fw-semibold fs-6">object</span>&nbsp;&nbsp;<span class="text-danger mt-1 fs-6">required</span>
                                    <br />
                                    <span class="text-active-dark mt-1 fs-7"> Contains the data if the status is success, and all the response below will contain in this parameter</span>
                                    <br />
                                </div>
                            </div>
                        </div>
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

