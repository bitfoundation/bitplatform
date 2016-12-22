using System;
using Foundation.Api.Contracts;
using Foundation.Core.Contracts;
using Foundation.Core.Models;
using NWebsec.Owin;
using Owin;

namespace Foundation.Api.Middlewares
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
                innerApp.Use<MetadatMiddlware>();
            });
        }
    }
}
