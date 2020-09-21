using Bit.Core.Models;
using Bit.Owin.Contracts;
using Owin;
using System;

namespace Bit.Owin.Middlewares
{
    public class SignOutPageMiddlewareConfiguration : IOwinMiddlewareConfiguration
    {
        public virtual AppEnvironment AppEnvironment { get; set; } = default!;

        public virtual void Configure(IAppBuilder owinApp)
        {
            if (owinApp == null)
                throw new ArgumentNullException(nameof(owinApp));

            owinApp.Map("/SignOut",
                innerApp =>
                {
                    if (AppEnvironment.GetConfig(AppEnvironment.KeyValues.RequireSsl, defaultValueOnNotFound: false))
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