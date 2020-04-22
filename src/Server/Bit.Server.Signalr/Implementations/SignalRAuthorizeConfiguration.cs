using Bit.Signalr.Contracts;
using Microsoft.AspNet.SignalR;
using Microsoft.AspNet.SignalR.Hubs;
using System;

namespace Bit.Signalr.Implementations
{
    public class SignalRAuthorizeConfiguration : ISignalRConfiguration
    {
        public virtual void Configure(HubConfiguration signalRConfig)
        {
            if (signalRConfig == null)
                throw new ArgumentNullException(nameof(signalRConfig));

            signalRConfig.Resolver.Resolve<IHubPipeline>().RequireAuthentication();
        }
    }
}
