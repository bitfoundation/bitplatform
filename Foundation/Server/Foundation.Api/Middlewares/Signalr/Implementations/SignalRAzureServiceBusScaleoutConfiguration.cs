using Foundation.Api.Middlewares.SignalR.Contracts;
using Foundation.Core.Contracts;
using Foundation.Core.Models;
using Microsoft.AspNet.SignalR;

namespace Foundation.Api.Middlewares.SignalR.Implementations
{
    public class SignalRAzureServiceBusScaleoutConfiguration : ISignalRConfiguration
    {
        private readonly IAppEnvironmentProvider _appEnvironmentProvider;

        protected SignalRAzureServiceBusScaleoutConfiguration()
        {
        }

        public SignalRAzureServiceBusScaleoutConfiguration(IAppEnvironmentProvider appEnvironmentProvider)
        {
            _appEnvironmentProvider = appEnvironmentProvider;
        }

        public virtual void Configure(HubConfiguration SignalRConfig)
        {
            AppEnvironment activeAppEnvironment = _appEnvironmentProvider.GetActiveAppEnvironment();

            string SignalRAzureServiceBusConnectionString =
                activeAppEnvironment.GetConfig<string>("SignalRAzureServiceBusConnectionString");

            SignalRConfig.Resolver.UseServiceBus(SignalRAzureServiceBusConnectionString, "SignalR");
        }
    }
}