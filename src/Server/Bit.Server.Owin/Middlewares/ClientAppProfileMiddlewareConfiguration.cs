using Bit.Core.Contracts;
using Bit.Owin.Contracts;
using Bit.Owin.Models;
using Microsoft.Owin;
using Owin;
using System;

namespace Bit.Owin.Middlewares
{
    public class ClientAppProfileMiddlewareConfiguration : IOwinMiddlewareConfiguration
    {
        public virtual void Configure(IAppBuilder owinApp)
        {
            if (owinApp == null)
                throw new ArgumentNullException(nameof(owinApp));

            owinApp.Map("/ClientAppProfile", innerOwinApp =>
            {
                innerOwinApp.UseXContentTypeOptions();

                innerOwinApp.Use<OwinNoCacheResponseMiddleware>();

                innerOwinApp.Run(async context =>
                {
                    if (context == null)
                        throw new ArgumentNullException(nameof(context));

                    IDependencyResolver dependencyResolver = context.GetDependencyResolver();

                    IClientProfileModelProvider clientProfileModelProvider = dependencyResolver.Resolve<IClientProfileModelProvider>();

                    ClientProfileModel clientProfileModel = await clientProfileModelProvider.GetClientProfileModelAsync(context.Request.CallCancelled);

                    string clientAppProfileJson = clientProfileModel.ToJavaScriptObject();

                    context.Response.ContentType = "text/javascript; charset=utf-8";

                    await context.Response.WriteAsync(clientAppProfileJson, context.Request.CallCancelled);
                });
            });
        }
    }
}
