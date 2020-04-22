using Bit.Hangfire;
using Bit.Hangfire.Contracts;
using Bit.Hangfire.Implementations;
using Hangfire;
using Hangfire.Dashboard;
using Hangfire.Logging;
using System;

namespace Bit.Core.Contracts
{
    public static class IDependencyManagerExtensions
    {
        public static IDependencyManager RegisterHangfireBackgroundJobWorkerUsingDefaultConfiguration<TJobSchedulerBackendConfiguration>(this IDependencyManager dependencyManager)
            where TJobSchedulerBackendConfiguration : class, IJobSchedulerBackendConfiguration
        {
            if (dependencyManager == null)
                throw new ArgumentNullException(nameof(dependencyManager));

            return dependencyManager.RegisterHangfireBackgroundJobWorkerUsingDefaultConfigurationInternal<TJobSchedulerBackendConfiguration>();
        }

        static IDependencyManager RegisterHangfireBackgroundJobWorkerUsingDefaultConfigurationInternal<TJobSchedulerBackendConfiguration>(this IDependencyManager dependencyManager)
            where TJobSchedulerBackendConfiguration : class, IJobSchedulerBackendConfiguration
        {
            dependencyManager.Register<ILogProvider, HangfireBackgroundJobWorkerLogProvider>(overwriteExisting: false);
            dependencyManager.Register<IDashboardAuthorizationFilter, HangfireJobsDashboardAuthorizationFilter>(lifeCycle: DependencyLifeCycle.SingleInstance, overwriteExisting: false);

            dependencyManager.RegisterAspNetCoreMiddleware<JobSchedulerMiddlewareConfiguration>();

            var services = ((IServiceCollectionAccessor)dependencyManager).ServiceCollection;

            services.AddHangfire((serviceProvider, config) =>
            {
            });

            dependencyManager.Register<IJobSchedulerBackendConfiguration, TJobSchedulerBackendConfiguration>(lifeCycle: DependencyLifeCycle.SingleInstance);

            dependencyManager.Register<IBackgroundJobWorker, HangfireBackgroundJobWorker>(lifeCycle: DependencyLifeCycle.SingleInstance, overwriteExisting: false);

            dependencyManager.Register<JobActivator, global::Hangfire.AspNetCore.AspNetCoreJobActivator>(lifeCycle: DependencyLifeCycle.SingleInstance, overwriteExisting: false);

            dependencyManager.RegisterHangfireFactories();

            return dependencyManager;
        }
    }
}
