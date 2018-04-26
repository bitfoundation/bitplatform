using Bit.Core.Models;
using Bit.Signalr.Contracts;
using Microsoft.AspNet.SignalR;

namespace Bit.Signalr.Implementations
{
    public class SignalRAzureServiceBusScaleoutConfiguration : ISignalRConfiguration
    {
        public virtual AppEnvironment AppEnvironment { get; set; }

        public virtual void Configure(HubConfiguration signalRConfig)
        {
            string signalRAzureServiceBusConnectionString =
                AppEnvironment.GetConfig<string>("SignalRAzureServiceBusConnectionString");

            signalRConfig.Resolver.UseServiceBus(signalRAzureServiceBusConnectionString, "SignalR");
        }
    }
}