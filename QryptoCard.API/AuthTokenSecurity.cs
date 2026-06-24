using Newtonsoft.Json;
using QryptoCard.INT.Model.Service;
using QryptoCard.INT.Script.Service.Auth.v1;
using System;
using System.ServiceModel;

namespace QryptoCard.API
{
    // ChannelFactory<IAuthV1Service> wrapper. Sibling of ApiSecurity (the
    // existing Basic-auth helper that wraps the generated AuthService.SecurityClient
    // proxy). Used by:
    //   - BearerAuthenticationAttribute -> Verify (per-request hot path)
    //   - AuthV1Controller mint/refresh/revoke routes
    //
    // Why ChannelFactory instead of a VS-generated proxy: this mirrors the
    // sister project's locked decision. The generated proxy pattern produces
    // unreadable Reference.cs files, creates drift hazards if IAuthV1Service
    // changes and someone forgets "Update Service Reference", and grows
    // web.config sprawl. ChannelFactory<T> directly references the interface
    // from QryptoCard.INT (compile-time safety, refactor-friendly).
    //
    // Endpoint configuration: Web.config <client><endpoint name="BasicHttpBinding_IAuthV1Service" />
    // Same pattern the existing services follow — operators rewrite the address
    // per-environment via web.config transformation at deploy time.
    //
    // Test seam: VerifyImpl is settable so tests can bypass the network and
    // point straight at a real (or stubbed) AuthV1Service. In production this
    // stays at the default WcfVerify path.
    public static class AuthTokenSecurity
    {
        public const string EndpointConfigName = "BasicHttpBinding_IAuthV1Service";

        // Test seam. Tests assign this to a stub (or a real AuthV1Service.verify
        // bridge); production leaves it at default. Internal so only same-assembly
        // code and assemblies named in InternalsVisibleTo can reach it.
        internal static Func<string, AuthVerifyResponse> VerifyImpl = WcfVerify;

        public static AuthVerifyResponse Verify(string accessToken) => VerifyImpl(accessToken);

        // User-action-triggered relays (login-mint, refresh, logout). Unlike
        // Verify (per-request hot path) latency matters less here. Pass-through
        // OutputModel shape; the controller handles deserialization of op.Data.
        public static OutputModel MintAfterOtpVerify(string otpSessionId, string otpCode, string subjectType)
            => InvokeOnChannel(c => c.mintAfterOtpVerify(otpSessionId, otpCode, subjectType));

        public static OutputModel Refresh(string refreshToken)
            => InvokeOnChannel(c => c.refresh(refreshToken));

        public static OutputModel Revoke(string refreshToken)
            => InvokeOnChannel(c => c.revoke(refreshToken));

        private static AuthVerifyResponse WcfVerify(string accessToken)
        {
            // ChannelFactory is expensive to construct. Cache the factory across
            // calls; create a per-call channel (channels are not thread-safe).
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

                // op.Data is a JSON string — AuthV1Service.verify serializes
                // AuthVerifyResponse via JsonConvert.SerializeObject (same pattern
                // as every other WCF service in this codebase). WCF carries it as
                // a string on the wire; deserialize it the same way controllers do.
                var json = op.Data.ToString();
                return JsonConvert.DeserializeObject<AuthVerifyResponse>(json)
                    ?? new AuthVerifyResponse { Valid = false };
            }
            catch
            {
                // Fail closed on any WCF fault. The Bearer attribute treats Valid=false
                // as 401, which is the correct outcome for "we cannot reach the auth
                // service" — never let an unverified token through.
                clientChannel.Abort();
                return new AuthVerifyResponse { Valid = false };
            }
        }

        // Channel-lifecycle helper for the user-action-triggered ops. Returns a
        // "failed" OutputModel on any WCF fault — the caller (AuthV1Controller)
        // surfaces that to the client as Status="error".
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
                return new OutputModel { Status = "error", Message = ex.Message };
            }
        }

        // Lazy-singleton holder for the ChannelFactory. Construction is deferred
        // until first use so the type loads cleanly even in test contexts where
        // VerifyImpl is reassigned and WcfVerify is never reached.
        static class ChannelFactoryHolder
        {
            internal static readonly ChannelFactory<IAuthV1Service> Instance =
                new ChannelFactory<IAuthV1Service>(EndpointConfigName);
        }
    }
}
