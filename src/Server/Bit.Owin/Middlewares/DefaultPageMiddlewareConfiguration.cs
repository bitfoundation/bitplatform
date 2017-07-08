using System;
using Bit.Core.Contracts;
using Bit.Core.Models;
using Bit.Owin.Contracts;
using NWebsec.Owin;
using Owin;

namespace Bit.Owin.Middlewares
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
                owinApp.UseHsts(config => config.IncludeSubdomains().MaxAge(days: 30));
            }

            owinApp.UseXContentTypeOptions();

            owinApp.UseXDownloadOptions();

            owinApp.UseXXssProtection(xssProtectionOptions => { xssProtectionOptions.EnabledWithBlockMode(); });

            owinApp.Use<OwinNoCacheResponseMiddleware>();

            owinApp.Use<DefaultPageMiddleware>();
        }
    }
}