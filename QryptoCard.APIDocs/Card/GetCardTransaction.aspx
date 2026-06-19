<%@ Page Title="" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="GetCardTransaction.aspx.cs" Inherits="QryptoCard.APIDocs.Card.GetCardTransaction" %>

<asp:Content ID="Content1" ContentPlaceHolderID="TitleContent" runat="server">
    <!--begin::Page title-->
    <div class="page-title d-flex flex-column me-3">
        <!--begin::Title-->
        <h1 class="d-flex text-gray-900 fw-bold my-1 fs-3">Get Card Transaction</h1>
        <!--end::Title-->
        <!--begin::Breadcrumb-->
        <ul class="breadcrumb breadcrumb-dot fw-semibold text-gray-600 fs-7 my-1">
            <!--begin::Item-->
            <li class="breadcrumb-item text-gray-600"><span class="badge badge-primary badge-sm">POST</span></li>
            <!--end::Item-->
            <!--begin::Item-->
            <li class="breadcrumb-item text-gray-800">https://api-dev.kash.cards/v1/card/transaction</li>
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
                        <span class="text-active-dark mt-1 fw-semibold fs-6">This API used to get card transaction.</span>
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
                                    <span class="text-active-dark mt-1 fw-semibold fs-6">ID</span>&nbsp;&nbsp;<span class="text-gray-500 mt-1 fw-semibold fs-6">long</span>&nbsp;&nbsp;<span class="text-danger mt-1 fs-6">required</span>
                                    <br />
                                    <span class="text-active-dark mt-1 fs-7">Unique ID to identify the card</span>
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

                                    <span class="text-active-dark mt-1 fw-semibold fs-6">Data</span>&nbsp;&nbsp;<span class="text-gray-500 mt-1 fw-semibold fs-6">array of object</span>&nbsp;&nbsp;<span class="text-danger mt-1 fs-6">required</span>
                                    <br />
                                    <div class="card card-dashed mt-4 mb-4">
                                        <div id="kt_docs_card_collapsible2" class="collapse show">
                                            <div class="card-body">
                                                <span class="text-active-dark mt-1 fw-semibold fs-6">ID</span>&nbsp;&nbsp;<span class="text-gray-500 mt-1 fw-semibold fs-6">long</span>&nbsp;&nbsp;<span class="text-danger mt-1 fs-6">required</span>
                                                <br />
                                                <span class="text-active-dark mt-1 fs-7">Unique ID to identify the transaction</span>
                                                <br />
                                                <hr />
                                                <span class="text-active-dark mt-1 fw-semibold fs-6">CardNo</span>&nbsp;&nbsp;<span class="text-gray-500 mt-1 fw-semibold fs-6">string</span>&nbsp;&nbsp;<span class="text-danger mt-1 fs-6">required</span>
                                                <br />
                                                <span class="text-active-dark mt-1 fs-7">Card no</span>
                                                <br />
                                                <hr />
                                                <span class="text-active-dark mt-1 fw-semibold fs-6">TradeNo</span>&nbsp;&nbsp;<span class="text-gray-500 mt-1 fw-semibold fs-6">string</span>&nbsp;&nbsp;<span class="text-danger mt-1 fs-6">required</span>
                                                <br />
                                                <span class="text-active-dark mt-1 fs-7">Trade no</span>
                                                <br />
                                                <hr />
                                                <span class="text-active-dark mt-1 fw-semibold fs-6">OriginTradeNo</span>&nbsp;&nbsp;<span class="text-gray-500 mt-1 fw-semibold fs-6">string</span>&nbsp;&nbsp;<span class="text-danger mt-1 fs-6">required</span>
                                                <br />
                                                <span class="text-active-dark mt-1 fs-7">Origin trade no</span>
                                                <br />
                                                <hr />
                                                <span class="text-active-dark mt-1 fw-semibold fs-6">Currency</span>&nbsp;&nbsp;<span class="text-gray-500 mt-1 fw-semibold fs-6">string</span>&nbsp;&nbsp;<span class="text-danger mt-1 fs-6">required</span>
                                                <br />
                                                <span class="text-active-dark mt-1 fs-7">Currency of transaction</span>
                                                <br />
                                                <hr />
                                                <span class="text-active-dark mt-1 fw-semibold fs-6">Amount</span>&nbsp;&nbsp;<span class="text-gray-500 mt-1 fw-semibold fs-6">double</span>&nbsp;&nbsp;<span class="text-danger mt-1 fs-6">required</span>
                                                <br />
                                                <span class="text-active-dark mt-1 fs-7">Amount</span>
                                                <br />
                                                <hr />
                                                <span class="text-active-dark mt-1 fw-semibold fs-6">AuthorizedAmount</span>&nbsp;&nbsp;<span class="text-gray-500 mt-1 fw-semibold fs-6">double</span>&nbsp;&nbsp;<span class="text-danger mt-1 fs-6">required</span>
                                                <br />
                                                <span class="text-active-dark mt-1 fs-7">Authorized amount</span>
                                                <br />
                                                <hr />
                                                <span class="text-active-dark mt-1 fw-semibold fs-6">AuthorizedCurrency</span>&nbsp;&nbsp;<span class="text-gray-500 mt-1 fw-semibold fs-6">string</span>&nbsp;&nbsp;<span class="text-danger mt-1 fs-6">required</span>
                                                <br />
                                                <span class="text-active-dark mt-1 fs-7">Authorized currency</span>
                                                <br />
                                                <hr />
                                                <span class="text-active-dark mt-1 fw-semibold fs-6">Fee</span>&nbsp;&nbsp;<span class="text-gray-500 mt-1 fw-semibold fs-6">double</span>&nbsp;&nbsp;<span class="text-danger mt-1 fs-6">required</span>
                                                <br />
                                                <span class="text-active-dark mt-1 fs-7">Fee of transaction</span>
                                                <br />
                                                <hr />
                                                <span class="text-active-dark mt-1 fw-semibold fs-6">FeeCurrency</span>&nbsp;&nbsp;<span class="text-gray-500 mt-1 fw-semibold fs-6">string</span>&nbsp;&nbsp;<span class="text-danger mt-1 fs-6">required</span>
                                                <br />
                                                <span class="text-active-dark mt-1 fs-7">Fee currency</span>
                                                <br />
                                                <hr />
                                                <span class="text-active-dark mt-1 fw-semibold fs-6">CrossBoardFee</span>&nbsp;&nbsp;<span class="text-gray-500 mt-1 fw-semibold fs-6">double</span>&nbsp;&nbsp;<span class="text-danger mt-1 fs-6">required</span>
                                                <br />
                                                <span class="text-active-dark mt-1 fs-7">Cross board fee</span>
                                                <br />
                                                <hr />
                                                <span class="text-active-dark mt-1 fw-semibold fs-6">CrossBoardFeeCurrency</span>&nbsp;&nbsp;<span class="text-gray-500 mt-1 fw-semibold fs-6">string</span>&nbsp;&nbsp;<span class="text-danger mt-1 fs-6">required</span>
                                                <br />
                                                <span class="text-active-dark mt-1 fs-7">Cross board fee currency</span>
                                                <br />
                                                <hr />
                                                <span class="text-active-dark mt-1 fw-semibold fs-6">SettleAmount</span>&nbsp;&nbsp;<span class="text-gray-500 mt-1 fw-semibold fs-6">double</span>&nbsp;&nbsp;<span class="text-danger mt-1 fs-6">required</span>
                                                <br />
                                                <span class="text-active-dark mt-1 fs-7">Settle amount</span>
                                                <br />
                                                <hr />
                                                <span class="text-active-dark mt-1 fw-semibold fs-6">SettleCurrency</span>&nbsp;&nbsp;<span class="text-gray-500 mt-1 fw-semibold fs-6">string</span>&nbsp;&nbsp;<span class="text-danger mt-1 fs-6">required</span>
                                                <br />
                                                <span class="text-active-dark mt-1 fs-7">Settle currency</span>
                                                <br />
                                                <hr />
                                                <span class="text-active-dark mt-1 fw-semibold fs-6">SettleDate</span>&nbsp;&nbsp;<span class="text-gray-500 mt-1 fw-semibold fs-6">string</span>&nbsp;&nbsp;<span class="text-danger mt-1 fs-6">required</span>
                                                <br />
                                                <span class="text-active-dark mt-1 fs-7">Settle date</span>
                                                <br />
                                                <hr />
                                                <span class="text-active-dark mt-1 fw-semibold fs-6">MerchantName</span>&nbsp;&nbsp;<span class="text-gray-500 mt-1 fw-semibold fs-6">string</span>&nbsp;&nbsp;<span class="text-danger mt-1 fs-6">required</span>
                                                <br />
                                                <span class="text-active-dark mt-1 fs-7">Merchant name</span>
                                                <br />
                                                <hr />
                                                <span class="text-active-dark mt-1 fw-semibold fs-6">Type</span>&nbsp;&nbsp;<span class="text-gray-500 mt-1 fw-semibold fs-6">string</span>&nbsp;&nbsp;<span class="text-danger mt-1 fs-6">required</span>
                                                <br />
                                                <span class="text-active-dark mt-1 fs-7">Type</span>
                                                <br />
                                                <hr />
                                                <span class="text-active-dark mt-1 fw-semibold fs-6">Status</span>&nbsp;&nbsp;<span class="text-gray-500 mt-1 fw-semibold fs-6">string</span>&nbsp;&nbsp;<span class="text-danger mt-1 fs-6">required</span>
                                                <br />
                                                <span class="text-active-dark mt-1 fs-7">Status</span>
                                                <br />
                                                <hr />
                                                <span class="text-active-dark mt-1 fw-semibold fs-6">Description</span>&nbsp;&nbsp;<span class="text-gray-500 mt-1 fw-semibold fs-6">string</span>&nbsp;&nbsp;<span class="text-danger mt-1 fs-6">required</span>
                                                <br />
                                                <span class="text-active-dark mt-1 fs-7">Description</span>
                                                <br />
                                                <hr />
                                                <span class="text-active-dark mt-1 fw-semibold fs-6">TransactionTime</span>&nbsp;&nbsp;<span class="text-gray-500 mt-1 fw-semibold fs-6">string</span>&nbsp;&nbsp;<span class="text-danger mt-1 fs-6">required</span>
                                                <br />
                                                <span class="text-active-dark mt-1 fs-7">Transaction time</span>
                                                <br />

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

