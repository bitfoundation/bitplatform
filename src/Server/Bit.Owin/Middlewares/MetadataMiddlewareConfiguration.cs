using System;
using Bit.Core.Contracts;
using Bit.Core.Models;
using Bit.Owin.Contracts;
using NWebsec.Owin;
using Owin;

namespace Bit.Owin.Middlewares
{
    public class MetadataMiddlewareConfiguration : IOwinMiddlewareConfiguration
    {
        public virtual IAppEnvironmentProvider AppEnvironmentProvider { get; set; }

        public virtual void Configure(IAppBuilder owinApp)
        {
            AppEnvironment appEnvironment = AppEnvironmentProvider.GetActiveAppEnvironment();

            string path = $@"/Metadata/V{appEnvironment.AppInfo.Version}";

            owinApp.Map(path, innerApp =>
            {
                if (appEnvironment.DebugMode == true)
                    innerApp.Use<OwinNoCacheResponseMiddleware>();
                else
                    innerApp.Use<OwinCacheResponseMiddleware>();
                innerApp.UseXContentTypeOptions();
                innerApp.UseXDownloadOptions();
                innerApp.Use<MetadatMiddleware>();
            });
        }
    }
}
