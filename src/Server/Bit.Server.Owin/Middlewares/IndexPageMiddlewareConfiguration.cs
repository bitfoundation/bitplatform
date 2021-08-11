using Bit.Core.Models;
using Bit.Owin.Contracts;
using Microsoft.AspNetCore.Builder;
using Owin;
using System;

namespace Bit.Owin.Middlewares
{
    public class IndexPageMiddlewareConfiguration : IAspNetCoreMiddlewareConfiguration
    {
        public virtual MiddlewarePosition MiddlewarePosition => MiddlewarePosition.AfterOwinMiddlewares;

        public virtual AppEnvironment AppEnvironment { get; set; } = default!;

        public virtual void Configure(IApplicationBuilder aspNetCoreApp)
        {
            if (aspNetCoreApp == null)
                throw new ArgumentNullException(nameof(aspNetCoreApp));

            if (AppEnvironment.GetConfig(AppEnvironment.KeyValues.RequireSsl, defaultValueOnNotFound: false))
            {
                aspNetCoreApp.UseHsts(config => config.IncludeSubdomains().MaxAge(days: 30));
            }

            aspNetCoreApp.UseXContentTypeOptions();

            aspNetCoreApp.UseXDownloadOptions();

            aspNetCoreApp.UseXXssProtection(xssProtectionOptions => { xssProtectionOptions.EnabledWithBlockMode(); });

            aspNetCoreApp.UseMiddleware<AspNetCoreNoCacheResponseMiddleware>();

            aspNetCoreApp.UseMiddleware<IndexPageMiddleware>();
        }
    }
}
