using System;
using Bit.Hangfire;
using Bit.Hangfire.Implementations;
using Hangfire;
using Hangfire.Dashboard;
using Hangfire.Logging;

namespace Bit.Core.Contracts
{
    public static class IDependencyManagerExtensions
    {
        public static IDependencyManager RegisterHangfireBackgroundJobWorkerUsingDefaultConfiguration<TJobSchedulerBackendConfiguration>(this IDependencyManager dependencyManager)
            where TJobSchedulerBackendConfiguration : class, IAppEvents
        {
            if (dependencyManager == null)
                throw new ArgumentNullException(nameof(dependencyManager));

            dependencyManager.Register<ILogProvider, HangfireBackgroundJobWorkerLogProvider>(overwriteExciting: false);
            dependencyManager.Register<IDashboardAuthorizationFilter, HangfireJobsDashboardAuthorizationFilter>(lifeCycle: DependencyLifeCycle.SingleInstance, overwriteExciting: false);
            dependencyManager.RegisterOwinMiddleware<JobSchedulerMiddlewareConfiguration>();
            dependencyManager.RegisterAppEvents<TJobSchedulerBackendConfiguration>();
            dependencyManager.Register<IBackgroundJobWorker, HangfireBackgroundJobWorker>(lifeCycle: DependencyLifeCycle.SingleInstance, overwriteExciting: false);
            dependencyManager.Register<JobActivator, AutofacJobActivator>(lifeCycle: DependencyLifeCycle.SingleInstance, overwriteExciting: false);

            return dependencyManager;
        }
    }
}
