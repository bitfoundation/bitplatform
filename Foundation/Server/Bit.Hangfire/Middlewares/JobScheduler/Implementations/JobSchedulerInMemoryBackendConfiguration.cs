using System;
using Autofac;
using Bit.Core.Contracts;
using Bit.Owin.Contracts;
using Hangfire;
using Hangfire.MemoryStorage;

namespace Bit.Hangfire.Middlewares.JobScheduler.Implementations
{
    public class JobSchedulerInMemoryBackendConfiguration : IAppEvents
    {
        private BackgroundJobServer _backgroundJobServer;
        private readonly JobActivator _jobActivator;
        private readonly ILifetimeScope _container;

        protected JobSchedulerInMemoryBackendConfiguration()
        {
        }

        public JobSchedulerInMemoryBackendConfiguration(JobActivator jobActivator, IAutofacDependencyManager dependencyManager)
        {
            if (jobActivator == null)
                throw new ArgumentNullException(nameof(jobActivator));

            _jobActivator = jobActivator;

            if (dependencyManager == null)
                throw new ArgumentNullException(nameof(dependencyManager));

            _container = dependencyManager.GetContainer();
        }

        public virtual void OnAppStartup()
        {
            MemoryStorage storage = new MemoryStorage();

            GlobalConfiguration.Configuration.UseStorage(storage);
            GlobalConfiguration.Configuration.UseAutofacActivator(_container);

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
