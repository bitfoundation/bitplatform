using Bit.Core.Contracts;
using Bit.OwinCore.Contracts;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Owin;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace Bit.OwinCore.Middlewares
{
    public class GetRequestInfoMiddleware
    {
        private readonly RequestDelegate _next;

        public GetRequestInfoMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext context)
        {
            await context.Response.WriteAsync(GetRequestInfo(context.RequestServices), context.RequestAborted);
        }

        string GetRequestInfo(IServiceProvider serviceProvider)
        {
            var requestInformationProvider = serviceProvider.GetRequiredService<IRequestInformationProvider>();
            var contentFormatter = serviceProvider.GetRequiredService<IContentFormatter>();

            return contentFormatter.Serialize(new
            {
                Url = requestInformationProvider.DisplayUrl,
                requestInformationProvider.ClientIp
            });
        }
    }

    public class AspNetCoreGetRequestInfoMiddlewareConfiguration : IAspNetCoreMiddlewareConfiguration
    {
        public MiddlewarePosition MiddlewarePosition => MiddlewarePosition.BeforeOwinMiddlewares;

        public void Configure(IApplicationBuilder aspNetCoreApp)
        {
            aspNetCoreApp.Map("/get-request-info", innerOwinApp =>
            {
                innerOwinApp.UseMiddleware<GetRequestInfoMiddleware>();
            });
        }
    }
}
