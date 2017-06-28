using System;
using Autofac;
using Bit.Core.Contracts;
using Bit.Owin.Contracts;
using Hangfire;
using Hangfire.Logging;
using Hangfire.MemoryStorage;

namespace Bit.Hangfire.Implementations
{
    public class JobSchedulerInMemoryBackendConfiguration : IAppEvents
    {
        private BackgroundJobServer _backgroundJobServer;
        private readonly JobActivator _jobActivator;
        private readonly ILifetimeScope _container;
        private readonly ILogProvider _logProvider;

        protected JobSchedulerInMemoryBackendConfiguration()
        {
        }

        public JobSchedulerInMemoryBackendConfiguration(JobActivator jobActivator, IAutofacDependencyManager dependencyManager, ILogProvider logProvider)
        {
            if (jobActivator == null)
                throw new ArgumentNullException(nameof(jobActivator));

            if (dependencyManager == null)
                throw new ArgumentNullException(nameof(dependencyManager));

            if (logProvider == null)
                throw new ArgumentNullException(nameof(logProvider));

            _logProvider = logProvider;

            _jobActivator = jobActivator;

            _container = dependencyManager.GetContainer();
        }

        public virtual void OnAppStartup()
        {
            MemoryStorage storage = new MemoryStorage();

            GlobalConfiguration.Configuration.UseStorage(storage);
            GlobalConfiguration.Configuration.UseAutofacActivator(_container);
            GlobalConfiguration.Configuration.UseLogProvider(_logProvider);

            _backgroundJobServer = new BackgroundJobServer(new BackgroundJobServerOptions
            {
                Activator = _jobActivator
            }, storage);
        }

        public virtual void OnAppEnd()
        {
            _backgroundJobServer?.Dispose();
        }
    }
}
