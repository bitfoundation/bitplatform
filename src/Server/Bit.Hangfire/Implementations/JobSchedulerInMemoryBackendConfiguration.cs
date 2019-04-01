using Autofac;
using Bit.Core.Contracts;
using Bit.Hangfire.Contracts;
using Bit.Owin.Contracts;
using Hangfire;
using Hangfire.Logging;
using Hangfire.MemoryStorage;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace Bit.Hangfire.Implementations
{
    public class JobSchedulerInMemoryBackendConfiguration : IAppEvents
    {
        private ILifetimeScope _container;

        private BackgroundJobServer _backgroundJobServer;

        public virtual ILogProvider LogProvider { get; set; }

        public virtual IEnumerable<IHangfireOptionsCustomizer> Customizers { get; set; }

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

            typeof(BackgroundJob)
                .GetProperty("ClientFactory", BindingFlags.NonPublic | BindingFlags.Static)
                .SetValue(null, new Func<IBackgroundJobClient>(() => new BackgroundJobClient()));

            JobStorage.Current = storage;
            GlobalConfiguration.Configuration.UseStorage(storage);
            GlobalConfiguration.Configuration.UseAutofacActivator(_container);
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
