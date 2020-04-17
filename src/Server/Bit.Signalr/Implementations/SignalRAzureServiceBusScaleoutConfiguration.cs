using Bit.Core.Models;
using Bit.Signalr.Contracts;
using Microsoft.AspNet.SignalR;
using System;

namespace Bit.Signalr.Implementations
{
    public class SignalRAzureServiceBusScaleoutConfiguration : ISignalRConfiguration
    {
        public virtual AppEnvironment AppEnvironment { get; set; } = default!;

        public virtual void Configure(HubConfiguration signalRConfig)
        {
            if (signalRConfig == null)
                throw new ArgumentNullException(nameof(signalRConfig));

            string signalRAzureServiceBusConnectionString =
                AppEnvironment.GetConfig<string>(AppEnvironment.KeyValues.Signalr.SignalRAzureServiceBusConnectionString) ?? throw new InvalidOperationException($"{nameof(AppEnvironment.KeyValues.Signalr.SignalRAzureServiceBusConnectionString)} is null.");

            signalRConfig.Resolver.UseServiceBus(signalRAzureServiceBusConnectionString, "SignalR");
        }
    }
}