using Bit.Hangfire.Contracts;
using Bit.Owin.Contracts;
using Hangfire;
using Hangfire.Dashboard;
using Owin;
using Bit.OwinCore.Contracts;
using Microsoft.AspNetCore.Builder;

namespace Bit.Hangfire
{
    public class JobSchedulerMiddlewareConfiguration : IAspNetCoreMiddlewareConfiguration
    {
        public virtual DashboardOptionsFactory DashboardOptionsFactory { get; set; } = default!;
        public virtual IJobSchedulerBackendConfiguration JobSchedulerBackendConfiguration { get; set; } = default!;
        public MiddlewarePosition MiddlewarePosition => MiddlewarePosition.BeforeOwinMiddlewares;
        public virtual void Configure(IApplicationBuilder app)
        {
            JobSchedulerBackendConfiguration.Init();

            app.UseHangfireDashboard("/jobs", DashboardOptionsFactory());
        }
    }
}
