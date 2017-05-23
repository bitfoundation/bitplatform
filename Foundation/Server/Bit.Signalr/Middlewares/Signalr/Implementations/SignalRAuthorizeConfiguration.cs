using Foundation.Api.Middlewares.SignalR.Contracts;
using Microsoft.AspNet.SignalR;
using Microsoft.AspNet.SignalR.Hubs;

namespace Foundation.Api.Middlewares.SignalR.Implementations
{
    public class SignalRAuthorizeConfiguration : ISignalRConfiguration
    {
        public virtual void Configure(HubConfiguration signalRConfig)
        {
            signalRConfig.Resolver.Resolve<IHubPipeline>().RequireAuthentication();
        }
    }
}
