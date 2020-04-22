using Bit.Core.Contracts;
using Bit.Owin.Middlewares;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;

namespace Bit.Owin.Middlewares
{
    public class AspNetCoreLogUserInformationMiddleware
    {
        private readonly RequestDelegate _next;

        public AspNetCoreLogUserInformationMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public Task Invoke(HttpContext context)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            ILogger logger = context.RequestServices.GetRequiredService<ILogger>();

            IUserInformationProvider userInformationProvider = context.RequestServices.GetRequiredService<IUserInformationProvider>();

            LogUserInformationMiddleware.LogUserInformation(logger, userInformationProvider);

            return _next.Invoke(context);
        }
    }
}
