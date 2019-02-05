using System;
using System.Collections.Generic;
using Bit.Core.Models;
using Bit.Owin.Contracts;
using Hangfire;
using Hangfire.Dashboard;
using Owin;

namespace Bit.Hangfire
{
    public class JobSchedulerMiddlewareConfiguration : IOwinMiddlewareConfiguration
    {
        public virtual IEnumerable<IDashboardAuthorizationFilter> AuthFilters { get; set; }
        public virtual AppEnvironment AppEnvironment { get; set; }

        public virtual void Configure(IAppBuilder owinApp)
        {
            owinApp.UseHangfireDashboard("/jobs", new DashboardOptions
            {
                Authorization = AuthFilters,
                AppPath = AppEnvironment.GetHostVirtualPath()
            });
        }
    }
}
