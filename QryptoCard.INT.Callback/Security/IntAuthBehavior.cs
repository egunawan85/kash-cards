using System;
using System.Collections.ObjectModel;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Description;
using System.ServiceModel.Dispatcher;
using QryptoCard.Sec;

namespace QryptoCard.INT.Callback.Security
{
    /// <summary>
    /// Requires every inbound WCF call to carry a valid X-Int-Auth shared secret. Defense in
    /// depth so the money-tier operations cannot be invoked by anyone who merely reaches the
    /// endpoint (network isolation remains the primary control). The secret is shared with the
    /// API tier via the environment variable INT_CALLBACK_SHARED_SECRET.
    /// Apply with [IntAuthBehavior] on the service class.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class IntAuthBehaviorAttribute : Attribute, IServiceBehavior
    {
        public void AddBindingParameters(ServiceDescription d, ServiceHostBase h,
            Collection<ServiceEndpoint> endpoints, BindingParameterCollection parameters) { }

        public void Validate(ServiceDescription d, ServiceHostBase h) { }

        public void ApplyDispatchBehavior(ServiceDescription d, ServiceHostBase host)
        {
            foreach (ChannelDispatcher cd in host.ChannelDispatchers)
                foreach (EndpointDispatcher ed in cd.Endpoints)
                    ed.DispatchRuntime.MessageInspectors.Add(new IntAuthInspector());
        }
    }

    internal sealed class IntAuthInspector : IDispatchMessageInspector
    {
        public const string HeaderName = "X-Int-Auth";
        public const string SecretName = "INT_CALLBACK_SHARED_SECRET";

        public object AfterReceiveRequest(ref Message request, IClientChannel channel, InstanceContext instanceContext)
        {
            string provided = null;
            object prop;
            if (request.Properties.TryGetValue(HttpRequestMessageProperty.Name, out prop))
            {
                HttpRequestMessageProperty http = prop as HttpRequestMessageProperty;
                if (http != null) provided = http.Headers[HeaderName];
            }

            if (!SharedSecretAuth.IsAuthorized(provided, SecretName))
                throw new FaultException("Unauthorized.");

            return null;
        }

        public void BeforeSendReply(ref Message reply, object correlationState) { }
    }
}
