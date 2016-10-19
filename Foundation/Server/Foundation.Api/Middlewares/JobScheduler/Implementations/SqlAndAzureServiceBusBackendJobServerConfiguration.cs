using System;
using System.Transactions;
using Foundation.Core.Contracts;
using Hangfire;
using Hangfire.SqlServer;
using Foundation.Core.Models;
using Hangfire.Azure.ServiceBusQueue;
using Autofac;

namespace Foundation.Api.Middlewares.JobScheduler.Implementations
{
    public class SqlAndAzureServiceBusBackendJobServerConfiguration : IAppEvents
    {
        private readonly IAppEnvironmentProvider _appEnvironmentProvider;
        private BackgroundJobServer _backgroundJobServer;
        private readonly JobActivator _jobActivator;
        private readonly ILifetimeScope _container;

        protected SqlAndAzureServiceBusBackendJobServerConfiguration()
        {
        }

        public SqlAndAzureServiceBusBackendJobServerConfiguration(IAppEnvironmentProvider appEnvironmentProvider, JobActivator jobActivator, ILifetimeScope container)
        {
            if (appEnvironmentProvider == null)
                throw new ArgumentNullException(nameof(appEnvironmentProvider));

            if (jobActivator == null)
                throw new ArgumentNullException(nameof(jobActivator));

            if (container == null)
                throw new ArgumentNullException(nameof(container));

            _appEnvironmentProvider = appEnvironmentProvider;
            _jobActivator = jobActivator;
            _container = container;
        }

        public virtual void OnAppStartup()
        {
            AppEnvironment activeAppEnvironment = _appEnvironmentProvider.GetActiveAppEnvironment();

            string jobSchedulerDbConnectionString = activeAppEnvironment.GetConfig<string>("JobSchedulerDbConnectionString");

            SqlServerStorage storage = new SqlServerStorage(jobSchedulerDbConnectionString, new SqlServerStorageOptions
            {
                PrepareSchemaIfNecessary = false,
                TransactionIsolationLevel = IsolationLevel.ReadCommitted,
                SchemaName = "Jobs"
            });

            if (activeAppEnvironment.DebugMode == false)
            {
                string SignalRAzureServiceBusConnectionString = activeAppEnvironment.GetConfig<string>("JobSchedulerAzureServiceBusConnectionString");

                storage.UseServiceBusQueues(SignalRAzureServiceBusConnectionString);
            }

            GlobalConfiguration.Configuration.UseStorage(storage);
            GlobalConfiguration.Configuration.UseAutofacActivator(_container);

            _backgroundJobServer = new BackgroundJobServer(new BackgroundJobServerOptions
            {
                Activator = _jobActivator
            }, storage);
        }

        public virtual void OnAppEnd()
        {
            _backgroundJobServer.Dispose();
        }
    }
}