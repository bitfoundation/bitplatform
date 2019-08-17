using Bit.Core.Models;
using Bit.Signalr.Contracts;
using Microsoft.AspNet.SignalR;

namespace Bit.Signalr.Implementations
{
    public class SignalRSqlServerScaleoutConfiguration : ISignalRConfiguration
    {
        public virtual AppEnvironment AppEnvironment { get; set; }

        public virtual void Configure(HubConfiguration signalRConfig)
        {
            string sqlServerConnectionString = AppEnvironment.GetConfig<string>(AppEnvironment.KeyValues.Signalr.SignalRSqlServerConnectionString);

            signalRConfig.Resolver.UseSqlServer(new SqlScaleoutConfiguration(sqlServerConnectionString)
            {
                TableCount = AppEnvironment.GetConfig(AppEnvironment.KeyValues.Signalr.SignalRSqlServerTableCount, AppEnvironment.KeyValues.Signalr.SignalRSqlServerTableCountDefaultValue)
            });
        }
    }
}