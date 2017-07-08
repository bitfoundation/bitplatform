using Bit.Signalr.Contracts;
using Microsoft.AspNet.SignalR;
using Microsoft.AspNet.SignalR.Hubs;

namespace Bit.Signalr.Implementations
{
    public class SignalRAuthorizeConfiguration : ISignalRConfiguration
    {
        public virtual void Configure(HubConfiguration signalRConfig)
        {
            signalRConfig.Resolver.Resolve<IHubPipeline>().RequireAuthentication();
        }
    }
}
