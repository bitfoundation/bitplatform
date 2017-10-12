using System;
using Bit.Core.Contracts;
using Bit.Core.Models;
using Bit.Owin.Contracts;
using NWebsec.Owin;
using Owin;

namespace Bit.Owin.Middlewares
{
    public class SignOutPageMiddlewareConfiguration : IOwinMiddlewareConfiguration
    {
        private AppEnvironment _activeAppEnvironment;

        public virtual IAppEnvironmentProvider AppEnvironmentProvider
        {
            set
            {
                if (value == null)
                    throw new ArgumentNullException(nameof(AppEnvironmentProvider));

                _activeAppEnvironment = value.GetActiveAppEnvironment();
            }
        }

        public virtual void Configure(IAppBuilder owinApp)
        {
            if (owinApp == null)
                throw new ArgumentNullException(nameof(owinApp));

            owinApp.Map("/SignOut",
                innerApp =>
                {
                    if (_activeAppEnvironment.GetConfig("RequireSsl", defaultValueOnNotFound: false))
                    {
                        innerApp.UseHsts(config => config.IncludeSubdomains().MaxAge(days: 30));
                    }

                    innerApp.UseXfo(xFrameOptions => { xFrameOptions.SameOrigin(); });

                    innerApp.UseXContentTypeOptions();

                    innerApp.UseXDownloadOptions();

                    innerApp.UseXXssProtection(xssProtectionOptions => { xssProtectionOptions.EnabledWithBlockMode(); });

                    innerApp.Use<OwinNoCacheResponseMiddleware>();

                    innerApp.Use<SignOutPageMiddleware>();
                });
        }
    }
}