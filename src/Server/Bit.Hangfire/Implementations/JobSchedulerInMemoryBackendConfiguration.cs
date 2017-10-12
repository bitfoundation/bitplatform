using Autofac;
using Bit.Core.Contracts;
using Bit.Owin.Contracts;
using Hangfire;
using Hangfire.Logging;
using Hangfire.MemoryStorage;
using System;

namespace Bit.Hangfire.Implementations
{
    public class JobSchedulerInMemoryBackendConfiguration : IAppEvents
    {
        private ILifetimeScope _container;

        private BackgroundJobServer _backgroundJobServer;

        public virtual ILogProvider LogProvider { get; set; }

        public virtual IAutofacDependencyManager DependencyManager
        {
            set
            {
                if (value == null)
                    throw new ArgumentNullException(nameof(DependencyManager));
                _container = value.GetContainer();
            }
        }

        public virtual JobActivator JobActivator { get; set; }

        public virtual void OnAppStartup()
        {
            MemoryStorage storage = new MemoryStorage();

            GlobalConfiguration.Configuration.UseStorage(storage);
            GlobalConfiguration.Configuration.UseAutofacActivator(_container);
            GlobalConfiguration.Configuration.UseLogProvider(LogProvider);

            _backgroundJobServer = new BackgroundJobServer(new BackgroundJobServerOptions
            {
                Activator = JobActivator
            }, storage);
        }

        public virtual void OnAppEnd()
        {
            _backgroundJobServer?.Dispose();
        }
    }
}
