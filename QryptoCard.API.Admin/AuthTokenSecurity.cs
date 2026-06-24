using Newtonsoft.Json;
using QryptoCard.INT.Model.Service;
using QryptoCard.INT.Script.Service.Auth.v1;
using System;
using System.ServiceModel;

namespace QryptoCard.API.Admin
{
    // Admin-tier mirror of QryptoCard.API.AuthTokenSecurity. ChannelFactory<IAuthV1Service>
    // wrapper used by:
    //   - BearerAuthenticationAttribute -> Verify (per-request hot path)
    //   - AuthV1Controller mint/refresh/revoke routes
    //
    // See QryptoCard.API/AuthTokenSecurity.cs for full design rationale (why
    // ChannelFactory over a generated proxy, endpoint config, the test seam).
    public static class AuthTokenSecurity
    {
        public const string EndpointConfigName = "BasicHttpBinding_IAuthV1Service";

        // Test seam — same shape as the user tier's, reachable via InternalsVisibleTo.
        internal static Func<string, AuthVerifyResponse> VerifyImpl = WcfVerify;

        public static AuthVerifyResponse Verify(string accessToken) => VerifyImpl(accessToken);

        public static OutputModel MintAfterOtpVerify(string otpSessionId, string otpCode, string subjectType)
            => InvokeOnChannel(c => c.mintAfterOtpVerify(otpSessionId, otpCode, subjectType));

        public static OutputModel Refresh(string refreshToken)
            => InvokeOnChannel(c => c.refresh(refreshToken));

        public static OutputModel Revoke(string refreshToken)
            => InvokeOnChannel(c => c.revoke(refreshToken));

        private static AuthVerifyResponse WcfVerify(string accessToken)
        {
            var factory = ChannelFactoryHolder.Instance;
            var channel = factory.CreateChannel();
            var clientChannel = (IClientChannel)channel;
            try
            {
                clientChannel.Open();
                var op = channel.verify(accessToken);
                clientChannel.Close();

                if (op == null || op.Status != "success" || op.Data == null)
                    return new AuthVerifyResponse { Valid = false };

                var json = op.Data.ToString();
                return JsonConvert.DeserializeObject<AuthVerifyResponse>(json)
                    ?? new AuthVerifyResponse { Valid = false };
            }
            catch
            {
                // Fail closed on any WCF fault — never let an unverified token through.
                clientChannel.Abort();
                return new AuthVerifyResponse { Valid = false };
            }
        }

        private static OutputModel InvokeOnChannel(Func<IAuthV1Service, OutputModel> call)
        {
            var factory = ChannelFactoryHolder.Instance;
            var channel = factory.CreateChannel();
            var clientChannel = (IClientChannel)channel;
            try
            {
                clientChannel.Open();
                var op = call(channel);
                clientChannel.Close();
                return op ?? new OutputModel { Status = "error", Message = "auth service returned null" };
            }
            catch (Exception ex)
            {
                clientChannel.Abort();
                // Don't surface the WCF channel exception (endpoint/topology) to the caller; log it.
                System.Diagnostics.Trace.TraceError("Auth channel error: " + ex);
                return new OutputModel { Status = "error", Message = "Authentication service is unavailable. Please try again." };
            }
        }

        static class ChannelFactoryHolder
        {
            internal static readonly ChannelFactory<IAuthV1Service> Instance =
                new ChannelFactory<IAuthV1Service>(EndpointConfigName);
        }
    }
}
