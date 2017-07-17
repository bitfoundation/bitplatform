using Bit.Core.Contracts;
using Bit.Core.Models;
using Bit.Signalr.Contracts;
using Microsoft.AspNet.SignalR;

namespace Bit.Signalr.Implementations
{
    public class SignalRSqlServerScaleoutConfiguration : ISignalRConfiguration
    {
        private readonly IAppEnvironmentProvider _appEnvironmentProvider;

#if DEBUG
        protected SignalRSqlServerScaleoutConfiguration()
        {
        }
#endif

        public SignalRSqlServerScaleoutConfiguration(IAppEnvironmentProvider appEnvironmentProvider)
        {
            _appEnvironmentProvider = appEnvironmentProvider;
        }

        public virtual void Configure(HubConfiguration signalRConfig)
        {
            AppEnvironment activeAppEnvironment = _appEnvironmentProvider.GetActiveAppEnvironment();

            string sqlServerConnectionString = activeAppEnvironment.GetConfig<string>("SignalRSqlServerConnectionString");

            signalRConfig.Resolver.UseSqlServer(new SqlScaleoutConfiguration(sqlServerConnectionString)
            {
                TableCount = activeAppEnvironment.GetConfig("SignalRSqlServerTableCount", 3)
            });
        }
    }
}