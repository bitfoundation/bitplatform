using Bit.Core.Models;
using Hangfire.Azure.ServiceBusQueue;
using Hangfire.SqlServer;
using System;

namespace Bit.Hangfire.Implementations
{
    public class JobSchedulerSqlAndAzureServiceBusBackendConfiguration : JobSchedulerBaseBackendConfiguration<SqlServerStorage>
    {
        public virtual AppEnvironment AppEnvironment { get; set; }

        protected override SqlServerStorage BuildStorage()
        {
            string jobSchedulerDbConnectionString = AppEnvironment.GetConfig<string>(AppEnvironment.KeyValues.Hangfire.JobSchedulerDbConnectionString);

            SqlServerStorage storage = new SqlServerStorage(jobSchedulerDbConnectionString, new SqlServerStorageOptions
            {
                PrepareSchemaIfNecessary = false,
                UseRecommendedIsolationLevel = true,
                SchemaName = "HangFire",
                CommandBatchMaxTimeout = TimeSpan.FromMinutes(5),
                SlidingInvisibilityTimeout = TimeSpan.FromMinutes(5),
                QueuePollInterval = TimeSpan.Zero,
                UsePageLocksOnDequeue = true,
                DisableGlobalLocks = true
            }); // https://docs.hangfire.io/en/latest/configuration/using-sql-server.html#configuration

            if (AppEnvironment.TryGetConfig(AppEnvironment.KeyValues.Hangfire.JobSchedulerAzureServiceBusConnectionString, out string signalRAzureServiceBusConnectionString))
            {
                storage.UseServiceBusQueues(signalRAzureServiceBusConnectionString);
            }

            return storage;
        }
    }
}