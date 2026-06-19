<%@ Page Title="Home Page" Language="C#" AutoEventWireup="true" CodeBehind="Default.aspx.cs" Inherits="QryptoCard.Dashboard._Default" %>

<%--MasterPageFile="~/Site.Master"--%>

<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>Kash.Cards - Card Payment for Crypto</title>
    <script src="https://cdn.tailwindcss.com"></script>
    <script>
        tailwind.config = {
            theme: {
                extend: {
                    colors: {
                        primary: '#BAFC04',
                        background: '#151515',
                    },
                },
            },
        }
    </script>
    <style>
        @import url('https://fonts.googleapis.com/css2?family=Inconsolata:wght@400;700&display=swap');

        body {
            font-family: 'Inconsolata', monospace;
        }
    </style>
</head>
<body class="bg-background text-white">
    <form runat="server">
        <header class="fixed top-0 left-0 right-0 z-50 transition-all duration-300">
            <div class="container mx-auto px-4 py-6">
                <nav class="flex justify-between items-center">
                    <a href="/" class="flex items-center gap-2">
                        <img src="https://hebbkx1anhila5yf.public.blob.vercel-storage.com/logo-light-rGvlSpiQEHrLADZZ288DAek20BMV7h.png" alt="Kash.Cards Logo" class="w-auto h-8">
                        <span class="text-2xl font-bold">Kash.Cards</span>
                    </a>
                    <ul class="hidden md:flex space-x-6">
                        <li><a href="#features" class="hover:text-primary">Features</a></li>
                        <li><a href="#how-it-works" class="hover:text-primary">How It Works</a></li>
                    </ul>
                    <div class="flex space-x-4">
                        <a href="login" runat="server" id="btnLogin" class="bg-transparent text-white border border-white hover:bg-white hover:text-background px-4 py-2 rounded">Log In</a>
                        <a href="register" runat="server" id="btnRegister" class="bg-primary text-background hover:bg-primary/90 px-4 py-2 rounded">Sign Up</a>
                        <a href="dashboard" runat="server" id="btnDashboard" class="bg-primary text-background hover:bg-primary/90 px-4 py-2 rounded">Open Dashboard</a>
                    </div>
                </nav>
            </div>
        </header>

        <main class="pt-20">
            <section class="relative w-full py-20 md:py-32">
                <div class="absolute inset-0 w-screen bg-gradient-to-r from-primary/5 to-background">
                    <div class="absolute inset-0 w-full" style="background-image: linear-gradient(to right, rgba(186, 252, 4, 0.02) 1px, transparent 1px), linear-gradient(to bottom, rgba(186, 252, 4, 0.02) 1px, transparent 1px); background-size: 24px 24px;"></div>
                </div>
                <div class="container mx-auto px-4 relative">
                    <div class="relative flex flex-col md:flex-row items-center">
                        <div class="md:w-1/2 mb-10 md:mb-0">
                            <h1 class="text-4xl md:text-6xl font-bold mb-6 bg-gradient-to-br from-[#00AAE9] to-primary bg-clip-text text-transparent">Transform Your Crypto into Everyday Spending Power
                        </h1>
                            <p class="text-xl mb-8">
                                Seamlessly integrate cryptocurrency payments into your business with our innovative card solution.
                       
                            </p>
                            <a href="register" runat="server" id="txtGetStarted" class="bg-primary text-background hover:bg-primary/90 text-lg px-8 py-4 rounded inline-flex items-center">Get Started
                           
                                <svg xmlns="http://www.w3.org/2000/svg" class="h-5 w-5 ml-2" viewBox="0 0 20 20" fill="currentColor">
                                    <path fill-rule="evenodd" d="M10.293 3.293a1 1 0 011.414 0l6 6a1 1 0 010 1.414l-6 6a1 1 0 01-1.414-1.414L14.586 11H3a1 1 0 110-2h11.586l-4.293-4.293a1 1 0 010-1.414z" clip-rule="evenodd" />
                                </svg>
                            </a>
                        </div>
                        <div class="md:w-1/2 hidden md:flex justify-center">
                            <div class="relative w-[500px] h-[312px]">
                                <%--<img src="https://hebbkx1anhila5yf.public.blob.vercel-storage.com/illushero-ZLemImWZ26TyjrHskMGLUcMlPPYXnm.png" alt="Kash.Cards Crypto Payment Card" class="object-contain w-full h-full">--%>
                                <img src="Content/media/illustrations/card-banner.png" alt="Kash.Cards Crypto Payment Card" class="object-contain w-full h-full">
                            </div>
                        </div>
                    </div>
                </div>
            </section>

            <section id="features" class="bg-[#1A1A1A] py-20">
                <div class="container mx-auto px-4">
                    <h2 class="text-3xl md:text-4xl font-bold mb-12 text-center">Key Features</h2>
                    <div class="grid grid-cols-1 md:grid-cols-3 gap-8">
                        <div class="bg-[#212121] p-6 rounded-lg border border-gray-400/10 hover:border-primary/50 transition-colors duration-300">
                            <svg xmlns="http://www.w3.org/2000/svg" class="h-8 w-8 text-primary mb-4" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 12l2 2 4-4m6 2a9 9 0 11-18 0 9 9 0 0118 0z" />
                            </svg>
                            <h3 class="text-xl font-semibold mb-2">Instant Crypto Transactions</h3>
                            <p class="text-gray-400">Experience lightning-fast crypto transactions, enabling real-time payments and transfers for your business needs.</p>
                        </div>
                        <div class="bg-[#212121] p-6 rounded-lg border border-gray-400/10 hover:border-primary/50 transition-colors duration-300">
                            <svg xmlns="http://www.w3.org/2000/svg" class="h-8 w-8 text-primary mb-4" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 12l2 2 4-4m5.618-4.016A11.955 11.955 0 0112 2.944a11.955 11.955 0 01-8.618 3.04A12.02 12.02 0 003 9c0 5.591 3.824 10.29 9 11.622 5.176-1.332 9-6.03 9-11.622 0-1.042-.133-2.052-.382-3.016z" />
                            </svg>
                            <h3 class="text-xl font-semibold mb-2">Secure and Compliant</h3>
                            <p class="text-gray-400">Our platform adheres to the highest security standards and regulatory requirements, ensuring your transactions are safe and compliant.</p>
                        </div>
                        <div class="bg-[#212121] p-6 rounded-lg border border-gray-400/10 hover:border-primary/50 transition-colors duration-300">
                            <svg xmlns="http://www.w3.org/2000/svg" class="h-8 w-8 text-primary mb-4" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M3 6l3 1m0 0l-3 9a5.002 5.002 0 006.001 0M6 7l3 9M6 7l6-2m6 2l3-1m-3 1l-3 9a5.002 5.002 0 006.001 0M18 7l3 9m-3-9l-6-2m0-2v2m0 16V5m0 16H9m3 0h3" />
                            </svg>
                            <h3 class="text-xl font-semibold mb-2">Multi-Currency Support</h3>
                            <p class="text-gray-400">Accept and manage multiple cryptocurrencies, expanding your payment options and reaching a global customer base.</p>
                        </div>
                    </div>
                </div>
            </section>

            <section id="how-it-works" class="py-20">
                <div class="container mx-auto px-4">
                    <h2 class="text-3xl md:text-4xl font-bold mb-12 text-center">How It Works</h2>
                    <div class="grid grid-cols-1 md:grid-cols-3 gap-8">
                        <div class="text-center">
                            <div class="w-16 h-16 bg-primary text-background rounded-full flex items-center justify-center text-2xl font-bold mb-4 mx-auto">1</div>
                            <h3 class="text-xl font-semibold mb-2">Login</h3>
                            <p class="text-gray-400">Create your account and securely log in to access our platform.</p>
                        </div>
                        <div class="text-center">
                            <div class="w-16 h-16 bg-primary text-background rounded-full flex items-center justify-center text-2xl font-bold mb-4 mx-auto">2</div>
                            <h3 class="text-xl font-semibold mb-2">Buy Card</h3>
                            <p class="text-gray-400">Choose and purchase your crypto card with just a few clicks.</p>
                        </div>
                        <div class="text-center">
                            <div class="w-16 h-16 bg-primary text-background rounded-full flex items-center justify-center text-2xl font-bold mb-4 mx-auto">3</div>
                            <h3 class="text-xl font-semibold mb-2">Start Spending</h3>
                            <p class="text-gray-400">Use your card for everyday purchases, converting crypto to fiat instantly.</p>
                        </div>
                    </div>
                </div>
            </section>

            <section class="bg-[#1A1A1A] py-20">
                <div class="container mx-auto px-4 text-center">
                    <h2 class="text-3xl md:text-4xl font-bold mb-6">Ready to Get Started?</h2>
                    <p class="text-xl mb-8">Join thousands of businesses already using CryptoCard for their payment needs.</p>
                    <div class="flex flex-col md:flex-row justify-center items-center space-y-4 md:space-y-0 md:space-x-4">
                        <input type="email" runat="server" id="txtEmail" placeholder="Enter your email" class="max-w-xs bg-[#212121] border-[#2A2826] text-white px-4 py-2 rounded">
                        <a href="register" runat="server" id="btnRegister2" class="bg-primary text-background hover:bg-primary/90 px-6 py-2 rounded inline-flex items-center">Sign Up Now
                       
                            <svg xmlns="http://www.w3.org/2000/svg" class="h-5 w-5 ml-2" viewBox="0 0 20 20" fill="currentColor">
                                <path fill-rule="evenodd" d="M10.293 3.293a1 1 0 011.414 0l6 6a1 1 0 010 1.414l-6 6a1 1 0 01-1.414-1.414L14.586 11H3a1 1 0 110-2h11.586l-4.293-4.293a1 1 0 010-1.414z" clip-rule="evenodd" />
                            </svg>
                        </a>
                    </div>
                </div>
            </section>
        </main>

        <footer class="bg-background py-8 border-t border-[#2A2826]">
            <div class="container mx-auto px-4 text-center text-gray-400">
                <p>&copy; 2025 Kash.Cards. All rights reserved.</p>
            </div>
        </footer>
    </form>
</body>
</html>

