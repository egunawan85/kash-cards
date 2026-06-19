<%@ Page Title="" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="TransactionWebhooks.aspx.cs" Inherits="QryptoCard.APIDocs.Webhook.TransactionWebhooks" %>

<asp:Content ID="Content1" ContentPlaceHolderID="TitleContent" runat="server">
    <!--begin::Page title-->
    <div class="page-title d-flex flex-column me-3">
        <!--begin::Title-->
        <h1 class="d-flex text-gray-900 fw-bold my-1 fs-3">Transaction webhooks</h1>
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
                <!--begin::Body-->
                <div class="card-body pt-7 px-0">
                    <div class="mb-2 px-9">
                        <div class="card card-dashed mb-4">
                            <div class="card-header">
                                <h3 class="card-title"><span><b>Subscribe to events using webhooks</b></span></h3>
                            </div>
                            <div class="card-body">
                                An event is a notification about any update in a process. In KashCards, events occur when there is an update on the transactions. These real-time updates are notified to you, as a Virtual Asset Service Provider (VASP). This ensures that you are always informed about the confirmation and completion of your customers’ transactions.
                                <br />
                                <br />
                                <%--The following flowchart illustrates the flow of the webhook and the actions you can take based on them.
                                <br />
                                <br />
                                <img alt="Logo" src='<%= ResolveUrl("~/assets/media/knwebhook.png")%>' class="w-50" />--%>
                                <br />
                                <br />
                                The above flowchart is described as in the following steps:
                                <ol style="text-align: justify" type="1">
                                    <li>Your customer (User) initiates a transaction.</li>
                                    <li>KashCards sends a webhook request to your webhook URL. </li>
                                    <li>You call the get transaction (purchase/deposit) API to check the status of the transaction.</li>
                                    <li>You validate the API response with the webhook request data.</li>
                                    <li>If confirmed, you credit to your customer’s card.</li>
                                </ol>

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


