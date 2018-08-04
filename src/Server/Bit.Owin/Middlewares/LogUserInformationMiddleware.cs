using Bit.Core.Contracts;
using Microsoft.Owin;
using System.Threading.Tasks;

namespace Bit.Owin.Middlewares
{
    public class LogUserInformationMiddleware : OwinMiddleware
    {
        public LogUserInformationMiddleware(OwinMiddleware next)
            : base(next)
        {
        }

        public override Task Invoke(IOwinContext context)
        {
            IDependencyResolver dependencyResolver = context.GetDependencyResolver();

            ILogger logger = dependencyResolver.Resolve<ILogger>();

            IUserInformationProvider userInformationProvider =
                dependencyResolver.Resolve<IUserInformationProvider>();

            LogUserInformation(logger, userInformationProvider);

            return Next.Invoke(context);
        }

        public static void LogUserInformation(ILogger logger, IUserInformationProvider userInformationProvider)
        {
            if (userInformationProvider.IsAuthenticated())
            {
                logger.AddLogData("UserId", userInformationProvider.GetCurrentUserId());
                logger.AddLogData("AuthenticationType", userInformationProvider.GetAuthenticationType());
                logger.AddLogData("ClientId", userInformationProvider.GetClientId());
            }
        }
    }
}
