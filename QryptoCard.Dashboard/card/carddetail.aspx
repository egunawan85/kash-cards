<%@ Page Title="Buy Card" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="carddetail.aspx.cs" Inherits="QryptoCard.Dashboard.card.carddetail" %>

<%@ MasterType VirtualPath="~/Site.Master" %>

<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" runat="server">
    <asp:HiddenField runat="server" ID="hfIsHolderNeeded" />
    <asp:HiddenField runat="server" ID="hfReferralLink" />
    <asp:HiddenField runat="server" ID="hfCardTypeID" />
    <asp:HiddenField runat="server" ID="hfHolderID" />
    <asp:HiddenField runat="server" ID="hfCardData" />
    <asp:HiddenField runat="server" ID="hfMinDeposit" />
    <asp:HiddenField runat="server" ID="hfMaxDeposit" />

    <style>
        .cd-grid { display: grid; grid-template-columns: 1.2fr 1fr; gap: 20px; align-items: start; }
        @media (max-width: 980px) { .cd-grid { grid-template-columns: 1fr; } }

        /* Card visual — mirrors the catalog tile, with the per-card-type artwork. */
        .cd-card { border-radius: 18px; padding: 22px; color: #eaf6f8; min-height: 196px; display: flex; flex-direction: column; justify-content: space-between; background: linear-gradient(135deg, #0c121a, #16222e 55%, #0b2b32); background-size: cover; background-position: center; border: 1px solid var(--line-2); text-shadow: 0 1px 3px rgba(0, 0, 0, .55); margin-bottom: 22px; }
        .cd-card-top { display: flex; justify-content: flex-end; min-height: 28px; }
        .cd-card-price { font-family: var(--font-display); font-weight: 700; font-size: 1.5rem; }
        .cd-card-bin { font-family: var(--font-mono); letter-spacing: .08em; font-size: 1.1rem; color: var(--cyan-bright); margin: 22px 0; }
        .cd-card-foot { display: flex; justify-content: space-between; align-items: flex-end; gap: 12px; font-size: .82rem; color: #cdd8e2; }
        .cd-card-logos { text-align: right; white-space: nowrap; }
        .cd-card-logos img { height: 26px; margin-left: 6px; vertical-align: middle; }

        /* Form blocks */
        .cd-block { border: 1px solid var(--line); border-radius: 14px; padding: 18px; margin-bottom: 18px; background: rgba(255, 255, 255, .02); }
        .cd-block-h { font-family: var(--font-display); font-weight: 600; font-size: 1rem; margin-bottom: 14px; }
        .cd-row2 { display: grid; grid-template-columns: 1fr 1fr; gap: 12px; }
        @media (max-width: 560px) { .cd-row2 { grid-template-columns: 1fr; } }
        .cd-input { width: 100%; padding: .8em 1em; margin-bottom: 12px; border-radius: 12px; border: 1px solid var(--line-2); background: rgba(255, 255, 255, .03); color: var(--ink); font-size: .92rem; outline: none; transition: border-color .2s, box-shadow .2s; }
        .cd-input:focus { border-color: var(--cyan); box-shadow: 0 0 0 4px rgba(0, 230, 255, .12); }
        .cd-input::placeholder { color: var(--ink-faint); }
        .cd-newholder { margin-top: 2px; }
        .cd-newholder a { color: var(--cyan-deep); font-weight: 600; font-size: .86rem; cursor: pointer; }
        .cd-newholder a:hover { color: var(--cyan-bright); }

        /* Deposit amount */
        .cd-amount { display: flex; align-items: stretch; border: 1px solid var(--line-2); border-radius: 12px; overflow: hidden; background: rgba(255, 255, 255, .03); margin-bottom: 14px; }
        .cd-amount-cur { display: flex; align-items: center; padding: 0 14px; background: rgba(255, 255, 255, .04); color: var(--ink-3); font-family: var(--font-mono); font-size: .82rem; border-right: 1px solid var(--line-2); }
        .cd-amount-input { flex: 1; border: none; background: transparent; margin: 0; border-radius: 0; }
        .cd-amount-input:focus { box-shadow: none; }
        .cd-chips { display: grid; grid-template-columns: repeat(5, 1fr); gap: 10px; }
        @media (max-width: 560px) { .cd-chips { grid-template-columns: repeat(3, 1fr); } }
        .cd-chip { width: 100%; padding: .7em .4em; font-size: .9rem; }

        /* Fee summary */
        .cd-summary { border: 1px solid var(--line); border-radius: 14px; padding: 4px 18px; margin-bottom: 20px; }
        .cd-sum-row { display: flex; justify-content: space-between; align-items: center; padding: 12px 0; border-bottom: 1px solid var(--line); font-size: .92rem; color: var(--ink-2); }
        .cd-sum-row:last-child { border-bottom: none; }
        .cd-sum-v { font-family: var(--font-mono); font-weight: 600; color: var(--ink); }
        .cd-sum-total { font-weight: 700; }
        .cd-sum-total .cd-sum-v { color: var(--cyan-bright); }
        .cd-buy { width: 100%; }

        /* Info panel */
        .cd-info-block { margin-bottom: 22px; }
        .cd-info-block:last-child { margin-bottom: 0; }
        .cd-info-h { font-family: var(--font-display); font-weight: 600; font-size: 1rem; margin-bottom: 8px; }
        .cd-info-text { color: var(--ink-3); font-size: .9rem; line-height: 1.5; display: block; }
        .cd-info-list { color: var(--ink-3); font-size: .9rem; line-height: 1.55; padding-left: 18px; margin: 0; }
        .cd-info-list li { margin-bottom: 6px; }

        /* Error modal — markup/JS unchanged; brought onto the NewDesign palette. */
        #alertModal .modal-content { background: var(--panel); border: 1px solid var(--line); border-radius: 16px; color: var(--ink); }
        #alertModal .modal-header, #alertModal .modal-footer { border-color: var(--line); }
        #alertModal .modal-title { font-family: var(--font-display); }
    </style>

    <!--begin::Page header-->
    <div class="dash-top">
        <div>
            <h1>Card Detail</h1>
            <div class="sub">Review the card and fund it from your wallet balance.</div>
        </div>
    </div>
    <!--end::Page header-->

    <div class="cd-grid">
        <!--begin::Buy form-->
        <section class="panel">
            <!-- Card visual -->
            <div class="cd-card" style="background-image: linear-gradient(135deg, rgba(8, 14, 22, .72), rgba(7, 28, 36, .58)), url(<%= CardArtUrl() %>);">
                <div class="cd-card-top">
                    <span class="cd-card-price"><span runat="server" id="lblCardPrice">0 USD</span></span>
                </div>
                <div class="cd-card-bin"><span runat="server" id="lblCardBin">0000</span></div>
                <div class="cd-card-foot">
                    <div>
                        <div>Deposit Fee : <span runat="server" id="lblDepositFeeRate">0%</span></div>
                        <div>Deposit Limit : <span runat="server" id="lblDepositLimit">0 USD</span></div>
                    </div>
                    <div class="cd-card-logos">
                        <img runat="server" id="icgpay" src="https://www.svgrepo.com/show/508690/google-pay.svg" alt="Chip" width="40" visible="false">
                        <img runat="server" id="icapay" src="https://www.svgrepo.com/show/508402/apple-pay.svg" alt="Chip" width="40" visible="false">
                        <img runat="server" id="imgOrg" alt="Chip" width="40">
                    </div>
                </div>
            </div>

            <!-- Cardholder (shown only when the card type requires KYC) -->
            <div class="cd-block" runat="server" id="viewCardholder" visible="false">
                <div class="cd-block-h">Cardholder</div>
                <div class="cd-row2">
                    <input type="text" runat="server" id="txtFirstName" placeholder="First name" autocomplete="off" class="cd-input" />
                    <input type="text" runat="server" id="txtLastName" placeholder="Last name" autocomplete="off" class="cd-input" />
                </div>
                <input type="text" runat="server" id="txtEmail" placeholder="Email (for verification code)" autocomplete="off" class="cd-input" />
                <div class="cd-newholder">
                    <asp:LinkButton runat="server" ID="lbtNewHolder" Text="Use new email" OnClick="lbtNewHolder_Click" />
                </div>
            </div>

            <!-- Deposit amount -->
            <div class="cd-block" runat="server" id="Div1">
                <div class="cd-block-h">Deposit amount</div>
                <div class="cd-amount">
                    <span class="cd-amount-cur">USD</span>
                    <asp:TextBox CssClass="cd-input cd-amount-input" runat="server" ID="txtDepositAmount" aria-label="Amount (to the nearest dollar)" TextMode="Number" AutoPostBack="true" OnTextChanged="txtDepositAmount_TextChanged" />
                </div>
                <div class="cd-chips">
                    <div runat="server" id="viewrc20">
                        <button class="btn btn-line cd-chip" id="rc20" runat="server" onserverclick="rc20_ServerClick">$20</button>
                    </div>
                    <div runat="server" id="viewrc30">
                        <button class="btn btn-line cd-chip" id="rc30" runat="server" onserverclick="rc30_ServerClick">$30</button>
                    </div>
                    <div>
                        <button class="btn btn-line cd-chip" id="rc50" runat="server" onserverclick="rc50_ServerClick">$50</button>
                    </div>
                    <div>
                        <button class="btn btn-line cd-chip" id="rc100" runat="server" onserverclick="rc100_ServerClick">$100</button>
                    </div>
                    <div>
                        <button class="btn btn-line cd-chip" id="rc200" runat="server" onserverclick="rc200_ServerClick">$200</button>
                    </div>
                </div>
            </div>

            <!-- Fee summary -->
            <div class="cd-summary">
                <div class="cd-sum-row">
                    <span>Card Fee</span>
                    <span class="cd-sum-v"><span runat="server" id="lblCardFeeX">$</span></span>
                </div>
                <div class="cd-sum-row">
                    <span>Deposit Amount</span>
                    <span class="cd-sum-v"><span runat="server" id="lblMinDepositX">$</span></span>
                </div>
                <div class="cd-sum-row">
                    <span>Deposit Fee Rate</span>
                    <span class="cd-sum-v"><span runat="server" id="lblDepositFeeRateX">$</span></span>
                </div>
                <div class="cd-sum-row">
                    <span>Deposit Fee</span>
                    <span class="cd-sum-v"><span runat="server" id="lblDepositFeeX">$</span></span>
                </div>
                <div class="cd-sum-row cd-sum-total">
                    <span>Total Payment</span>
                    <span class="cd-sum-v"><span runat="server" id="lblTotalX">$</span></span>
                </div>
            </div>

            <asp:Button runat="server" ID="btnBuyX" OnClick="btnBuy_ServerClick" CssClass="btn btn-cyan btn-lg cd-buy" Text="Buy" OnClientClick="this.disabled=true;" UseSubmitBehavior="false" />
        </section>
        <!--end::Buy form-->

        <!--begin::Info-->
        <section class="panel">
            <div class="cd-info-block">
                <div class="cd-info-h">Supported Usage Scenarios</div>
                <span class="cd-info-text" runat="server" id="lblUsage"></span>
            </div>
            <div class="cd-info-block">
                <div class="cd-info-h">Fees</div>
                <span class="cd-info-text" runat="server" id="lblDepositFee"></span>
                <span class="cd-info-text" runat="server" id="lblCardFee"></span>
            </div>
            <div class="cd-info-block">
                <div class="cd-info-h">Credit Limits</div>
                <ol class="cd-info-list">
                    <li><span runat="server" id="lblMinDeposit"></span></li>
                    <li><span runat="server" id="lblMaxDeposit"></span></li>
                    <li><span runat="server" id="lblMaxCardPurchase"></span></li>
                    <li><span runat="server" id="lblInitialDepositRequired"></span></li>
                    <li><span runat="server" id="lblMinInitialDepositAmount"></span></li>
                </ol>
            </div>
            <div class="cd-info-block">
                <div class="cd-info-h">Card Usage Notice</div>
                <ol class="cd-info-list">
                    <li>If the card issuer detects malicious activities such as bulk refunds, cancellations, failures, or chargebacks during card usage, the card will be automatically frozen, and a fee of 10 USD per occurrence will be deducted.</li>
                    <li>If the overall transaction failure rate exceeds 20%, the card will be automatically frozen.</li>
                    <li>If the card has insufficient balance and accumulates up to 3 consecutive authorization failures, the card will be automatically canceled.</li>
                    <li>If the card is frozen, please contact customer service to apply for unfreezing.</li>
                    <li>The card is valid for 3 years.</li>
                </ol>
            </div>
        </section>
        <!--end::Info-->
    </div>
</asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderID="DrawerContent" runat="server">
</asp:Content>
<asp:Content ID="Content3" ContentPlaceHolderID="ModalContent" runat="server">
    <div class="modal fade" tabindex="-1" id="alertModal" aria-hidden="true">
        <div class="modal-dialog">
            <div class="modal-content">
                <div class="modal-header">
                    <h3 class="modal-title">Error</h3>

                    <!--begin::Close-->
                    <div class="btn btn-icon btn-sm btn-active-light-primary ms-2" data-bs-dismiss="modal" aria-label="Close">
                        <i class="ki-duotone ki-cross fs-1"><span class="path1"></span><span class="path2"></span></i>
                    </div>
                    <!--end::Close-->
                </div>

                <div class="modal-body">
                    <label class="form-label" runat="server" id="lblalert"></label>
                </div>

                <div class="modal-footer">
                    <button class="btn btn-danger" type="button" data-bs-dismiss="modal">Close</button>
                </div>
            </div>
        </div>
    </div>
</asp:Content>
<asp:Content ID="Content4" ContentPlaceHolderID="ScriptContent" runat="server">
    <script type="text/javascript">
        $(window).load(function () {

            $("input[id*='txtduedate]").datepicker({
                format: "yyyy/mm/dd"

            });
            //$("input[id*='txtStartDate]").datepicker({
            //    format: "yyyy/mm/dd"

            //});
        });

        $(window).load(function () {
            $('#myModalAdd').modal({ backdrop: 'static', keyboard: false });
            if (window.isModalAdd) {
                $('#myModalAdd').modal('show');
            }
            else {
                $('#myModalAdd').modal('hide');
            }
            console.log(window.isModalAdd);
            console.log('this is another debugging message');
        });

        $(window).load(function () {
            //$('#alertModal').modal({ backdrop: 'static', keyboard: false });
            if (window.isModalAlert) {
                $('#alertModal').modal('show');
            }
            else {
                $('#alertModal').modal('hide');
            }
            console.log(window.isModalAlert);
            console.log('this is another debugging message');
        });



        $(window).load(function () {
            if (window.isModalPopup) {
                $('#popupModal').modal('show');
                var myModal = $('#popupModal');
                clearTimeout(myModal.data('hideInterval'));
                myModal.data('hideInterval', setTimeout(function () {
                    myModal.modal('hide');
                }, 1000));
            }
            else {
                $('#popupModal').modal('hide');
            }
            console.log(window.isModalPopup);
            console.log('this is another debugging for popup');
        });

        <%--function copyReferralCode() {
            var copyText = document.getElementById("<%= hfReferralCode.ClientID %>");
            //copyText.select();
            //document.execCommand("copy");
            navigator.clipboard.writeText(copyText.value).then(() => { }).catch((error) => { })
            console.log("copytext =>" + copyText.value);
            alert("Copied to clipboard: " + copyText.value);
        }--%>

        function copyReferralLink() {
            var copyText = document.getElementById("<%= hfReferralLink.ClientID %>");
            //copyText.select();
            //document.execCommand("copy");
            navigator.clipboard.writeText(copyText.value).then(() => { }).catch((error) => { })
            console.log("copytext =>" + copyText.value);
            alert("Copied to clipboard: " + copyText.value);
        }


        function copyAddress(addr) {
            navigator.clipboard.writeText(addr).then(() => { }).catch((error) => { })
        }
    </script>
</asp:Content>
