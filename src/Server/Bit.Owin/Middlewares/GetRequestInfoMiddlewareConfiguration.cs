using Bit.Core.Contracts;
using Owin;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using System;
using Microsoft.Owin;
using Bit.Owin.Contracts;

namespace Bit.Owin.Middlewares
{
    public class GetRequestInfoMiddleware : OwinMiddleware
    {
        public GetRequestInfoMiddleware(OwinMiddleware next) : base(next)
        {
        }

        public override async Task Invoke(IOwinContext context)
        {
            await context.Response.WriteAsync(GetRequestInfo(context.GetDependencyResolver().Resolve<IServiceProvider>()), context.Request.CallCancelled);
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

    public class GetRequestInfoMiddlewareConfiguration : IOwinMiddlewareConfiguration
    {
        public void Configure(IAppBuilder owinApp)
        {
            owinApp.Map("/get-request-info", innerOwinApp =>
            {
                innerOwinApp.Use<GetRequestInfoMiddleware>();
            });
        }
    }
}
