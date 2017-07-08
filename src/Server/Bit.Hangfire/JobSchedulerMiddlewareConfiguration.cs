using System;
using System.Collections.Generic;
using Bit.Core.Contracts;
using Bit.Owin.Contracts;
using Hangfire;
using Hangfire.Dashboard;
using Owin;

namespace Bit.Hangfire
{
    public class JobSchedulerMiddlewareConfiguration : IOwinMiddlewareConfiguration
    {
        private readonly IEnumerable<IDashboardAuthorizationFilter> _authFilters;
        private readonly IAppEnvironmentProvider _appEnvironmentProvider;

        public JobSchedulerMiddlewareConfiguration(IEnumerable<IDashboardAuthorizationFilter> authFilters, IAppEnvironmentProvider appEnvironmentProvider)
        {
            if (authFilters == null)
                throw new ArgumentNullException(nameof(authFilters));

            _authFilters = authFilters;

            if (appEnvironmentProvider == null)
                throw new ArgumentNullException(nameof(appEnvironmentProvider));

            _appEnvironmentProvider = appEnvironmentProvider;
        }

        protected JobSchedulerMiddlewareConfiguration()
        {

        }

        public virtual void Configure(IAppBuilder owinApp)
        {
            owinApp.UseHangfireDashboard("/jobs", new DashboardOptions
            {
                Authorization = _authFilters,
                AppPath = _appEnvironmentProvider.GetActiveAppEnvironment().GetHostVirtualPath()
            });
        }
    }
}