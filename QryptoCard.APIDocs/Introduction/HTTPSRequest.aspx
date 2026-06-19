<%@ Page Title="" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="HTTPSRequest.aspx.cs" Inherits="QryptoCard.APIDocs.Introduction.HTTPSRequest" %>

<asp:Content ID="Content1" ContentPlaceHolderID="TitleContent" runat="server">
    <!--begin::Page title-->
    <div class="page-title d-flex flex-column me-3">
        <!--begin::Title-->
        <h1 class="d-flex text-gray-900 fw-bold my-1 fs-3">HTTP(S) Request</h1>
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
                        <span class="text-active-dark mt-1 fw-semibold fs-6">KashCards API can be requested through HTTP(S) Request to KashCards Base URL endpoint. The HTTP(S) Header has to be used to allow proper authentication.</span>
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
                    <div class="mb-2 px-9">
                        <div class="card card-dashed mb-4">
                            <div class="card-header">
                                <h3 class="card-title"><span><b>API Base URL</b></span></h3>
                            </div>
                            <div class="card-body">
                                
                                <div class="d-flex flex-column">
                                    <li class="d-flex align-items-center py-2">
                                        <span class="bullet bullet-dot me-5"></span>Development Environment : <b>https://api-dev.kash.cards</b>
                                    </li>
                                    <li class="d-flex align-items-center py-2">
                                        <span class="bullet bullet-dot me-5"></span>Production Environment : <b>to be confirmed</b>
                                    </li>
                                </div>
                            </div>
                        </div>

                    </div>
                    <div class="mb-2 px-9">
                        <div class="card card-dashed mb-4">
                            <div class="card-header">
                                <h3 class="card-title"><span><b>HTTP(S) Header</b></span></h3>
                            </div>
                            <div class="card-body">

                                <div class="table-responsive">
                                    <table class="table table-row-dashed table-row-gray-300 gy-7">
                                        <thead>
                                            <tr class="fw-bold fs-6 text-gray-800">
                                                <th>Header</th>
                                                <th>Value</th>
                                                <th>Definition</th>
                                            </tr>
                                        </thead>
                                        <tbody>
                                            <tr>
                                                <td>Content-Type</td>
                                                <td>application/json</td>
                                                <td>The Content-Type field indicates that JSON type is acceptable to send to the recipient</td>
                                            </tr>
                                            <tr>
                                                <td>Accept</td>
                                                <td>application/json</td>
                                                <td>The Accept field is used to specify that JSON type is acceptable for the response</td>
                                            </tr>
                                            <tr>
                                                <td>Authorization</td>
                                                <td>Basic AUTH_STRING</td>
                                                <td>The Authorization field, please lookup at the authorization header below</td>
                                            </tr>
                                        </tbody>
                                    </table>
                                </div>
                            </div>
                        </div>

                    </div>
                    <div class="mb-2 px-9">
                        <div class="card card-dashed mb-4">
                            <div class="card-header">
                                <h3 class="card-title"><span><b>Content-Type and Accept Header</b></span></h3>
                            </div>
                            <div class="card-body">

                                <span class="fs-6">In KashCards API, the input and output parameters of the methods will be in JSON format. To accept JSON input and output parameters, you need to add the following HTTP(S) header:</span>
                                <br />
                                <br />
                                <div class="d-flex flex-column">
                                    <li class="d-flex align-items-center py-2">
                                        <span class="bullet bullet-dot me-5"></span>Content-Type: application/json
                                    </li>
                                    <li class="d-flex align-items-center py-2">
                                        <span class="bullet bullet-dot me-5"></span>Accept: application/json
                                    </li>
                                </div>
                            </div>
                        </div>
                    </div>
                    <div class="mb-2 px-9">
                        <div class="card card-dashed mb-4">
                            <div class="card-header">
                                <h3 class="card-title"><span><b>Authorization Header</b></span></h3>
                            </div>
                            <div class="card-body">

                                <span class="fs-6">The authorization header utilizes API Key following HTTP(S) BASIC AUTH convention: </span>
                                <br />
                                <br />
                                Authorization: Basic AUTH_STRING
                                <br />
                                AUTH_STRING = Base64(<b>API_KEY</b> + : + <b>SECRET_KEY</b>) 
                                <br />
                                <br />
                                KashCards validates HTTP request by using Basic Authentication method. You can generate your <b>API key</b> and <b>Secret key</b> on the dashboard.
                            </div>
                        </div>
                    </div>
                    <div class="mb-2 px-9">
                        <div class="card card-dashed mb-4">
                            <div class="card-header">
                                <h3 class="card-title"><span><b>Request Body</b></span></h3>
                            </div>
                            <div class="card-body">

                                <span class="fs-6">In every request body, the <span class="text-danger mt-1 fs-6">required</span> stands for mandatory. </span>
                            </div>
                        </div>
                    </div>
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


