using System;
using Bit.Core.Contracts;
using Hangfire.Annotations;
using Hangfire.Dashboard;
using Microsoft.Owin;

namespace Bit.Hangfire.Implementations
{
    public class HangfireJobsDashboardAuthorizationFilter : IDashboardAuthorizationFilter
    {
        public virtual bool Authorize([NotNull] DashboardContext context)
        {
            IOwinContext owinContext = new OwinContext(context.GetOwinEnvironment());
            IUserInformationProvider userInformationProvider = owinContext.GetDependencyResolver().Resolve<IUserInformationProvider>();
            bool isAuthenticated = userInformationProvider.IsAuthenticated();
            return isAuthenticated;
        }
    }
}
