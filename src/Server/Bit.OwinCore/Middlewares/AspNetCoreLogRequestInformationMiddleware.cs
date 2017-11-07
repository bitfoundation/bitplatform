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

        public async Task Invoke(HttpContext context)
        {
            ILogger logger = context.RequestServices.GetService<ILogger>();

            IRequestInformationProvider requestInformationProvider = context.RequestServices.GetService<IRequestInformationProvider>();

            LogRequestInformationMiddleware.LogRequest(logger, requestInformationProvider);

            await _next.Invoke(context);
        }
    }
}