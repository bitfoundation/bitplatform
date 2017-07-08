using Bit.Core.Contracts;
using Hangfire.Annotations;
using Hangfire.Dashboard;
using Microsoft.Owin;

namespace Bit.Hangfire.Implementations
{
    public class DefaultJobsDashboardAuthorizationFilter : IDashboardAuthorizationFilter
    {
        public virtual bool Authorize([NotNull] DashboardContext context)
        {
            IOwinContext owinContext = new OwinContext(context.GetOwinEnvironment());
            bool isAuthenticated = owinContext.GetDependencyResolver().Resolve<IUserInformationProvider>().IsAuthenticated();
            return isAuthenticated;
        }
    }
}