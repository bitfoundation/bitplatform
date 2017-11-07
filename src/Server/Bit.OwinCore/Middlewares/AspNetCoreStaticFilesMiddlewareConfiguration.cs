using Bit.Core.Contracts;
using Bit.Core.Models;
using Bit.OwinCore.Contracts;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;

namespace Bit.OwinCore.Middlewares
{
    public class AspNetCoreStaticFilesMiddlewareConfiguration : IAspNetCoreMiddlewareConfiguration
    {
        public virtual IAppEnvironmentProvider AppEnvironmentProvider { get; set; }
        public virtual IHostingEnvironment HostingEnvironment { get; set; }

        public virtual void Configure(IApplicationBuilder aspNetCoreApp)
        {
            AppEnvironment appEnvironment = AppEnvironmentProvider.GetActiveAppEnvironment();

            FileServerOptions options = new FileServerOptions
            {
                EnableDirectoryBrowsing = appEnvironment.DebugMode,
                EnableDefaultFiles = false
            };

            options.DefaultFilesOptions.DefaultFileNames.Clear();

            options.FileProvider = HostingEnvironment.WebRootFileProvider;

            string path = $@"/Files/V{appEnvironment.AppInfo.Version}";

            aspNetCoreApp.Map(path, innerApp =>
            {
                if (appEnvironment.DebugMode == true)
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