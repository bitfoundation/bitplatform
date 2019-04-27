using Bit.Core.Extensions;
using Bit.Hangfire;
using Bit.Hangfire.Implementations;
using Hangfire;
using Hangfire.Dashboard;
using Hangfire.Logging;
using System;
using System.IO;

namespace Bit.Core.Contracts
{
    public static class IDependencyManagerExtensions
    {
        public static IDependencyManager RegisterHangfireBackgroundJobWorkerUsingDefaultConfiguration<TJobSchedulerBackendConfiguration>(this IDependencyManager dependencyManager)
            where TJobSchedulerBackendConfiguration : class, IAppEvents
        {
            if (dependencyManager == null)
                throw new ArgumentNullException(nameof(dependencyManager));

            try
            {
                return dependencyManager.RegisterHangfireBackgroundJobWorkerUsingDefaultConfigurationInternal<TJobSchedulerBackendConfiguration>();
            }
            catch (FileNotFoundException exp) when (PlatformUtilities.IsRunningOnDotNetCore && exp.Message.StartsWith("Could not load file or assembly 'Hangfire."))
            {
                throw new InvalidOperationException("Please install Bit.Hangfire.AspNetCore to your asp.net core & test projects if any.", exp);
            }
        }

        static IDependencyManager RegisterHangfireBackgroundJobWorkerUsingDefaultConfigurationInternal<TJobSchedulerBackendConfiguration>(this IDependencyManager dependencyManager)
            where TJobSchedulerBackendConfiguration : class, IAppEvents
        {
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
