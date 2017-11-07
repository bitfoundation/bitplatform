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

                    IDefaultPageModelProvider defaultPageModelProvider = dependencyResolver.Resolve<IDefaultPageModelProvider>();

                    DefaultPageModel model = await defaultPageModelProvider.GetDefaultPageModelAsync(context.Request.CallCancelled);

                    string clientAppProfile = $@"
clientAppProfile = {{
    theme: ""{model.Theme}"",
    culture: ""{model.Culture}"",
    version: ""{model.AppVersion}"",
    isDebugMode: {model.DebugMode.ToString().ToLowerInvariant()},
    appTitle: ""{model.AppTitle}"",
    appName: ""{model.AppName}"",
    desiredTimeZone: ""{model.DesiredTimeZoneValue}"",
    environmentConfigs: {model.EnvironmentConfigsJson}
}};

                ";

                    context.Response.ContentType = "text/javascript; charset=utf-8";

                    await context.Response.WriteAsync(clientAppProfile, context.Request.CallCancelled);
                });
            });
        }
    }
}
