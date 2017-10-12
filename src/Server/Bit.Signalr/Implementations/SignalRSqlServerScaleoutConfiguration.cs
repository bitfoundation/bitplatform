using Bit.Core.Contracts;
using Bit.Core.Models;
using Bit.Signalr.Contracts;
using Microsoft.AspNet.SignalR;

namespace Bit.Signalr.Implementations
{
    public class SignalRSqlServerScaleoutConfiguration : ISignalRConfiguration
    {
        public virtual IAppEnvironmentProvider AppEnvironmentProvider { get; set; }

        public virtual void Configure(HubConfiguration signalRConfig)
        {
            AppEnvironment activeAppEnvironment = AppEnvironmentProvider.GetActiveAppEnvironment();

            string sqlServerConnectionString = activeAppEnvironment.GetConfig<string>("SignalRSqlServerConnectionString");

            signalRConfig.Resolver.UseSqlServer(new SqlScaleoutConfiguration(sqlServerConnectionString)
            {
                TableCount = activeAppEnvironment.GetConfig("SignalRSqlServerTableCount", 3)
            });
        }
    }
}