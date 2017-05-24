using Foundation.Api.Contracts;
using Foundation.Api.Implementations;
using Foundation.Api.Middlewares.JobScheduler;
using Foundation.Api.Middlewares.JobScheduler.Implementations;
using Hangfire;
using Hangfire.Dashboard;
using System;

namespace Foundation.Core.Contracts
{
    public static class IDependencyManangerExtensions
    {
        public static IDependencyManager RegisterBackgroundJobWorkerUsingDefaultConfiguration<TJobSchedulerInMemoryBackendConfiguration>(this IDependencyManager dependencyManager)
            where TJobSchedulerInMemoryBackendConfiguration : class, IAppEvents
        {
            if (dependencyManager == null)
                throw new ArgumentNullException(nameof(dependencyManager));

            dependencyManager.Register<IDashboardAuthorizationFilter, DefaultJobsDashboardAuthorizationFilter>(lifeCycle: DependencyLifeCycle.SingleInstance, overwriteExciting: false);
            dependencyManager.RegisterOwinMiddleware<JobSchedulerMiddlewareConfiguration>();
            dependencyManager.RegisterAppEvents<TJobSchedulerInMemoryBackendConfiguration>();
            dependencyManager.Register<IBackgroundJobWorker, DefaultBackgroundJobWorker>(lifeCycle: DependencyLifeCycle.SingleInstance);
            dependencyManager.Register<JobActivator, AutofacJobActivator>(lifeCycle: DependencyLifeCycle.SingleInstance);


            return dependencyManager;
        }
    }
}
