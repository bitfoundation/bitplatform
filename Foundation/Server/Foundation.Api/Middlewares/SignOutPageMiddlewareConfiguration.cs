using System;
using Foundation.Api.Contracts;
using Foundation.Core.Contracts;
using Foundation.Core.Models;
using Microsoft.Owin;
using NWebsec.Owin;
using Owin;

namespace Foundation.Api.Middlewares
{
    public class SignOutPageMiddlewareConfiguration : IOwinMiddlewareConfiguration
    {
        private readonly AppEnvironment _activeAppEnvironment;

        protected SignOutPageMiddlewareConfiguration()
        {
        }

        public SignOutPageMiddlewareConfiguration(IAppEnvironmentProvider appEnvironmentProvider)
        {
            if (appEnvironmentProvider == null)
                throw new ArgumentNullException(nameof(appEnvironmentProvider));

            _activeAppEnvironment = appEnvironmentProvider.GetActiveAppEnvironment();
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
                        innerApp.UseHsts(TimeSpan.FromDays(1));
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