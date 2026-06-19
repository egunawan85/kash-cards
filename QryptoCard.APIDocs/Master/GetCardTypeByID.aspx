<%@ Page Title="" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="GetCardTypeByID.aspx.cs" Inherits="QryptoCard.APIDocs.Master.GetCardTypeByID" %>


<asp:Content ID="Content1" ContentPlaceHolderID="TitleContent" runat="server">
    <!--begin::Page title-->
    <div class="page-title d-flex flex-column me-3">
        <!--begin::Title-->
        <h1 class="d-flex text-gray-900 fw-bold my-1 fs-3">GetCardTypeByID</h1>
        <!--end::Title-->
        <!--begin::Breadcrumb-->
        <ul class="breadcrumb breadcrumb-dot fw-semibold text-gray-600 fs-7 my-1">
            <!--begin::Item-->
            <li class="breadcrumb-item text-gray-600"><span class="badge badge-primary badge-sm">POST</span></li>
            <!--end::Item-->
            <!--begin::Item-->
            <li class="breadcrumb-item text-gray-800">https://api-dev.kash.cards/v1/master/card/type/detail</li>
            <!--end::Item-->
        </ul>
        <!--end::Breadcrumb-->
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
                        <span class="text-active-dark mt-1 fw-semibold fs-6">This API used to get card type detail by id.</span>
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
                            <div class="card-header collapsible cursor-pointer rotate" data-bs-toggle="collapse" data-bs-target="#kt_docs_card_collapsible">
                                <h3 class="card-title"><span>BODY PARAMS</span></h3>
                                <div class="card-toolbar rotate-180">
                                    <i class="ki-duotone ki-down fs-1"></i>
                                </div>
                            </div>
                            <div id="kt_docs_card_collapsible" class="collapse show">
                                <div class="card-body">
                                    <span class="text-active-dark mt-1 fw-semibold fs-6">CardTypeId</span>&nbsp;&nbsp;<span class="text-gray-500 mt-1 fw-semibold fs-6">long</span>&nbsp;&nbsp;<span class="text-danger mt-1 fs-6">required</span>
                                    <br />
                                    <span class="text-active-dark mt-1 fs-7">Unique ID to identify the card type</span>
                                    <br />

                                </div>
                            </div>
                        </div>
                        <div class="card card-dashed mb-4">
                            <div class="card-header collapsible cursor-pointer rotate" data-bs-toggle="collapse" data-bs-target="#kt_docs_card_collapsible1">
                                <h3 class="card-title"><span>RESPONSE</span></h3>
                                <div class="card-toolbar rotate-180">
                                    <i class="ki-duotone ki-down fs-1"></i>
                                </div>
                            </div>
                            <div id="kt_docs_card_collapsible1" class="collapse show">
                                <div class="card-body">
                                    <span class="text-active-dark mt-1 fw-semibold fs-6">Status</span>&nbsp;&nbsp;<span class="text-gray-500 mt-1 fw-semibold fs-6">string</span>&nbsp;&nbsp;<span class="text-danger mt-1 fs-6">required</span>
                                    <br />
                                    <span class="text-active-dark mt-1 fs-7">Status of the request</span>
                                    <br />
                                    <hr />

                                    <span class="text-active-dark mt-1 fw-semibold fs-6">Message</span>&nbsp;&nbsp;<span class="text-gray-500 mt-1 fw-semibold fs-6">string</span>&nbsp;&nbsp;<span class="text-danger mt-1 fs-6">required</span>
                                    <br />
                                    <span class="text-active-dark mt-1 fs-7">Contains message of the status</span>
                                    <br />
                                    <hr />

                                    <span class="text-active-dark mt-1 fw-semibold fs-6">Data</span>&nbsp;&nbsp;<span class="text-gray-500 mt-1 fw-semibold fs-6">object</span>&nbsp;&nbsp;<span class="text-danger mt-1 fs-6">required</span>
                                    <br />
                                    <div class="card card-dashed mt-4 mb-4">
                                        <div id="kt_docs_card_collapsible2" class="collapse show">
                                            <div class="card-body">
                                                <span class="text-active-dark mt-1 fw-semibold fs-6">CardTypeId</span>&nbsp;&nbsp;<span class="text-gray-500 mt-1 fw-semibold fs-6">long</span>&nbsp;&nbsp;<span class="text-danger mt-1 fs-6">required</span>
                                                <br />
                                                <span class="text-active-dark mt-1 fs-7">Unique ID to identify the card type</span>
                                                <br />
                                                <hr />
                                                <span class="text-active-dark mt-1 fw-semibold fs-6">Organization</span>&nbsp;&nbsp;<span class="text-gray-500 mt-1 fw-semibold fs-6">string</span>&nbsp;&nbsp;<span class="text-danger mt-1 fs-6">required</span>
                                                <br />
                                                <span class="text-active-dark mt-1 fs-7">Organization of the card</span>
                                                <br />
                                                <hr />
                                                <span class="text-active-dark mt-1 fw-semibold fs-6">Country</span>&nbsp;&nbsp;<span class="text-gray-500 mt-1 fw-semibold fs-6">string</span>&nbsp;&nbsp;<span class="text-danger mt-1 fs-6">required</span>
                                                <br />
                                                <span class="text-active-dark mt-1 fs-7">Country of the card</span>
                                                <br />
                                                <hr />
                                                <span class="text-active-dark mt-1 fw-semibold fs-6">BankCardBin</span>&nbsp;&nbsp;<span class="text-gray-500 mt-1 fw-semibold fs-6">string</span>&nbsp;&nbsp;<span class="text-danger mt-1 fs-6">required</span>
                                                <br />
                                                <span class="text-active-dark mt-1 fs-7">Bank card bin</span>
                                                <br />
                                                <hr />
                                                <span class="text-active-dark mt-1 fw-semibold fs-6">Type</span>&nbsp;&nbsp;<span class="text-gray-500 mt-1 fw-semibold fs-6">string</span>&nbsp;&nbsp;<span class="text-danger mt-1 fs-6">required</span>
                                                <br />
                                                <span class="text-active-dark mt-1 fs-7">Card type</span>
                                                <br />
                                                <hr />
                                                <span class="text-active-dark mt-1 fw-semibold fs-6">CardName</span>&nbsp;&nbsp;<span class="text-gray-500 mt-1 fw-semibold fs-6">string</span>&nbsp;&nbsp;<span class="text-danger mt-1 fs-6">required</span>
                                                <br />
                                                <span class="text-active-dark mt-1 fs-7">Card name</span>
                                                <br />
                                                <hr />
                                                <span class="text-active-dark mt-1 fw-semibold fs-6">CardDesc</span>&nbsp;&nbsp;<span class="text-gray-500 mt-1 fw-semibold fs-6">string</span>&nbsp;&nbsp;<span class="text-danger mt-1 fs-6">required</span>
                                                <br />
                                                <span class="text-active-dark mt-1 fs-7">Card description</span>
                                                <br />
                                                <hr />
                                                <span class="text-active-dark mt-1 fw-semibold fs-6">CardPrice</span>&nbsp;&nbsp;<span class="text-gray-500 mt-1 fw-semibold fs-6">string</span>&nbsp;&nbsp;<span class="text-danger mt-1 fs-6">required</span>
                                                <br />
                                                <span class="text-active-dark mt-1 fs-7">Card price</span>
                                                <br />
                                                <hr />
                                                <span class="text-active-dark mt-1 fw-semibold fs-6">CardPriceCurrency</span>&nbsp;&nbsp;<span class="text-gray-500 mt-1 fw-semibold fs-6">string</span>&nbsp;&nbsp;<span class="text-danger mt-1 fs-6">required</span>
                                                <br />
                                                <span class="text-active-dark mt-1 fs-7">Card price currency</span>
                                                <br />
                                                <hr />
                                                <span class="text-active-dark mt-1 fw-semibold fs-6">RechargeFeeRate</span>&nbsp;&nbsp;<span class="text-gray-500 mt-1 fw-semibold fs-6">string</span>&nbsp;&nbsp;<span class="text-danger mt-1 fs-6">required</span>
                                                <br />
                                                <span class="text-active-dark mt-1 fs-7">Recharge fee rate</span>
                                                <br />
                                                <hr />
                                                <span class="text-active-dark mt-1 fw-semibold fs-6">RechargeFee</span>&nbsp;&nbsp;<span class="text-gray-500 mt-1 fw-semibold fs-6">string</span>&nbsp;&nbsp;<span class="text-danger mt-1 fs-6">required</span>
                                                <br />
                                                <span class="text-active-dark mt-1 fs-7">Recharge fee</span>
                                                <br />
                                                <hr />
                                                <span class="text-active-dark mt-1 fw-semibold fs-6">Support</span>&nbsp;&nbsp;<span class="text-gray-500 mt-1 fw-semibold fs-6">string</span>&nbsp;&nbsp;<span class="text-danger mt-1 fs-6">required</span>
                                                <br />
                                                <span class="text-active-dark mt-1 fs-7">Card supported vendor/merchant</span>
                                                <br />
                                                <hr />
                                                <span class="text-active-dark mt-1 fw-semibold fs-6">SupportHolderRegin</span>&nbsp;&nbsp;<span class="text-gray-500 mt-1 fw-semibold fs-6">string</span>&nbsp;&nbsp;<span class="text-danger mt-1 fs-6">required</span>
                                                <br />
                                                <span class="text-active-dark mt-1 fs-7">Fee of the Support holder region</span>
                                                <br />
                                                <hr />
                                                <span class="text-active-dark mt-1 fw-semibold fs-6">SupportHolderAreaCode</span>&nbsp;&nbsp;<span class="text-gray-500 mt-1 fw-semibold fs-6">string</span>&nbsp;&nbsp;
    <br />
                                                <span class="text-active-dark mt-1 fs-7">Support holder area code</span>
                                                <br />
                                                <hr />
                                                <div class="row">
                                                    <div class="col-lg-9">
                                                        <span class="text-active-dark mt-1 fw-semibold fs-6">NeedCardHolder</span>&nbsp;&nbsp;<span class="text-gray-500 mt-1 fw-semibold fs-6">int32</span>&nbsp;&nbsp;<span class="text-danger mt-1 fs-6">required</span>
                                                        <br />
                                                        <span class="text-active-dark mt-1 fs-7">Flag for is this card need cardholder or not</span>

                                                    </div>
                                                    <div class="col-lg-3  text-end">
                                                        <select class="form-select" aria-label="Select example">
                                                            <option>Open this for value option</option>
                                                            <option value="1">0 = no</option>
                                                            <option value="2">1 = yes</option>
                                                        </select>
                                                    </div>
                                                </div>
                                                <hr />
                                                <div class="row">
                                                    <div class="col-lg-9">
                                                        <span class="text-active-dark mt-1 fw-semibold fs-6">NeedDepositForActiveCard</span>&nbsp;&nbsp;<span class="text-gray-500 mt-1 fw-semibold fs-6">int32</span>&nbsp;&nbsp;<span class="text-danger mt-1 fs-6">required</span>
                                                        <br />
                                                        <span class="text-active-dark mt-1 fs-7">Flag for this card is need deposit amount or not</span>

                                                    </div>
                                                    <div class="col-lg-3  text-end">
                                                        <select class="form-select" aria-label="Select example">
                                                            <option>Open this for value option</option>
                                                            <option value="1">0 = no</option>
                                                            <option value="2">1 = yes</option>
                                                        </select>
                                                    </div>
                                                </div>
                                                <hr />


                                                <div class="row">
                                                    <div class="col-lg-9">
                                                        <span class="text-active-dark mt-1 fw-semibold fs-6">isActive</span>&nbsp;&nbsp;<span class="text-gray-500 mt-1 fw-semibold fs-6">int32</span>&nbsp;&nbsp;<span class="text-danger mt-1 fs-6">required</span>
                                                        <br />
                                                        <span class="text-active-dark mt-1 fs-7">Flag for active status</span>

                                                    </div>
                                                    <div class="col-lg-3  text-end">
                                                        <select class="form-select" aria-label="Select example">
                                                            <option>Open this for value option</option>
                                                            <option value="1">0 = inactive</option>
                                                            <option value="2">1 = active</option>
                                                        </select>
                                                    </div>
                                                </div>
                                                <hr />

                                                <div class="row">
                                                    <div class="col-lg-9">
                                                        <span class="text-active-dark mt-1 fw-semibold fs-6">Status</span>&nbsp;&nbsp;<span class="text-gray-500 mt-1 fw-semibold fs-6">string</span>&nbsp;&nbsp;<span class="text-danger mt-1 fs-6">required</span>
                                                        <br />
                                                        <span class="text-active-dark mt-1 fs-7">Status of the card</span>

                                                    </div>
                                                    <div class="col-lg-3  text-end">
                                                        <select class="form-select" aria-label="Select example">
                                                            <option>Open this for value option</option>
                                                            <option value="1">online = the card type is online</option>
                                                            <option value="2">offline = the card type is offline</option>
                                                        </select>
                                                    </div>
                                                </div>
                                                <hr />

                                                <span class="text-active-dark mt-1 fw-semibold fs-6">DateCreated</span>&nbsp;&nbsp;<span class="text-gray-500 mt-1 fw-semibold fs-6">datetime</span>&nbsp;&nbsp;<span class="text-danger mt-1 fs-6">required</span>
                                                <br />
                                                <span class="text-active-dark mt-1 fs-7">Created date of the transaction (yyyy-MM-dd hh:mm:ss.fff)</span>
                                                <br />
                                                <hr />


                                            </div>
                                        </div>
                                    </div>
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

