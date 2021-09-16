using Bit.Core.Contracts;
using Bit.Core.Models;
using Microsoft.Owin;
using System;
using System.Linq;
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

        private static readonly string[] toBeLogIgnoredClaims = new[] { "nbf", "exp", "iss", "aud", "client_id", "scope", "auth_time", "idp", "name", "jti", "amr", "sub" };

        public static void LogUserInformation(ILogger logger, IUserInformationProvider userInformationProvider)
        {
            if (userInformationProvider == null)
                throw new ArgumentNullException(nameof(userInformationProvider));

            if (logger == null)
                throw new ArgumentNullException(nameof(logger));

            if (userInformationProvider.IsAuthenticated())
            {
                BitJwtToken bitJwtToken = userInformationProvider.GetBitJwtToken();
                logger.AddLogData("UserId", bitJwtToken.UserId);
                if (bitJwtToken.Claims != null)
                {
                    foreach (var keyVal in bitJwtToken.Claims)
                    {
                        if (!toBeLogIgnoredClaims.Contains(keyVal.Key))
                            logger.AddLogData(keyVal.Key, keyVal.Value);
                    }
                }
                logger.AddLogData("AuthenticationType", userInformationProvider.GetAuthenticationType());
                logger.AddLogData("ClientId", userInformationProvider.GetClientId());
            }
        }
    }
}
