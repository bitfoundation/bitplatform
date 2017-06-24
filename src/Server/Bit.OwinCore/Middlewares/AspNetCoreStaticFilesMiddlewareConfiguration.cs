using Bit.Core.Contracts;
using Bit.Core.Models;
using Bit.OwinCore.Contracts;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.FileProviders;
using Owin;
using System;

namespace Bit.OwinCore.Middlewares
{
    public class AspNetCoreStaticFilesMiddlewareConfiguration : IAspNetCoreMiddlewareConfiguration
    {
        private readonly IAppEnvironmentProvider _appEnvironmentProvider;
        private readonly IPathProvider _pathProvider;

        protected AspNetCoreStaticFilesMiddlewareConfiguration()
        {
        }

        public AspNetCoreStaticFilesMiddlewareConfiguration(IAppEnvironmentProvider appEnvironmentProvider,
            IPathProvider pathProvider)
        {
            if (appEnvironmentProvider == null)
                throw new ArgumentNullException(nameof(appEnvironmentProvider));

            if (pathProvider == null)
                throw new ArgumentNullException(nameof(pathProvider));

            _appEnvironmentProvider = appEnvironmentProvider;
            _pathProvider = pathProvider;
        }

        public virtual void Configure(IApplicationBuilder aspNetCoreApp)
        {
            AppEnvironment appEnvironment = _appEnvironmentProvider.GetActiveAppEnvironment();

            string rootFolder = _pathProvider.GetCurrentStaticFilesPath();

            PhysicalFileProvider fileSystem = new PhysicalFileProvider(rootFolder);

            FileServerOptions options = new FileServerOptions
            {
                EnableDirectoryBrowsing = appEnvironment.DebugMode,
                EnableDefaultFiles = false
            };

            options.DefaultFilesOptions.DefaultFileNames.Clear();

            options.FileProvider = fileSystem;

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