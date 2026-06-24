using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Description;
using System.ServiceModel.Dispatcher;
using QryptoCard.Sec;

namespace QryptoCard.API.Callback.Security
{
    /// <summary>
    /// Adds the X-Int-Auth shared-secret header to outbound calls to the INT callback tier, so the
    /// money-tier WCF operations only accept calls from this API tier. Pair of the INT-side
    /// IntAuthBehavior; the secret is INT_CALLBACK_SHARED_SECRET from the environment.
    /// </summary>
    public sealed class IntAuthClientBehavior : IEndpointBehavior
    {
        public void AddBindingParameters(ServiceEndpoint endpoint, BindingParameterCollection parameters) { }
        public void ApplyClientBehavior(ServiceEndpoint endpoint, ClientRuntime clientRuntime)
        {
            clientRuntime.ClientMessageInspectors.Add(new IntAuthClientInspector());
        }
        public void ApplyDispatchBehavior(ServiceEndpoint endpoint, EndpointDispatcher endpointDispatcher) { }
        public void Validate(ServiceEndpoint endpoint) { }
    }

    internal sealed class IntAuthClientInspector : IClientMessageInspector
    {
        public const string HeaderName = "X-Int-Auth";

        public object BeforeSendRequest(ref Message request, IClientChannel channel)
        {
            HttpRequestMessageProperty http;
            object existing;
            if (request.Properties.TryGetValue(HttpRequestMessageProperty.Name, out existing))
                http = (HttpRequestMessageProperty)existing;
            else
            {
                http = new HttpRequestMessageProperty();
                request.Properties.Add(HttpRequestMessageProperty.Name, http);
            }
            http.Headers[HeaderName] = SecretsConfig.Require("INT_CALLBACK_SHARED_SECRET");
            return null;
        }

        public void AfterReceiveReply(ref Message reply, object correlationState) { }
    }
}
