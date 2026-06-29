using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;

namespace QryptoCard.Tests.Smoke
{
    // Builds HttpClients against the deployed public API. The public tier uses HTTP
    // Basic auth: base64("<APIKey>:<secret>"), where the secret is the plaintext the
    // seeder emitted as SMOKE_API_SECRET (the INT tier bcrypt-verifies it).
    internal static class SmokeHttpClient
    {
        public static HttpClient Authenticated()
        {
            var client = Base();
            string raw = SmokeEnv.ApiKey + ":" + SmokeEnv.ApiSecret;
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Basic", Convert.ToBase64String(Encoding.UTF8.GetBytes(raw)));
            return client;
        }

        public static HttpClient Anonymous() => Base();

        private static HttpClient Base()
        {
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            return new HttpClient
            {
                BaseAddress = new Uri(SmokeEnv.BaseUrl.TrimEnd('/') + "/"),
                Timeout = TimeSpan.FromSeconds(30)
            };
        }
    }
}
