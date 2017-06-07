using System.Threading.Tasks;
using Bit.Core.Contracts;
using Microsoft.Owin;

namespace Bit.Owin.Middlewares
{
    public class LogUserInformationMiddleware : OwinMiddleware
    {
        public LogUserInformationMiddleware(OwinMiddleware next)
            : base(next)
        {
        }

        public override async Task Invoke(IOwinContext context)
        {
            IDependencyResolver dependencyResolver = context.GetDependencyResolver();

            ILogger logger = dependencyResolver.Resolve<ILogger>();

            IUserInformationProvider userInformationProvider =
                dependencyResolver.Resolve<IUserInformationProvider>();

            if (userInformationProvider.IsAuthenticated())
            {
                logger.AddLogData("UserId", userInformationProvider.GetCurrentUserId());
                logger.AddLogData("AuthenticationType", userInformationProvider.GetAuthenticationType());
            }

            await Next.Invoke(context);
        }
    }
}
