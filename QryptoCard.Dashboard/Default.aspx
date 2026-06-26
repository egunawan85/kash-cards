<%@ Page Title="Home Page" Language="C#" AutoEventWireup="true" CodeBehind="Default.aspx.cs" Inherits="QryptoCard.Dashboard._Default" %>

<%--MasterPageFile="~/Site.Master"--%>

<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="UTF-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1.0" />
    <link rel="icon" type="image/png" href="Content/media/landing/kash-logo.png" />
    <link rel="apple-touch-icon" href="Content/media/landing/kash-logo.png" />
    <meta name="theme-color" content="#05070a" />
    <title>Kash &mdash; Top up with USDT. Spend like cash.</title>
    <meta name="description" content="Kash is the virtual card you top up with USDT and spend anywhere. Instant conversion at checkout. One flat 3% top-up fee. Bank-grade security." />
    <meta name="robots" content="index, follow" />
    <link rel="preconnect" href="https://fonts.googleapis.com" />
    <link rel="preconnect" href="https://fonts.gstatic.com" crossorigin />
    <link href="https://fonts.googleapis.com/css2?family=Sora:wght@400;500;600;700;800&family=Hanken+Grotesk:wght@400;500;600;700&family=JetBrains+Mono:wght@400;500;700&display=swap" rel="stylesheet" />
    <link rel="stylesheet" href='<%= ResolveUrl("~/Content/css/landing.css") %>' />
    <script src="https://cdnjs.cloudflare.com/ajax/libs/gsap/3.12.5/gsap.min.js"></script>
    <script src="https://cdnjs.cloudflare.com/ajax/libs/gsap/3.12.5/ScrollTrigger.min.js"></script>
    <script src="https://cdn.jsdelivr.net/npm/lenis@1.1.13/dist/lenis.min.js"></script>
