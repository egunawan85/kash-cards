<%@ Page Title="" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="WebhookRequests.aspx.cs" Inherits="QryptoCard.APIDocs.Webhook.WebhookRequests" %>

<asp:Content ID="Content1" ContentPlaceHolderID="TitleContent" runat="server">
    <!--begin::Page title-->
    <div class="page-title d-flex flex-column me-3">
        <!--begin::Title-->
        <h1 class="d-flex text-gray-900 fw-bold my-1 fs-3">Receive webhook requests</h1>
        <!--end::Title-->
        <!--begin::Breadcrumb-->
        <%--<ul class="breadcrumb breadcrumb-dot fw-semibold text-gray-600 fs-7 my-1">
            <!--begin::Item-->
            <li class="breadcrumb-item text-gray-600"><span class="badge badge-primary badge-sm">POST</span></li>
            <!--end::Item-->
            <!--begin::Item-->
            <li class="breadcrumb-item text-gray-800">https://api-dev.runegate.co/v1/transfer</li>
            <!--end::Item-->
        </ul>--%>
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
                        <span class="text-active-dark mt-1 fw-semibold fs-6">The structure of the Receive webhook requests contains several fields that provide information about the transaction.</span>
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
                                <h3 class="card-title"><span>The following is a sample payload of the webhook request.</span></h3>
                                <div class="card-toolbar rotate-180">
                                    <i class="ki-duotone ki-down fs-1"></i>
                                </div>
                            </div>
                            <div id="kt_docs_card_collapsible1" class="collapse show">
                                <div class="card-body">
                                    <span class="text-active-dark mt-1 fw-semibold fs-6">ID</span>&nbsp;&nbsp;<span class="text-gray-500 mt-1 fw-semibold fs-6">string</span>&nbsp;&nbsp;<span class="text-danger mt-1 fs-6">required</span>
                                    <br />
                                    <span class="text-active-dark mt-1 fs-7">Unique ID to identify the transaction</span>
                                    <br />
                                    <hr />
                                    <div class="row">
                                        <div class="col-lg-9">
                                            <span class="text-active-dark mt-1 fw-semibold fs-6">Type</span>&nbsp;&nbsp;<span class="text-gray-500 mt-1 fw-semibold fs-6">string</span>&nbsp;&nbsp;<span class="text-danger mt-1 fs-6">required</span>
                                            <br />
                                            <span class="text-active-dark mt-1 fs-7">Type of the transaction</span>

                                        </div>
                                        <div class="col-lg-3  text-end">
                                            <select class="form-select" aria-label="Select example">
                                                <option>Open this for value option</option>
                                                <option value="1">Purchase = transaction from card's purchase</option>
                                                <option value="2">Deposit = transaction from card's deposit</option>
                                            </select>
                                        </div>
                                    </div>
                                    <hr />
                                    <span class="text-active-dark mt-1 fw-semibold fs-6">Amount</span>&nbsp;&nbsp;<span class="text-gray-500 mt-1 fw-semibold fs-6">double</span>&nbsp;&nbsp;<span class="text-danger mt-1 fs-6">required</span>
                                    <br />
                                    <span class="text-active-dark mt-1 fs-7">Amount of the transaction</span>
                                    <br />
                                    <hr />
                                    <span class="text-active-dark mt-1 fw-semibold fs-6">Currency</span>&nbsp;&nbsp;<span class="text-gray-500 mt-1 fw-semibold fs-6">string</span>&nbsp;&nbsp;<span class="text-danger mt-1 fs-6">required</span>
                                    <br />
                                    <span class="text-active-dark mt-1 fs-7">Currency that belongs to the transaction/payment</span>
                                    <br />
                                    <hr />
                                    <span class="text-active-dark mt-1 fw-semibold fs-6">Fee</span>&nbsp;&nbsp;<span class="text-gray-500 mt-1 fw-semibold fs-6">double</span>&nbsp;&nbsp;<span class="text-danger mt-1 fs-6">required</span>
                                    <br />
                                    <span class="text-active-dark mt-1 fs-7">Fee of the transaction that charged by KashCards</span>
                                    <br />
                                    <hr />
                                    <span class="text-active-dark mt-1 fw-semibold fs-6">FeeInPercentage</span>&nbsp;&nbsp;<span class="text-gray-500 mt-1 fw-semibold fs-6">double</span>&nbsp;&nbsp;<span class="text-danger mt-1 fs-6">required</span>
                                    <br />
                                    <span class="text-active-dark mt-1 fs-7">Amount of the fee's percentage that KashCards charged to merchant per transaction</span>
                                    <br />
                                    <hr />
                                    <span class="text-active-dark mt-1 fw-semibold fs-6">Total</span>&nbsp;&nbsp;<span class="text-gray-500 mt-1 fw-semibold fs-6">double</span>&nbsp;&nbsp;<span class="text-danger mt-1 fs-6">required</span>
                                    <br />
                                    <span class="text-active-dark mt-1 fs-7">Total amount that merchant will receive in merchant's balance</span>
                                    <br />
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
                                            <span class="text-active-dark mt-1 fs-7">Status of the trsnsaction</span>

                                        </div>
                                        <div class="col-lg-3  text-end">
                                            <select class="form-select" aria-label="Select example">
                                                <option>Open this for value option</option>
                                                <option value="1">created = transaction created</option>
                                                <option value="2">paid = transaction was paid</option>
                                                <option value="2">success = transaction was completed</option>
                                                <option value="3">expired = transaction expired due to no payment made from customer</option>
                                                <option value="4">cancelled = transaction cancelled from merchant's side</option>
                                            </select>
                                        </div>
                                    </div>
                                    <hr />

                                    <span class="text-active-dark mt-1 fw-semibold fs-6">DateCreated</span>&nbsp;&nbsp;<span class="text-gray-500 mt-1 fw-semibold fs-6">datetime</span>&nbsp;&nbsp;<span class="text-danger mt-1 fs-6">required</span>
                                    <br />
                                    <span class="text-active-dark mt-1 fs-7">Created date of the transaction (yyyy-MM-dd hh:mm:ss.fff)</span>
                                    <br />
                                    <hr />

                                    <span class="text-active-dark mt-1 fw-semibold fs-6">UserReferenceID</span>&nbsp;&nbsp;<span class="text-gray-500 mt-1 fw-semibold fs-6">string</span>&nbsp;&nbsp;<span class="text-danger mt-1 fs-6">required</span>
                                    <br />
                                    <span class="text-active-dark mt-1 fs-7">Unique ID from user to identify the transaction</span>
                                    <br />
                                    <hr />
                                    <span class="text-active-dark mt-1 fw-semibold fs-6">Address</span>&nbsp;&nbsp;<span class="text-gray-500 mt-1 fw-semibold fs-6">string</span>&nbsp;&nbsp;<span class="text-danger mt-1 fs-6">required</span>
                                    <br />
                                    <span class="text-active-dark mt-1 fs-7">Blockchain address for transaction/payment</span>
                                    <br />
                                    <hr />

                                    <span class="text-active-dark mt-1 fw-semibold fs-6">ReceiptURL</span>&nbsp;&nbsp;<span class="text-gray-500 mt-1 fw-semibold fs-6">string</span>&nbsp;&nbsp;<span class="text-danger mt-1 fs-6">required</span>
                                    <br />
                                    <span class="text-active-dark mt-1 fs-7">Receipt URL from the network provider. This receipt URL will appear if the transaction has been paid</span>
                                    <br />
                                    <hr />
                                    <span class="text-active-dark mt-1 fw-semibold fs-6">Txhash</span>&nbsp;&nbsp;<span class="text-gray-500 mt-1 fw-semibold fs-6">string</span>&nbsp;&nbsp;<span class="text-danger mt-1 fs-6">required</span>
                                    <br />
                                    <span class="text-active-dark mt-1 fs-7">Transaction hash of blockchain transaction</span>
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


