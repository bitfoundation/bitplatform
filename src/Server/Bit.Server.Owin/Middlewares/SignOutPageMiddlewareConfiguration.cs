using System;
using Bit.Core.Models;
using Bit.Owin.Contracts;
using Microsoft.AspNetCore.Builder;

namespace Bit.Owin.Middlewares
{
    public class SignOutPageMiddlewareConfiguration : IAspNetCoreMiddlewareConfiguration
    {
        public virtual AppEnvironment AppEnvironment { get; set; } = default!;

        public virtual MiddlewarePosition MiddlewarePosition => MiddlewarePosition.BeforeOwinMiddlewares;

        public virtual void Configure(IApplicationBuilder aspNetCoreApp)
        {
            if (aspNetCoreApp == null)
                throw new ArgumentNullException(nameof(aspNetCoreApp));

            aspNetCoreApp.Map("/SignOut",
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

                    innerApp.UseMiddleware<AspNetCoreNoCacheResponseMiddleware>();

                    innerApp.UseMiddleware<SignOutPageMiddleware>();
                });
        }
    }
}
