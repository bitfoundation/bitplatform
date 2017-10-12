using System;
using System.Collections.Generic;
using Bit.Core.Contracts;
using Bit.Owin.Contracts;
using Hangfire;
using Hangfire.Dashboard;
using Owin;

namespace Bit.Hangfire
{
    public class JobSchedulerMiddlewareConfiguration :
#if NET461
        IOwinMiddlewareConfiguration
#else
        OwinCore.Contracts.IAspNetCoreMiddlewareConfiguration
#endif
    {
        public virtual IEnumerable<IDashboardAuthorizationFilter> AuthFilters { get; set; }
        public virtual IAppEnvironmentProvider AppEnvironmentProvider { get; set; }

#if NET461
        public virtual void Configure(IAppBuilder owinApp)
        {
            owinApp.UseHangfireDashboard("/jobs", new DashboardOptions
            {
                Authorization = AuthFilters,
                AppPath = AppEnvironmentProvider.GetActiveAppEnvironment().GetHostVirtualPath()
            });
        }
#else
        public virtual void Configure(Microsoft.AspNetCore.Builder.IApplicationBuilder aspNetCoreApp)
        {
            aspNetCoreApp.UseHangfireDashboard("/jobs", new DashboardOptions
            {
                Authorization = AuthFilters,
                AppPath = AppEnvironmentProvider.GetActiveAppEnvironment().GetHostVirtualPath()
            });
        }
#endif
    }
}