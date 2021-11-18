using Bit.Hangfire.Contracts;
using Bit.Hangfire.Filters;
using Hangfire;
using Hangfire.Logging;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Bit.Hangfire.Implementations
{
    public abstract class JobSchedulerBaseBackendConfiguration<TStorage> : IJobSchedulerBackendConfiguration
        where TStorage : JobStorage
    {
        public virtual JobActivator JobActivator { get; set; } = default!;

        public virtual ILogProvider LogProvider { get; set; } = default!;

        public virtual IEnumerable<IHangfireOptionsCustomizer> Customizers { get; set; } = default!;

        protected virtual BackgroundJobServer? BackgroundJobServer { get; set; }

        protected abstract TStorage BuildStorage();

        public virtual void Init()
        {
            var storage = BuildStorage();

            JobStorage.Current = storage;

            GlobalConfiguration.Configuration
                .SetDataCompatibilityLevel(CompatibilityLevel.Version_170)
                .UseSimpleAssemblyNameTypeSerializer()
                .UseRecommendedSerializerSettings()
                .UseStorage(storage)
                .UseLogProvider(LogProvider);

            if (GlobalJobFilters.Filters.Any(f => f.Instance is AutomaticRetryAttribute))
            {
                GlobalConfiguration.Configuration.UseFilter(new DefaultAutomaticRetryAttribute { });
                GlobalJobFilters.Filters.Remove(GlobalJobFilters.Filters.ExtendedSingle("Finding automatic retry job filter attribute to remove it from global job filters", f => f.Instance is AutomaticRetryAttribute).Instance);
            }

            BackgroundJobServerOptions options = new BackgroundJobServerOptions
            {
                Activator = JobActivator
            };

            foreach (IHangfireOptionsCustomizer customizer in Customizers)
            {
                customizer.Customize(GlobalConfiguration.Configuration, options, storage);
            }

            BuildBackgroundJobServer(storage, options);
        }

        protected virtual void BuildBackgroundJobServer(TStorage storage, BackgroundJobServerOptions options)
        {
            BackgroundJobServer = new BackgroundJobServer(options, storage);
        }

        private bool isDisposed;

        public virtual void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (isDisposed) return;

            if (disposing)
                BackgroundJobServer?.Stop(force: true);

            BackgroundJobServer?.Dispose();

            isDisposed = true;
        }

        ~JobSchedulerBaseBackendConfiguration()
        {
            Dispose(false);
        }
    }
}
