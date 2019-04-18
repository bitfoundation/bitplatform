using System.Threading.Tasks;
using Bit.Core.Contracts;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Bit.Owin.Middlewares;

namespace Bit.OwinCore.Middlewares
{
    public class AspNetCoreLogRequestInformationMiddleware
    {
        private readonly RequestDelegate _next;

        public AspNetCoreLogRequestInformationMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public Task Invoke(HttpContext context)
        {
            ILogger logger = context.RequestServices.GetRequiredService<ILogger>();

            IRequestInformationProvider requestInformationProvider = context.RequestServices.GetRequiredService<IRequestInformationProvider>();

            LogRequestInformationMiddleware.LogRequest(logger, requestInformationProvider);

            return _next.Invoke(context);
        }
    }
}