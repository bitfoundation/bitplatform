using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Owin.Builder;
using Microsoft.Owin.BuilderProperties;
using Owin;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Foundation.AspNetCore.Extensions
{
    public static class OwinExtensions
    {
        public static IApplicationBuilder UseOwinApp(this IApplicationBuilder aspNetCoreApp, Action<IAppBuilder> owinAppConfiguration)
        {
            return aspNetCoreApp.UseOwin(setup => setup(aspNetCoreOwinPipeline =>
            {
                AppBuilder owinAppBuilder = new AppBuilder();

                IApplicationLifetime aspNetCoreAppLifetime = (IApplicationLifetime)aspNetCoreApp.ApplicationServices.GetService(typeof(IApplicationLifetime));

                AppProperties owinAppProps = new AppProperties(owinAppBuilder.Properties)
                {
                    OnAppDisposing = aspNetCoreAppLifetime?.ApplicationStopping ?? CancellationToken.None,
                    DefaultApp = aspNetCoreOwinPipeline
                };

                owinAppConfiguration(owinAppBuilder);

                return owinAppBuilder.Build<Func<IDictionary<string, object>, Task>>();
            }));
        }
    }
}
