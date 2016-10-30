using Foundation.Api.Middlewares.SignalR.Contracts;
using Foundation.Core.Contracts;
using Foundation.Core.Models;
using Microsoft.AspNet.SignalR;

namespace Foundation.Api.Middlewares.SignalR.Implementations
{
    public class SignalRSqlServerScaleoutConfiguration : ISignalRConfiguration
    {
        private readonly IAppEnvironmentProvider _appEnvironmentProvider;

        protected SignalRSqlServerScaleoutConfiguration()
        {
        }

        public SignalRSqlServerScaleoutConfiguration(IAppEnvironmentProvider appEnvironmentProvider)
        {
            _appEnvironmentProvider = appEnvironmentProvider;
        }

        public virtual void Configure(HubConfiguration SignalRConfig)
        {
            AppEnvironment activeAppEnvironment = _appEnvironmentProvider.GetActiveAppEnvironment();

            string sqlServerConnectionString = activeAppEnvironment.GetConfig<string>("SignalRSqlServerConnectionString");

            SignalRConfig.Resolver.UseSqlServer(new SqlScaleoutConfiguration(sqlServerConnectionString)
            {
                TableCount = activeAppEnvironment.GetConfig("SignalRSqlServerTableCount", 3)
            });
        }
    }
}