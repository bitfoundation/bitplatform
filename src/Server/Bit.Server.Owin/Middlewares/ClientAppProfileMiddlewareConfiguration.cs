using System;
using Bit.Owin.Contracts;
using Bit.Owin.Models;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Bit.Owin.Middlewares
{
    public class ClientAppProfileMiddlewareConfiguration : IAspNetCoreMiddlewareConfiguration
    {
        public virtual MiddlewarePosition MiddlewarePosition => MiddlewarePosition.BeforeOwinMiddlewares;

        public virtual void Configure(IApplicationBuilder aspNetCoreApp)
        {
            if (aspNetCoreApp == null)
                throw new ArgumentNullException(nameof(aspNetCoreApp));

            aspNetCoreApp.Map("/ClientAppProfile", innerAspNetCoreApp =>
            {
                innerAspNetCoreApp.UseXContentTypeOptions();

                innerAspNetCoreApp.UseMiddleware<AspNetCoreNoCacheResponseMiddleware>();

                innerAspNetCoreApp.Run(async context =>
                {
                    if (context == null)
                        throw new ArgumentNullException(nameof(context));

                    IClientProfileModelProvider clientProfileModelProvider = context.RequestServices.GetService<IClientProfileModelProvider>();

                    ClientProfileModel clientProfileModel = await clientProfileModelProvider.GetClientProfileModelAsync(context.RequestAborted).ConfigureAwait(false);

                    string clientAppProfileJson = clientProfileModel.ToJavaScriptObject();

                    context.Response.ContentType = "text/javascript; charset=utf-8";

                    await context.Response.WriteAsync(clientAppProfileJson, context.RequestAborted).ConfigureAwait(false);
                });
            });
        }
    }
}
