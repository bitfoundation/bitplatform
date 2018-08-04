using Bit.Core.Contracts;
using Hangfire.Annotations;
using Hangfire.Dashboard;
using Microsoft.Owin;
using System;

namespace Bit.Hangfire.Implementations
{
    public class HangfireJobsDashboardAuthorizationFilter : IDashboardAuthorizationFilter
    {
        public virtual bool Authorize([NotNull] DashboardContext context)
        {
#if DotNetCore
            throw new NotImplementedException();
#else
            IUserInformationProvider userInformationProvider = null;
            IOwinContext owinContext = new OwinContext(context.GetOwinEnvironment());
            userInformationProvider = owinContext.GetDependencyResolver().Resolve<IUserInformationProvider>();
            bool isAuthenticated = userInformationProvider.IsAuthenticated();
            return isAuthenticated;
#endif
        }
    }
}