</head>
<body>
    <form runat="server">
        <div class="bg-fx"></div>
        <div class="grain"></div>

        <!-- NAV -->
        <header class="nav">
            <div class="wrap nav-inner">
                <a class="brand" href="/"><img class="brand-logo" src="Content/media/landing/kash-logo.png" alt="Kash logo" /> Kash</a>
                <nav class="nav-links">
                    <a href="#features">Features</a>
                    <a href="#security">Security</a>
                    <a href="#how">How it works</a>
                    <a href="#faq">FAQ</a>
                </nav>
                <div class="nav-cta">
                    <a class="btn btn-line" href="login" runat="server" id="btnLogin">Sign in</a>
                    <a class="btn btn-cyan" href="register" runat="server" id="btnRegister">Get your card
                        <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M5 12h14M13 6l6 6-6 6" /></svg></a>
                    <a class="btn btn-cyan" href="dashboard" runat="server" id="btnDashboard">Open Dashboard
                        <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M5 12h14M13 6l6 6-6 6" /></svg></a>
                </div>
            </div>
        </header>

        <!-- HERO -->
        <section class="hero">
            <div class="hero-bg" style="background-image: url('Content/media/landing/bg-texture.png')" role="presentation"></div>
            <div class="wrap hero-grid">
                <div class="hero-copy">
                    <span class="hero-badge"><span class="dot"></span> 12,400+ already on the waitlist</span>
                    <h1>Top up with USDT.<br>Spend like <span class="grad">cash.</span></h1>
                    <p class="lede">Kash is the virtual card you load with USDT and spend anywhere online &mdash; converted to fiat the instant you check out. One flat fee, zero guesswork.</p>
                    <div class="hero-actions">
                        <a class="btn btn-cyan btn-lg" href="register" runat="server" id="txtGetStarted">Get your Kash card
                            <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M5 12h14M13 6l6 6-6 6" /></svg></a>
                        <a class="btn btn-line btn-lg" href="#how">See how it works</a>
                    </div>
                    <p class="hero-note"><svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M20 6L9 17l-5-5" /></svg> Simple 3% top-up fee, charged upfront. Nothing hidden.</p>
                    <div class="hero-stats">
                        <div class="hero-stat"><div class="n"><span data-count="3" data-suffix="%">0%</span></div><div class="l">Flat top-up fee</div></div>
                        <div class="hero-stat"><div class="n">&lt;1s</div><div class="l">USDT to fiat</div></div>
                        <div class="hero-stat"><div class="n"><span data-count="40" data-suffix="M+">0</span></div><div class="l">Merchants worldwide</div></div>
                    </div>
                </div>
                <div class="hero-card">
                    <img src="Content/media/landing/hero-card.png" alt="The Kash virtual card, a glossy black metallic card with a cyan edge glow" />
                </div>
            </div>
            <div class="scroll-cue"><span class="mouse"></span> Scroll</div>
        </section>

        <!-- TRUST STRIP -->
        <div class="trust">
            <div class="wrap trust-inner">
                <div class="trust-item"><svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8"><path d="M12 2l8 3v6c0 5-3.4 8.5-8 11-4.6-2.5-8-6-8-11V5l8-3z" /><path d="M9 12l2 2 4-4" /></svg> Regulated &amp; compliant</div>
                <span class="trust-sep"></span>
                <div class="trust-item"><svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8"><rect x="4" y="10" width="16" height="11" rx="2" /><path d="M8 10V7a4 4 0 018 0v3" /></svg> 256-bit encryption</div>
                <span class="trust-sep"></span>
                <div class="trust-item"><svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8"><path d="M3 7l9-4 9 4-9 4-9-4z" /><path d="M3 7v6l9 4 9-4V7" /></svg> Funds in segregated custody</div>
                <span class="trust-sep"></span>
                <div class="trust-net"><b>USDT</b> &middot; <b>Apple&nbsp;Pay</b> &middot; <b>Google&nbsp;Pay</b> &middot; <b>Visa</b></div>
            </div>
        </div>

        <!-- FEATURES BENTO -->
        <section class="features" id="features">
            <div class="wrap">
                <div class="sec-head reveal">
                    <span class="eyebrow">Why Kash</span>
                    <h2 class="h2">A card built around<br>one simple idea.</h2>
                    <p>Load it with USDT. Spend it like any card. No exchanges to babysit, no confusing fees.</p>
                </div>
                <div class="bento">
                    <article class="tile tile-img tall col-4 row-2 reveal">
                        <div class="ph" style="background-image: url('Content/media/landing/lifestyle-pay.png')"></div>
                        <div class="ov"></div>
                        <div class="cap">
                            <span class="tag">Spend anywhere</span>
                            <h3>Tap to pay, in the real world</h3>
                            <p>Add Kash to Apple&nbsp;Pay or Google&nbsp;Pay and pay at 40M+ merchants. Your USDT converts to fiat the instant the terminal beeps.</p>
                        </div>
                    </article>
                    <article class="tile tile-img col-2 reveal" data-d="1" style="min-height: 260px;">
                        <div class="ph" style="background-image: url('Content/media/landing/feat-usdt.png')"></div>
                        <div class="ov"></div>
                        <div class="cap"><span class="tag">Funding</span><h3>Fund with USDT</h3><p>Top up straight from USDT &mdash; the only asset you need.</p></div>
                    </article>
                    <article class="tile tile-img col-2 reveal" data-d="2" style="min-height: 260px;">
                        <div class="ph" style="background-image: url('Content/media/landing/card-detail.png')"></div>
                        <div class="ov"></div>
                        <div class="cap"><span class="tag">One flat fee</span><div class="big-num grad">3%</div><p>Charged upfront. No monthly fee, no hidden FX.</p></div>
                    </article>
                    <article class="tile tile-img col-2 reveal" data-d="1" style="min-height: 260px;">
                        <div class="ph" style="background-image: url('Content/media/landing/app-phone.png'); background-position: center 30%"></div>
                        <div class="ov"></div>
                        <div class="cap"><span class="tag">Your control center</span><h3>Manage it from your phone</h3></div>
                    </article>
                    <article class="tile tile-img col-2 reveal" data-d="3" style="min-height: 260px;">
                        <div class="ph" style="background-image: url('Content/media/landing/feat-virtual.png')"></div>
                        <div class="ov"></div>
                        <div class="cap"><span class="tag">The card</span><h3>Virtual, in 60s</h3><p>Instant issuance &mdash; no plastic to wait for.</p></div>
                    </article>
                    <article class="tile tile-img col-2 reveal" data-d="4" style="min-height: 260px;">
                        <div class="ph" style="background-image: url('Content/media/landing/feat-speed.png')"></div>
                        <div class="ov"></div>
                        <div class="cap"><span class="tag">Speed</span><h3>Instant conversion</h3><p>Live rate, locked the moment your payment clears.</p></div>
                    </article>
                </div>
            </div>
        </section>

        <!-- SPLIT: instant conversion (macro card) -->
        <section>
            <div class="wrap split">
                <div class="split-media reveal"><img src="Content/media/landing/card-detail.png" alt="Macro close-up of the Kash card surface, gold chip and cyan edge light" /></div>
                <div class="split-copy reveal" data-d="1">
                    <span class="eyebrow">Instant conversion</span>
                    <h2 class="h2">Crypto in.<br>Fiat out. Instantly.</h2>
                    <p>The moment you pay, Kash converts exactly what you need from your USDT balance at the live rate and settles in fiat &mdash; locked the second the payment clears.</p>
                    <div class="ticks">
                        <div class="tick"><svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M20 6L9 17l-5-5" /></svg> Live mid-market rate at checkout</div>
                        <div class="tick"><svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M20 6L9 17l-5-5" /></svg> No slippage surprises after you pay</div>
                        <div class="tick"><svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M20 6L9 17l-5-5" /></svg> Works online and in-store via Apple/Google Pay</div>
                    </div>
                </div>
            </div>
        </section>

        <!-- SECURITY -->
        <section id="security">
            <div class="wrap split rev">
                <div class="split-media reveal"><img src="Content/media/landing/security.png" alt="A glowing cyan security shield protecting the Kash card" /></div>
                <div class="split-copy reveal" data-d="1">
                    <span class="eyebrow">Security first</span>
                    <h2 class="h2">Built to protect<br>your money.</h2>
                    <p>Crypto should feel safer than cash, not scarier. Every Kash account is hardened end-to-end.</p>
                    <div class="ticks">
                        <div class="tick"><svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M20 6L9 17l-5-5" /></svg> Full KYC &amp; regulatory compliance</div>
                        <div class="tick"><svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M20 6L9 17l-5-5" /></svg> Funds held in segregated custody</div>
                        <div class="tick"><svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M20 6L9 17l-5-5" /></svg> 256-bit encryption &amp; 24/7 on-chain monitoring</div>
                        <div class="tick"><svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M20 6L9 17l-5-5" /></svg> Freeze or kill the card instantly from your phone</div>
                    </div>
                </div>
            </div>
        </section>

        <!-- HOW IT WORKS -->
        <section class="steps" id="how">
            <div class="wrap">
                <div class="split rev" style="padding-bottom: 40px;">
                    <div class="split-media reveal"><img src="Content/media/landing/app-phone.png" alt="The Kash app showing a USDT balance and Top Up button" /></div>
                    <div class="split-copy reveal" data-d="1">
                        <span class="eyebrow">How it works</span>
                        <h2 class="h2">Live in three steps.</h2>
                        <p>From sign-up to spending in under a minute &mdash; all from the app.</p>
                    </div>
                </div>
                <div class="steps-grid">
                    <article class="step reveal" data-d="1">
                        <div class="step-ph" style="background-image: url('Content/media/landing/app-phone.png'); background-position: center top"></div>
                        <div class="step-body">
                            <div class="num"></div>
                            <h3>Create your account</h3>
                            <p>Sign up and breeze through verification. Your virtual card is issued the moment you're approved.</p>
                        </div>
                    </article>
                    <article class="step reveal" data-d="2">
                        <div class="step-ph" style="background-image: url('Content/media/landing/feat-usdt.png')"></div>
                        <div class="step-body">
                            <div class="num"></div>
                            <h3>Top up with USDT</h3>
                            <p>Send USDT to your Kash balance. The fee is applied upfront, so the amount you see is the amount you can spend.</p>
                            <span class="fee">3% top-up fee, charged once at top-up</span>
                        </div>
                    </article>
                    <article class="step reveal" data-d="3">
                        <div class="step-ph" style="background-image: url('Content/media/landing/lifestyle-pay.png')"></div>
                        <div class="step-body">
                            <div class="num"></div>
                            <h3>Spend anywhere</h3>
                            <p>Add the card to Apple Pay or Google Pay and pay at 40M+ online merchants. USDT converts to fiat instantly.</p>
                        </div>
                    </article>
                </div>
            </div>
        </section>

        <!-- PRICING BAND -->
        <section class="band" id="fees">
            <div class="ph" data-parallax="0.15" style="background-image: url('Content/media/landing/card-detail.png')" role="presentation"></div>
            <div class="ov"></div>
            <div class="wrap"><div class="inner reveal">
                <span class="eyebrow" style="margin-bottom: 16px;">Pricing</span>
                <h2 class="h2">Honest, upfront pricing.</h2>
                <p>No monthly fee to hold the card, no hidden FX markups, no surprise charges. You pay a flat <strong style="color: var(--ink)">3% when you top up with USDT</strong> &mdash; and that's the whole story.</p>
            </div></div>
        </section>

        <!-- FAQ -->
        <section class="faq" id="faq">
            <div class="wrap">
                <div class="sec-head center reveal">
                    <span class="eyebrow" style="justify-content: center; display: flex;">Questions</span>
                    <h2 class="h2">Good to know.</h2>
                </div>
                <div class="faq-grid">
                    <div class="faq-media reveal"><img src="Content/media/landing/security.png" alt="Kash security shield" /></div>
                    <div class="faq-list reveal" data-d="1">
                        <div class="faq-item">
                            <button class="faq-q" type="button">Which crypto can I use to top up?<span class="pm"></span></button>
                            <div class="faq-a"><div class="faq-a-inner">Right now Kash supports topping up with <strong>USDT only</strong>. It's a stablecoin pegged to the US dollar, so your balance stays predictable. Support for more assets is on the roadmap.</div></div>
                        </div>
                        <div class="faq-item">
                            <button class="faq-q" type="button">How much does it cost?<span class="pm"></span></button>
                            <div class="faq-a"><div class="faq-a-inner">A flat <strong>3% fee, charged upfront each time you top up</strong> from USDT to your card. There's no monthly fee, no FX markup, and no surprise charges at checkout &mdash; the balance you see is exactly what you can spend.</div></div>
                        </div>
                        <div class="faq-item">
                            <button class="faq-q" type="button">Is it a physical or virtual card?<span class="pm"></span></button>
                            <div class="faq-a"><div class="faq-a-inner">Kash is a <strong>virtual card</strong>. It's issued instantly and you add it to Apple&nbsp;Pay or Google&nbsp;Pay to tap and pay in stores, or use the details for online checkout. No plastic to wait for.</div></div>
                        </div>
                        <div class="faq-item">
                            <button class="faq-q" type="button">How does the crypto-to-fiat conversion work?<span class="pm"></span></button>
                            <div class="faq-a"><div class="faq-a-inner">When you pay, Kash converts exactly the amount you need from your USDT balance at the live mid-market rate and settles the merchant in fiat &mdash; all in under a second, with the rate locked the moment the payment clears.</div></div>
                        </div>
                        <div class="faq-item">
                            <button class="faq-q" type="button">Is my money safe?<span class="pm"></span></button>
                            <div class="faq-a"><div class="faq-a-inner">Yes. Kash uses full KYC and regulatory compliance, holds funds in segregated custody, encrypts everything with 256-bit encryption, and monitors on-chain activity 24/7. You can freeze or kill your card instantly from the app.</div></div>
                        </div>
                    </div>
                </div>
            </div>
        </section>

        <!-- CTA -->
        <section class="cta" id="waitlist">
            <div class="wrap">
                <div class="cta-card reveal">
                    <div class="cta-visual"><img src="Content/media/landing/hero-card.png" alt="Kash virtual card" /></div>
                    <span class="eyebrow" style="justify-content: center; display: flex; margin-bottom: 18px;">Join 12,400+ on the waitlist</span>
                    <h2 class="display">Your USDT is ready.<br><span class="grad">Is your card?</span></h2>
                    <p>Be first in line for the next batch of Kash virtual cards. No spam &mdash; just one email when your spot opens.</p>
                    <div class="cta-form">
                        <a class="btn btn-cyan" href="register" runat="server" id="btnRegister2">Reserve my card</a>
                    </div>
                </div>
            </div>
        </section>

        <!-- FOOTER -->
        <footer class="footer">
            <div class="wrap">
                <div class="footer-grid">
                    <div>
                        <a class="brand" href="/"><img class="brand-logo" src="Content/media/landing/kash-logo.png" alt="Kash logo" /> Kash</a>
                        <p class="footer-about">The virtual card you top up with USDT and spend anywhere. Instant conversion, one simple fee.</p>
                    </div>
                    <div class="footer-col"><h4>Product</h4><a href="#features">Features</a><a href="#how">How it works</a><a href="#fees">Fees</a><a href="register">Get a card</a></div>
                    <div class="footer-col"><h4>Company</h4><a href="#security">Security</a><a href="about">About</a><a href="contact">Contact</a></div>
                    <div class="footer-col"><h4>Legal</h4><span>Terms of Service</span><span>Privacy Policy</span></div>
                </div>
                <div class="footer-bottom">
                    <span>&copy; 2026 Kash. All rights reserved.</span>
                    <span class="mono">kash.cards</span>
                </div>
            </div>
        </footer>

        <script src='<%= ResolveUrl("~/Content/js/landing.js") %>'></script>
    </form>
</body>
</html>
