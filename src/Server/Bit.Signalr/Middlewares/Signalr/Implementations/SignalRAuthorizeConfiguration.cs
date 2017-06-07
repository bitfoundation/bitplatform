using Bit.Signalr.Middlewares.Signalr.Contracts;
using Microsoft.AspNet.SignalR;
using Microsoft.AspNet.SignalR.Hubs;

namespace Bit.Signalr.Middlewares.Signalr.Implementations
{
    public class SignalRAuthorizeConfiguration : ISignalRConfiguration
    {
        public virtual void Configure(HubConfiguration signalRConfig)
        {
            signalRConfig.Resolver.Resolve<IHubPipeline>().RequireAuthentication();
        }
    }
}
