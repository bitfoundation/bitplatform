using System;
using Bit.Core.Models;
using Bit.Owin.Contracts;
using Owin;

namespace Bit.Owin.Middlewares
{
    public class SignInPageMiddlewareConfiguration : IOwinMiddlewareConfiguration
    {
        public virtual AppEnvironment AppEnvironment { get; set; }

        public virtual void Configure(IAppBuilder owinApp)
        {
            if (owinApp == null)
                throw new ArgumentNullException(nameof(owinApp));

            owinApp.Map("/SignIn",
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

                    innerApp.Use<SignInPageMiddleware>();
                });
        }
    }
}