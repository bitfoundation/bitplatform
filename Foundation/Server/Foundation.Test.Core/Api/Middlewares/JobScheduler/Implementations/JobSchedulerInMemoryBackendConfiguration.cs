using System;
using Foundation.Core.Contracts;
using Hangfire;
using Hangfire.MemoryStorage;
using Autofac;

namespace Foundation.Test.Api.Middlewares.JobScheduler.Implementations
{
    public class JobSchedulerInMemoryBackendConfiguration : IAppEvents
    {
        private BackgroundJobServer _backgroundJobServer;
        private readonly JobActivator _jobActivator;
        private readonly ILifetimeScope _container;

        protected JobSchedulerInMemoryBackendConfiguration()
        {
        }

        public JobSchedulerInMemoryBackendConfiguration(JobActivator jobActivator, ILifetimeScope container)
        {
            if (jobActivator == null)
                throw new ArgumentNullException(nameof(jobActivator));

            _jobActivator = jobActivator;

            if (container == null)
                throw new ArgumentNullException(nameof(container));

            _container = container;
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
            _backgroundJobServer.Dispose();
        }
    }
}
