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
        private readonly IAppEnvironmentProvider _appEnvironmentProvider;

        protected MetadataMiddlewareConfiguration()
        {
        }

        public MetadataMiddlewareConfiguration(IAppEnvironmentProvider appEnvironmentProvider)
        {
            if (appEnvironmentProvider == null)
                throw new ArgumentNullException(nameof(appEnvironmentProvider));

            _appEnvironmentProvider = appEnvironmentProvider;
        }

        public virtual void Configure(IAppBuilder owinApp)
        {
            AppEnvironment appEnvironment = _appEnvironmentProvider.GetActiveAppEnvironment();

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
