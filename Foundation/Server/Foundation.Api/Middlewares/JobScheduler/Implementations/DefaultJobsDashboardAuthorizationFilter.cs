using Foundation.Core.Contracts;
using Hangfire.Annotations;
using Hangfire.Dashboard;
using Microsoft.Owin;

namespace Foundation.Api.Middlewares.JobScheduler.Implementations
{
    public class DefaultJobsDashboardAuthorizationFilter : IDashboardAuthorizationFilter
    {
        public virtual bool Authorize([NotNull] DashboardContext context)
        {
            IOwinContext owinContext = new OwinContext(context.GetOwinEnvironment());
            return owinContext.GetDependencyResolver().Resolve<IUserInformationProvider>().IsAuthenticated();
        }
    }
}