using Bit.Core.Contracts;
using Bit.Owin.Contracts;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Owin;
using System;
using System.Threading.Tasks;

namespace Bit.Owin.Middlewares
{
    public class GetRequestInfoMiddleware
    {
        public GetRequestInfoMiddleware(RequestDelegate next)
        {
        }

        public async Task Invoke(HttpContext context)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

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
