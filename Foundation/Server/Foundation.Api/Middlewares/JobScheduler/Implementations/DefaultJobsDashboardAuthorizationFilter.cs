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
            bool isAuthenticated = owinContext.GetDependencyResolver().Resolve<IUserInformationProvider>().IsAuthenticated();
            if (isAuthenticated == false && owinContext.Authentication?.User == null)
            {
                // https://github.com/HangfireIO/Hangfire/issues/833
                owinContext.Authentication.User = new System.Security.Claims.ClaimsPrincipal { };
                owinContext.Authentication.User.AddIdentity(new System.Security.Claims.ClaimsIdentity { });
            };
            return isAuthenticated;
        }
    }
}