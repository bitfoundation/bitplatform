using Foundation.Api.Contracts;
using Hangfire;
using Hangfire.Dashboard;
using Owin;
using System;
using System.Collections.Generic;

namespace Foundation.Api.Middlewares.JobScheduler
{
    public class JobSchedulerMiddlewareConfiguration : IOwinMiddlewareConfiguration
    {
        private readonly IEnumerable<IDashboardAuthorizationFilter> _authFilters;

        public JobSchedulerMiddlewareConfiguration(IEnumerable<IDashboardAuthorizationFilter> authFilters)
        {
            if (authFilters == null)
                throw new ArgumentNullException(nameof(authFilters));

            _authFilters = authFilters;
        }

        protected JobSchedulerMiddlewareConfiguration()
        {

        }

        public virtual void Configure(IAppBuilder owinApp)
        {
            owinApp.UseHangfireDashboard("/jobs", new DashboardOptions
            {
                Authorization = _authFilters
            });
        }
    }
}