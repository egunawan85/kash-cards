<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="newpassword.aspx.cs" Inherits="QryptoCard.Dashboard.newpassword" %>

<!DOCTYPE html>

<html lang="en">
<head>
    <base href="../../../" />
    <title>Set a new password - Kash</title>
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1" />
    <meta name="robots" content="noindex, nofollow" />
    <link rel="shortcut icon" href='<%= ResolveUrl("~/Content/media/landing/kash-logo.png") %>' type="image/png" />
    <link rel="preconnect" href="https://fonts.googleapis.com" />
    <link rel="preconnect" href="https://fonts.gstatic.com" crossorigin />
    <link href="https://fonts.googleapis.com/css2?family=Sora:wght@400;500;600;700;800&family=Hanken+Grotesk:wght@400;500;600;700&family=JetBrains+Mono:wght@400;500;700&display=swap" rel="stylesheet" />
    <link rel="stylesheet" href='<%= ResolveUrl("~/Content/css/kash-auth.css") %>' />
    <script>// Frame-busting to prevent site from being loaded within a frame without permission (click-jacking) if (window.top != window.self) { window.top.location.replace(window.self.location.href); }</script>
</head>
<body>
    <div class="atmosphere"><span class="aura a1"></span><span class="aura a2"></span></div>

    <div class="auth">
        <aside class="auth-aside">
            <span class="glow-blob"></span>
            <a class="brand" href="/"><img class="brand-logo" src='<%= ResolveUrl("~/Content/media/landing/kash-logo.png") %>' alt="Kash logo" /> Kash</a>

            <div class="auth-mini-card">
                <div class="card-3d" style="width: 100%;">
                    <div class="qcard">
                        <div class="qcard-inner">
                            <div class="qcard-top"><div class="qcard-brand">K<b>ash</b></div><span class="qcard-tag">Virtual</span></div>
                            <div><div class="qcard-chip"></div></div>
                            <div class="qcard-num"><span>4929</span><span>&#8226;&#8226;&#8226;&#8226;</span><span>&#8226;&#8226;&#8226;&#8226;</span><span>8317</span></div>
                            <div class="qcard-bottom">
                                <div><div class="lab">Card holder</div><div class="val">SATOSHI N.</div></div>
                                <div><div class="lab">Funded with</div><div class="val">USDT</div></div>
                                <div class="qcard-logo">K</div>
                            </div>
                        </div>
                    </div>
                </div>
            </div>

            <div class="auth-quote">
                <h2>One step to<br><span class="grad">back in business.</span></h2>
                <p>Choose a strong new password and you're ready to spend again.</p>
                <div class="auth-points" style="margin-top: 28px;">
                    <div class="auth-point"><div class="n">256-bit</div><div class="l">Encryption</div></div>
                    <div class="auth-point"><div class="n">KYC</div><div class="l">Compliant</div></div>
                    <div class="auth-point"><div class="n">24/7</div><div class="l">Monitoring</div></div>
                </div>
            </div>
        </aside>

        <main class="auth-main">
            <div class="auth-form-wrap">
                <a class="back" href="login"><svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M19 12H5M11 18l-6-6 6-6" /></svg> Back to sign in</a>
                <h1>Set a new password</h1>
                <p class="sub">Choose a new password for your account.</p>

                <form runat="server" class="form">
                    <asp:HiddenField runat="server" ID="hfID" />
                    <div class="auth-error" runat="server" id="divfailed" visible="false">
                        <span><asp:Label runat="server" ID="lblFailed" Text="Error message" /></span>
                        <button type="button" class="x" runat="server" id="btnfailed" onserverclick="btnfailed_ServerClick" aria-label="Dismiss">&times;</button>
                    </div>

                    <div class="field" runat="server" id="divpassword">
                        <label for="txtPassword">New password</label>
                        <div class="ctrl">
                            <input type="password" runat="server" id="txtPassword" placeholder="Create a strong password" name="confirm-password" autocomplete="new-password" data-strength-input="data-strength-input" />
                            <button type="button" class="toggle" data-toggle-pw aria-label="Show password"><svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8"><path d="M2 12s3.6-7 10-7 10 7 10 7-3.6 7-10 7-10-7-10-7z" /><circle cx="12" cy="12" r="3" /></svg></button>
                        </div>
                        <div class="strength" data-strength="data-strength"><span></span><span></span><span></span><span></span></div>
                    </div>
                    <div class="field" runat="server" id="divpasswordconfirm">
                        <label for="txtPasswordConfirm">Repeat password</label>
                        <div class="ctrl"><input type="password" runat="server" id="txtPasswordConfirm" placeholder="Re-enter your password" name="confirm-password" autocomplete="new-password" /></div>
                    </div>

                    <div class="auth-note" runat="server" id="divfinish" visible="false">
                        Your password has been changed. Please sign in again.
                    </div>

                    <button type="button" runat="server" id="btnSubmit" onserverclick="btnSubmit_ServerClick" class="btn btn-primary btn-block btn-lg">Update password</button>
                    <a href="login" runat="server" id="btnLogin" class="btn btn-primary btn-block btn-lg" visible="false">Back to sign in</a>
                </form>

                <p class="auth-foot">Already reset it? <a href="login">Sign in</a></p>
            </div>
        </main>
    </div>

    <script src='<%= ResolveUrl("~/Content/js/kash-auth.js") %>'></script>
</body>
</html>
