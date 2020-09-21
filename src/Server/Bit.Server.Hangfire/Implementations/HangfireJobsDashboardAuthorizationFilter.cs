using Bit.Core.Contracts;
using Hangfire.Annotations;
using Hangfire.Dashboard;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace Bit.Hangfire.Implementations
{
    public class HangfireJobsDashboardAuthorizationFilter : IDashboardAuthorizationFilter
    {
        public virtual bool Authorize([NotNull] DashboardContext context)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            IServiceProvider serviceProvider = GetServiceProvider(context);
            IUserInformationProvider userInformationProvider = serviceProvider.GetRequiredService<IUserInformationProvider>();
            bool isAuthenticated = userInformationProvider.IsAuthenticated();
            return isAuthenticated;
        }

        protected virtual IServiceProvider GetServiceProvider(DashboardContext context)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            return context.GetHttpContext().RequestServices;
        }
    }
}
