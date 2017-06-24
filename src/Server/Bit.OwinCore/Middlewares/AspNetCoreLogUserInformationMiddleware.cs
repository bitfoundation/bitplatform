using Bit.Core.Contracts;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using System.Threading.Tasks;

namespace Bit.OwinCore.Middlewares
{
    public class AspNetCoreLogUserInformationMiddleware
    {
        private readonly RequestDelegate Next;

        public AspNetCoreLogUserInformationMiddleware(RequestDelegate next)
        {
            Next = next;
        }

        public async Task Invoke(HttpContext context)
        {
            ILogger logger = context.RequestServices.GetService<ILogger>();

            IUserInformationProvider userInformationProvider = context.RequestServices.GetService<IUserInformationProvider>();

            if (userInformationProvider.IsAuthenticated())
            {
                logger.AddLogData("UserId", userInformationProvider.GetCurrentUserId());
                logger.AddLogData("AuthenticationType", userInformationProvider.GetAuthenticationType());
            }

            await Next.Invoke(context);
        }
    }
}
