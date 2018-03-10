using Bit.Core.Contracts;
using Bit.Owin.Contracts;
using Bit.Owin.Models;
using Microsoft.Owin;
using NWebsec.Owin;
using Owin;

namespace Bit.Owin.Middlewares
{
    public class ClientAppProfileMiddlewareConfiguration : IOwinMiddlewareConfiguration
    {
        public virtual void Configure(IAppBuilder owinApp)
        {
            owinApp.Map("/ClientAppProfile", innerOwinApp =>
            {
                innerOwinApp.UseXContentTypeOptions();

                innerOwinApp.Use<OwinNoCacheResponseMiddleware>();

                innerOwinApp.Run(async context =>
                {
                    IDependencyResolver dependencyResolver = context.GetDependencyResolver();

                    IClientProfileAppModelProvider pageModelProvider = dependencyResolver.Resolve<IClientProfileAppModelProvider>();

                    ClientAppProfileModel clientAppProfileModel = await pageModelProvider.GetClientAppProfileModelAsync(context.Request.CallCancelled);

                    string clientAppProfile = $@"
clientAppProfile = {{
    theme: ""{clientAppProfileModel.Theme}"",
    culture: ""{clientAppProfileModel.Culture}"",
    version: ""{clientAppProfileModel.AppVersion}"",
    isDebugMode: {clientAppProfileModel.DebugMode.ToString().ToLowerInvariant()},
    appTitle: ""{clientAppProfileModel.AppTitle}"",
    appName: ""{clientAppProfileModel.AppName}"",
    desiredTimeZone: ""{clientAppProfileModel.DesiredTimeZoneValue}"",
    environmentConfigs: {clientAppProfileModel.EnvironmentConfigsJson}
}};

                ";

                    context.Response.ContentType = "text/javascript; charset=utf-8";

                    await context.Response.WriteAsync(clientAppProfile, context.Request.CallCancelled);
                });
            });
        }
    }
}
