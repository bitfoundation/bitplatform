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

            LogUserInformation(logger, userInformationProvider);

            await Next.Invoke(context);
        }

        public static void LogUserInformation(ILogger logger, IUserInformationProvider userInformationProvider)
        {
            if (userInformationProvider.IsAuthenticated())
            {
                logger.AddLogData("UserId", userInformationProvider.GetCurrentUserId());
                logger.AddLogData("AuthenticationType", userInformationProvider.GetAuthenticationType());
            }
        }
    }
}
