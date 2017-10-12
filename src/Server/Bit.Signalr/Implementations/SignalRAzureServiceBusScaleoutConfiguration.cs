using Bit.Core.Contracts;
using Bit.Core.Models;
using Bit.Signalr.Contracts;
using Microsoft.AspNet.SignalR;

namespace Bit.Signalr.Implementations
{
    public class SignalRAzureServiceBusScaleoutConfiguration : ISignalRConfiguration
    {
        public virtual IAppEnvironmentProvider AppEnvironmentProvider { get; set; }

        public virtual void Configure(HubConfiguration signalRConfig)
        {
            AppEnvironment activeAppEnvironment = AppEnvironmentProvider.GetActiveAppEnvironment();

            string signalRAzureServiceBusConnectionString =
                activeAppEnvironment.GetConfig<string>("SignalRAzureServiceBusConnectionString");

            signalRConfig.Resolver.UseServiceBus(signalRAzureServiceBusConnectionString, "SignalR");
        }
    }
}