using Bit.Core.Models;
using Bit.OwinCore.Contracts;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;

namespace Bit.OwinCore.Middlewares
{
    public class AspNetCoreStaticFilesMiddlewareConfiguration : IAspNetCoreMiddlewareConfiguration
    {
        public virtual MiddlewarePosition MiddlewarePosition => MiddlewarePosition.BeforeOwinMiddlewares;

        public virtual AppEnvironment AppEnvironment { get; set; }
        public virtual IHostingEnvironment HostingEnvironment { get; set; }

        public virtual void Configure(IApplicationBuilder aspNetCoreApp)
        {
            FileServerOptions options = new FileServerOptions
            {
                EnableDirectoryBrowsing = AppEnvironment.DebugMode,
                EnableDefaultFiles = false
            };

            options.DefaultFilesOptions.DefaultFileNames.Clear();

            options.FileProvider = HostingEnvironment.WebRootFileProvider;

            string path = $@"/Files/V{AppEnvironment.AppInfo.Version}";

            aspNetCoreApp.Map(path, innerApp =>
            {
                if (AppEnvironment.DebugMode == true)
                    innerApp.UseMiddleware<AspNetCoreNoCacheResponseMiddleware>();
                else
                    innerApp.UseMiddleware<AspNetCoreCacheResponseMiddleware>();
                innerApp.UseXContentTypeOptions();
                innerApp.UseXDownloadOptions();
                innerApp.UseFileServer(options);
            });

            aspNetCoreApp.Map("/Files", innerApp =>
            {
                innerApp.UseMiddleware<AspNetCoreNoCacheResponseMiddleware>();
                innerApp.UseXContentTypeOptions();
                innerApp.UseXDownloadOptions();
                innerApp.UseFileServer(options);
            });
        }
    }
}