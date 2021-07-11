﻿using Bit.Core.Contracts;
using Bit.Core.Models;
using Bit.Hangfire.Contracts;
using Hangfire;
using Hangfire.Dashboard;
using System;

namespace Bit.Hangfire.Implementations
{
    public static class DefaultHangfireFactories
    {
        public static IDependencyManager RegisterHangfireFactories(this IDependencyManager dependencyManager)
        {
            if (dependencyManager == null)
                throw new ArgumentNullException(nameof(dependencyManager));

            dependencyManager.RegisterUsing(resolver => new DashboardOptionsFactory(() => DashboardOptionsFactory(resolver)), lifeCycle: DependencyLifeCycle.Transient, overwriteExisting: false);

            return dependencyManager;
        }

        public static DashboardOptions DashboardOptionsFactory(IDependencyResolver resolver)
        {
            if (resolver == null)
                throw new ArgumentNullException(nameof(resolver));

            var appEnv = resolver.Resolve<AppEnvironment>();

            return new DashboardOptions
            {
                AsyncAuthorization = resolver.ResolveAll<IDashboardAsyncAuthorizationFilter>(),
                Authorization = resolver.ResolveAll<IDashboardAuthorizationFilter>(),
                AppPath = appEnv.GetHostVirtualPath(),
                DashboardTitle = $"Hangfire dashboard - {appEnv.Name} environment"
            };
        }
    }
}
