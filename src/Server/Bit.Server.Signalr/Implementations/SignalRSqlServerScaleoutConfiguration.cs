using Bit.Core.Models;
using Bit.Signalr.Contracts;
using Microsoft.AspNet.SignalR;
using System;

namespace Bit.Signalr.Implementations
{
    public class SignalRSqlServerScaleoutConfiguration : ISignalRConfiguration
    {
        public virtual AppEnvironment AppEnvironment { get; set; } = default!;

        public virtual void Configure(HubConfiguration signalRConfig)
        {
            if (signalRConfig == null)
                throw new ArgumentNullException(nameof(signalRConfig));

            string sqlServerConnectionString = AppEnvironment.GetConfig<string>(AppEnvironment.KeyValues.Signalr.SignalRSqlServerConnectionString) ?? throw new InvalidOperationException($"{nameof(AppEnvironment.KeyValues.Signalr.SignalRSqlServerConnectionString)} is null.");

            signalRConfig.Resolver.UseSqlServer(new SqlScaleoutConfiguration(sqlServerConnectionString)
            {
                TableCount = AppEnvironment.GetConfig(AppEnvironment.KeyValues.Signalr.SignalRSqlServerTableCount, AppEnvironment.KeyValues.Signalr.SignalRSqlServerTableCountDefaultValue)
            });
        }
    }
}