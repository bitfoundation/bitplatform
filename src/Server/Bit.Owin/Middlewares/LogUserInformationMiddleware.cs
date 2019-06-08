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
                BitJwtToken bitJwtToken = userInformationProvider.GetBitJwtToken();
                logger.AddLogData("UserId", bitJwtToken.UserId);
                if (bitJwtToken.CustomProps != null)
                {
                    foreach (var keyVal in bitJwtToken.CustomProps)
                    {
                        logger.AddLogData(keyVal.Key, keyVal.Value);
                    }
                }
                logger.AddLogData("AuthenticationType", userInformationProvider.GetAuthenticationType());
                logger.AddLogData("ClientId", userInformationProvider.GetClientId());
            }
        }
    }
}
