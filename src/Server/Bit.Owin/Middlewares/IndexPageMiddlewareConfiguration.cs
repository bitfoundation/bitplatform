using System;
using Bit.Core.Models;
using Bit.Owin.Contracts;
using Owin;

namespace Bit.Owin.Middlewares
{
    public class IndexPageMiddlewareConfiguration : IOwinMiddlewareConfiguration
    {
        public virtual AppEnvironment AppEnvironment { get; set; } = default!;

        public virtual void Configure(IAppBuilder owinApp)
        {
            if (owinApp == null)
                throw new ArgumentNullException(nameof(owinApp));

            if (AppEnvironment.GetConfig(AppEnvironment.KeyValues.RequireSsl, defaultValueOnNotFound: false))
            {
                owinApp.UseHsts(config => config.IncludeSubdomains().MaxAge(days: 30));
            }

            owinApp.UseXContentTypeOptions();

            owinApp.UseXDownloadOptions();

            owinApp.UseXXssProtection(xssProtectionOptions => { xssProtectionOptions.EnabledWithBlockMode(); });

            owinApp.Use<OwinNoCacheResponseMiddleware>();

            owinApp.Use<IndexPageMiddleware>();
        }
    }
}