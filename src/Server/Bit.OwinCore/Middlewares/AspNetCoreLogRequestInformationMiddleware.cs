using Bit.Core.Contracts;
using Bit.Owin.Middlewares;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using System.Threading.Tasks;

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
            ILogger logger = context.RequestServices.GetService<ILogger>();

            IRequestInformationProvider requestInformationProvider = context.RequestServices.GetService<IRequestInformationProvider>();

            LogRequestInformationMiddleware.LogRequest(logger, requestInformationProvider);

            return _next.Invoke(context);
        }
    }
}
