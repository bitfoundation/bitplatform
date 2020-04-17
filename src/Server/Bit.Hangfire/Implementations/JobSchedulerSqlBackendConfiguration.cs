using Bit.Core.Models;
using Hangfire.SqlServer;
using System;

namespace Bit.Hangfire.Implementations
{
    public class JobSchedulerSqlBackendConfiguration : JobSchedulerBaseBackendConfiguration<SqlServerStorage>
    {
        public virtual AppEnvironment AppEnvironment { get; set; } = default!;

        protected override SqlServerStorage BuildStorage()
        {
            string jobSchedulerDbConnectionString = AppEnvironment.GetConfig<string>(AppEnvironment.KeyValues.Hangfire.JobSchedulerDbConnectionString) ?? throw new InvalidOperationException("JobSchedulerDbConnectionString could not be found");

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

            return storage;
        }
    }
}