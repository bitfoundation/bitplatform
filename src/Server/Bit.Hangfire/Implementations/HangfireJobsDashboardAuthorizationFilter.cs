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
            IUserInformationProvider userInformationProvider = null;
#if NET461
            IOwinContext owinContext = new OwinContext(context.GetOwinEnvironment());
            userInformationProvider = owinContext.GetDependencyResolver().Resolve<IUserInformationProvider>();
#else
            userInformationProvider = Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetService<IUserInformationProvider>(context.GetHttpContext().RequestServices);
#endif
            bool isAuthenticated = userInformationProvider.IsAuthenticated();
            return isAuthenticated;
        }
    }
}