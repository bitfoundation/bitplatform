using System;
using Foundation.Api.Contracts;
using Foundation.Core.Contracts;
using Foundation.Core.Models;
using NWebsec.Owin;
using Owin;

namespace Foundation.Api.Middlewares
{
    public class DefaultPageMiddlewareConfiguration : IOwinMiddlewareConfiguration
    {
        private readonly AppEnvironment _activeAppEnvironment;

        protected DefaultPageMiddlewareConfiguration()
        {
        }

        public DefaultPageMiddlewareConfiguration(IAppEnvironmentProvider appEnvironmentProvider)
        {
            if (appEnvironmentProvider == null)
                throw new ArgumentNullException(nameof(appEnvironmentProvider));

            _activeAppEnvironment = appEnvironmentProvider.GetActiveAppEnvironment();
        }

        public virtual void Configure(IAppBuilder owinApp)
        {
            if (owinApp == null)
                throw new ArgumentNullException(nameof(owinApp));

            if (_activeAppEnvironment.GetConfig("RequireSsl", defaultValueOnNotFound: false))
            {
                owinApp.UseHsts(TimeSpan.FromDays(1));
            }

            owinApp.UseXContentTypeOptions();

            owinApp.UseXDownloadOptions();

            owinApp.UseXXssProtection(xssProtectionOptions => { xssProtectionOptions.EnabledWithBlockMode(); });

            owinApp.Use<OwinNoCacheResponseMiddleware>();

            owinApp.Use<DefaultPageMiddleware>();
        }
    }
}