<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="otpregister.aspx.cs" Inherits="QryptoCard.Dashboard.otpregister" %>

<!DOCTYPE html>

<html lang="en">
<head>
    <base href="../../../" />
    <title>Verify your email - Kash</title>
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1" />
    <meta name="robots" content="noindex, nofollow" />
    <link rel="shortcut icon" href='<%= ResolveUrl("~/Content/media/landing/kash-logo.png") %>' type="image/png" />
    <link rel="preconnect" href="https://fonts.googleapis.com" />
    <link rel="preconnect" href="https://fonts.gstatic.com" crossorigin />
    <link href="https://fonts.googleapis.com/css2?family=Sora:wght@400;500;600;700;800&family=Hanken+Grotesk:wght@400;500;600;700&family=JetBrains+Mono:wght@400;500;700&display=swap" rel="stylesheet" />
    <link rel="stylesheet" href='<%= ResolveUrl("~/Content/css/landing.css") %>' />
    <link rel="stylesheet" href='<%= ResolveUrl("~/Content/css/app.css") %>' />
    <script>// Frame-busting to prevent site from being loaded within a frame without permission (click-jacking) if (window.top != window.self) { window.top.location.replace(window.self.location.href); }</script>
</head>
<body>
    <div class="bg-fx"></div>
    <div class="grain"></div>

    <div class="otp-wrap">
        <a class="brand otp-home" href="/"><img class="brand-logo" src='<%= ResolveUrl("~/Content/media/landing/kash-logo.png") %>' alt="Kash logo" /> Kash</a>

        <div class="otp-card">
            <div class="otp-icon">
                <svg width="28" height="28" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.7"><rect x="3" y="5" width="18" height="14" rx="2" /><path d="M3 7l9 6 9-6" /></svg>
            </div>
            <h1>Verify your email</h1>
            <p class="otp-sub">Enter the 6-digit code we sent to confirm your new account.</p>

            <form runat="server" class="form">
                <div class="otp-banner fail" runat="server" id="divfailed" visible="false">
                    <span><asp:Label runat="server" ID="lblFailed" Text="Error message" /></span>
                    <button type="button" class="x" runat="server" id="btnfailed" onserverclick="btnfailed_ServerClick" aria-label="Dismiss">&times;</button>
                </div>
                <div class="otp-banner ok" runat="server" id="divsuccess" visible="false">
                    <span><asp:Label runat="server" ID="lblSuccess" Text="Success message" /></span>
                    <button type="button" class="x" runat="server" id="btnsuccess" onserverclick="btnsuccess_ServerClick" aria-label="Dismiss">&times;</button>
                </div>

                <div class="otp-inputs" id="otp">
                    <input type="text" runat="server" id="icode1" inputmode="numeric" autocomplete="one-time-code" maxlength="1" aria-label="Digit 1" value="" />
                    <input type="text" runat="server" id="icode2" inputmode="numeric" maxlength="1" aria-label="Digit 2" value="" />
                    <input type="text" runat="server" id="icode3" inputmode="numeric" maxlength="1" aria-label="Digit 3" value="" />
                    <input type="text" runat="server" id="icode4" inputmode="numeric" maxlength="1" aria-label="Digit 4" value="" />
                    <input type="text" runat="server" id="icode5" inputmode="numeric" maxlength="1" aria-label="Digit 5" value="" />
                    <input type="text" runat="server" id="icode6" inputmode="numeric" maxlength="1" aria-label="Digit 6" value="" />
                </div>

                <div class="otp-actions">
                    <asp:Button runat="server" ID="btnAuthX" OnClick="btnAuth_ServerClick" CssClass="btn btn-cyan btn-block btn-lg" Text="Verify and continue" OnClientClick="this.disabled=true;" UseSubmitBehavior="false" />
                </div>

                <p class="otp-resend">Didn't get the code? <asp:LinkButton runat="server" ID="lbtResendOTP" OnClick="lbtResendOTP_Click">Resend</asp:LinkButton></p>
            </form>

            <p class="otp-foot">Wrong email? <a href="register">Back to sign up</a></p>
        </div>
    </div>

    <script type="text/javascript">
        // Auto-advance, backspace, and paste-to-fill across the six server-rendered OTP inputs.
        (function () {
            var ids = ['<%= icode1.ClientID %>', '<%= icode2.ClientID %>', '<%= icode3.ClientID %>', '<%= icode4.ClientID %>', '<%= icode5.ClientID %>', '<%= icode6.ClientID %>'];
            var inputs = ids.map(function (id) { return document.getElementById(id); }).filter(Boolean);
            if (!inputs.length) return;
            function focusAt(i) { if (inputs[i]) inputs[i].focus(); }
            inputs.forEach(function (input, idx) {
                input.addEventListener('input', function () {
                    input.value = input.value.replace(/\D/g, '').slice(-1);
                    input.classList.toggle('filled', !!input.value);
                    if (input.value && idx < inputs.length - 1) focusAt(idx + 1);
                });
                input.addEventListener('keydown', function (e) {
                    if (e.key === 'Backspace' && !input.value && idx > 0) focusAt(idx - 1);
                    else if (e.key === 'ArrowLeft' && idx > 0) { e.preventDefault(); focusAt(idx - 1); }
                    else if (e.key === 'ArrowRight' && idx < inputs.length - 1) { e.preventDefault(); focusAt(idx + 1); }
                });
                input.addEventListener('paste', function (e) {
                    e.preventDefault();
                    var digits = (e.clipboardData || window.clipboardData).getData('text').replace(/\D/g, '').slice(0, 6);
                    if (!digits) return;
                    digits.split('').forEach(function (d, k) { if (inputs[k]) { inputs[k].value = d; inputs[k].classList.add('filled'); } });
                    focusAt(Math.min(digits.length, inputs.length - 1));
                });
            });
            focusAt(0);
        })();
    </script>
</body>
</html>
