using Microsoft.Extensions.Hosting;
using Microsoft.Owin.Builder;
using Microsoft.Owin.BuilderProperties;
using Owin;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Builder
{
    public static class OwinExtensions
    {
        public static IApplicationBuilder UseOwinApp(this IApplicationBuilder aspNetCoreApp, Action<IAppBuilder> owinAppConfiguration)
        {
            if (aspNetCoreApp == null)
                throw new ArgumentNullException(nameof(aspNetCoreApp));

            return aspNetCoreApp.UseOwin(setup => setup(aspNetCoreOwinPipeline =>
            {
                AppBuilder owinAppBuilder = new AppBuilder();

                IHostApplicationLifetime hostApplicationLifetime = (IHostApplicationLifetime)aspNetCoreApp.ApplicationServices.GetService(typeof(IHostApplicationLifetime));

                AppProperties owinAppProps = new AppProperties(owinAppBuilder.Properties)
                {
                    OnAppDisposing = hostApplicationLifetime?.ApplicationStopping ?? CancellationToken.None,
                    DefaultApp = aspNetCoreOwinPipeline
                };

                owinAppConfiguration(owinAppBuilder);

                return owinAppBuilder.Build<Func<IDictionary<string, object>, Task>>();
            }));
        }
    }
}
