using System.ServiceModel;
using System.ServiceModel.Channels;

namespace QryptoCard.INT.Script.Service
{
    // Shared helper for extracting the originating client IP from the WCF
    // operation context, used to populate the SourceIP column on tblH_Auth_Log.
    //
    // Returns null when:
    //   - There is no active OperationContext (direct in-process test
    //     invocation; never crosses a WCF channel).
    //   - The RemoteEndpointMessageProperty is absent (channel doesn't
    //     surface it; e.g., named-pipe transport).
    //   - Any reflection / cast throws (defensive — observability must
    //     never degrade the response path).
    //
    // Production WCF dispatch over basicHttp(s)Binding populates the
    // RemoteEndpointMessageProperty with the originating client IP.
    public static class WcfSourceIp
    {
        public static string TryGet()
        {
            try
            {
                var ctx = OperationContext.Current;
                if (ctx == null) return null;
                object propObj;
                if (!ctx.IncomingMessageProperties.TryGetValue(
                        RemoteEndpointMessageProperty.Name, out propObj))
                    return null;
                var endpoint = propObj as RemoteEndpointMessageProperty;
                return endpoint == null ? null : endpoint.Address;
            }
            catch
            {
                return null;
            }
        }
    }
}
