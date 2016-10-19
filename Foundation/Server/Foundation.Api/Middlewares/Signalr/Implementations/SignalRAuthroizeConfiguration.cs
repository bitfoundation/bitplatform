using Foundation.Api.Middlewares.SignalR.Contracts;
using Microsoft.AspNet.SignalR;
using Microsoft.AspNet.SignalR.Hubs;

namespace Foundation.Api.Middlewares.SignalR.Implementations
{
    public class SignalRAuthroizeConfiguration : ISignalRConfiguration
    {
        public virtual void Configure(HubConfiguration SignalRConfig)
        {
            SignalRConfig.Resolver.Resolve<IHubPipeline>().RequireAuthentication();
        }
    }
}
