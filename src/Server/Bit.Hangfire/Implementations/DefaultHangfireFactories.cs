using Bit.Core.Contracts;
using Bit.Core.Models;
using Bit.Hangfire.Contracts;
using Hangfire;
using Hangfire.Dashboard;

namespace Bit.Hangfire.Implementations
{
    public static class DefaultHangfireFactories
    {
        public static IDependencyManager RegisterHangfireFactories(this IDependencyManager dependencyManager)
        {
            dependencyManager.RegisterUsing(resolver => new DashboardOptionsFactory(() => DashboardOptionsFactory(resolver)), lifeCycle: DependencyLifeCycle.Transient, overwriteExisting: false);

            return dependencyManager;
        }

        public static DashboardOptions DashboardOptionsFactory(IDependencyResolver resolver)
        {
            var appEnv = resolver.Resolve<AppEnvironment>();

            return new DashboardOptions
            {
                Authorization = resolver.ResolveAll<IDashboardAuthorizationFilter>(),
                AppPath = appEnv.GetHostVirtualPath(),
                DashboardTitle = $"Hangfire dashboard - {appEnv.Name} environment"
            };
        }
    }
}
