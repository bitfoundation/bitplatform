using Bit.Core.Models;
using Bit.Owin.Contracts;
using Microsoft.AspNetCore.Builder;

namespace Bit.Owin.Middlewares
{
    public class MetadataMiddlewareConfiguration : IAspNetCoreMiddlewareConfiguration
    {
        public virtual MiddlewarePosition MiddlewarePosition => MiddlewarePosition.BeforeOwinMiddlewares;

        public virtual AppEnvironment AppEnvironment { get; set; } = default!;

        public virtual void Configure(IApplicationBuilder aspNetCoreApp)
        {
            if (aspNetCoreApp == null)
                throw new ArgumentNullException(nameof(aspNetCoreApp));

            string path = $@"/Metadata/V{AppEnvironment.AppInfo.Version}";

            aspNetCoreApp.Map(path, innerApp =>
            {
                if (AppEnvironment.DebugMode == true)
                    innerApp.UseMiddleware<AspNetCoreNoCacheResponseMiddleware>();
                else
                    innerApp.UseMiddleware<AspNetCoreCacheResponseMiddleware>();
                innerApp.UseXContentTypeOptions();
                innerApp.UseXDownloadOptions();
                innerApp.UseMiddleware<MetadataMiddleware>();
            });
        }
    }
}
