using Autofac;
using Bit.Core.Contracts;
using Bit.Core.Models;
using Bit.Hangfire.Contracts;
using Bit.Owin.Contracts;
using Hangfire;
using Hangfire.Azure.ServiceBusQueue;
using Hangfire.Logging;
using Hangfire.SqlServer;
using System;
using System.Collections.Generic;
using System.Transactions;

namespace Bit.Hangfire.Implementations
{
    public class SqlAndAzureServiceBusBackendJobServerConfiguration : IAppEvents
    {
        public virtual IAutofacDependencyManager DependencyManager
        {
            set
            {
                if (value == null)
                    throw new ArgumentNullException(nameof(DependencyManager));
                _lifetimeScope = value.GetContainer();
            }
        }

        public virtual JobActivator JobActivator { get; set; }

        public virtual AppEnvironment AppEnvironment { get; set; }

        public virtual ILogProvider LogProvider { get; set; }

        public virtual IEnumerable<IHangfireOptionsCustomizer> Customizers { get; set; }

        private BackgroundJobServer _backgroundJobServer;
        private ILifetimeScope _lifetimeScope;

        public virtual void OnAppStartup()
        {
            string jobSchedulerDbConnectionString = AppEnvironment.GetConfig<string>("JobSchedulerDbConnectionString");

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

            if (AppEnvironment.HasConfig("JobSchedulerAzureServiceBusConnectionString"))
            {
                string signalRAzureServiceBusConnectionString = AppEnvironment.GetConfig<string>("JobSchedulerAzureServiceBusConnectionString");

                storage.UseServiceBusQueues(signalRAzureServiceBusConnectionString);
            }

            JobStorage.Current = storage;
            GlobalConfiguration.Configuration.UseStorage(storage);
            GlobalConfiguration.Configuration.UseAutofacActivator(_lifetimeScope);
            GlobalConfiguration.Configuration.UseLogProvider(LogProvider);

            BackgroundJobServerOptions options = new BackgroundJobServerOptions
            {
                Activator = JobActivator
            };

            foreach (IHangfireOptionsCustomizer customizer in Customizers)
            {
                customizer.Customize(GlobalConfiguration.Configuration, options, storage);
            }

            _backgroundJobServer = new BackgroundJobServer(options, storage);
        }

        public virtual void OnAppEnd()
        {
            _backgroundJobServer?.Dispose();
        }
    }
}