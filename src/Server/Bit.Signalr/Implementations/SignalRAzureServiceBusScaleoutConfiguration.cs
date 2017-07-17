using Bit.Core.Contracts;
using Bit.Core.Models;
using Bit.Signalr.Contracts;
using Microsoft.AspNet.SignalR;

namespace Bit.Signalr.Implementations
{
    public class SignalRAzureServiceBusScaleoutConfiguration : ISignalRConfiguration
    {
        private readonly IAppEnvironmentProvider _appEnvironmentProvider;

#if DEBUG
        protected SignalRAzureServiceBusScaleoutConfiguration()
        {
        }
#endif

        public SignalRAzureServiceBusScaleoutConfiguration(IAppEnvironmentProvider appEnvironmentProvider)
        {
            _appEnvironmentProvider = appEnvironmentProvider;
        }

        public virtual void Configure(HubConfiguration signalRConfig)
        {
            AppEnvironment activeAppEnvironment = _appEnvironmentProvider.GetActiveAppEnvironment();

            string signalRAzureServiceBusConnectionString =
                activeAppEnvironment.GetConfig<string>("SignalRAzureServiceBusConnectionString");

            signalRConfig.Resolver.UseServiceBus(signalRAzureServiceBusConnectionString, "SignalR");
        }
    }
}