using Bit.Core.Contracts;
using Bit.Core.Models;
using Bit.OwinCore.Contracts;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using System;

namespace Bit.OwinCore.Middlewares
{
    public class AspNetCoreStaticFilesMiddlewareConfiguration : IAspNetCoreMiddlewareConfiguration
    {
        private readonly IAppEnvironmentProvider _appEnvironmentProvider;
        private readonly IHostingEnvironment _hostingEnvironment;

#if DEBUG
        protected AspNetCoreStaticFilesMiddlewareConfiguration()
        {
        }
#endif

        public AspNetCoreStaticFilesMiddlewareConfiguration(IAppEnvironmentProvider appEnvironmentProvider, IHostingEnvironment hostingEnvironment)
        {
            if (appEnvironmentProvider == null)
                throw new ArgumentNullException(nameof(appEnvironmentProvider));

            if (hostingEnvironment == null)
                throw new ArgumentNullException(nameof(hostingEnvironment));

            _appEnvironmentProvider = appEnvironmentProvider;
            _hostingEnvironment = hostingEnvironment;
        }

        public virtual void Configure(IApplicationBuilder aspNetCoreApp)
        {
            AppEnvironment appEnvironment = _appEnvironmentProvider.GetActiveAppEnvironment();

            FileServerOptions options = new FileServerOptions
            {
                EnableDirectoryBrowsing = appEnvironment.DebugMode,
                EnableDefaultFiles = false
            };

            options.DefaultFilesOptions.DefaultFileNames.Clear();

            options.FileProvider = _hostingEnvironment.WebRootFileProvider;

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