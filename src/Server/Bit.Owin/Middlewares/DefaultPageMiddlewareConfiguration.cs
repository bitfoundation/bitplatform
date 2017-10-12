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
        private AppEnvironment _activeAppEnvironment;

        public virtual IAppEnvironmentProvider AppEnvironmentProvider
        {
            set
            {
                if (value == null)
                    throw new ArgumentNullException(nameof(value));

                _activeAppEnvironment = value.GetActiveAppEnvironment();
            }
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