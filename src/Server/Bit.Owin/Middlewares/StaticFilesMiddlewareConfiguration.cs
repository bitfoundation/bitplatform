using System;
using Bit.Core.Contracts;
using Bit.Core.Models;
using Bit.Owin.Contracts;
using Microsoft.Owin.FileSystems;
using Microsoft.Owin.StaticFiles;
using NWebsec.Owin;
using Owin;

namespace Bit.Owin.Middlewares
{
    public class StaticFilesMiddlewareConfiguration : IOwinMiddlewareConfiguration
    {
        private readonly IAppEnvironmentProvider _appEnvironmentProvider;
        private readonly IPathProvider _pathProvider;

        protected StaticFilesMiddlewareConfiguration()
        {
        }

        public StaticFilesMiddlewareConfiguration(IAppEnvironmentProvider appEnvironmentProvider,
            IPathProvider pathProvider)
        {
            if (appEnvironmentProvider == null)
                throw new ArgumentNullException(nameof(appEnvironmentProvider));

            if (pathProvider == null)
                throw new ArgumentNullException(nameof(pathProvider));

            _appEnvironmentProvider = appEnvironmentProvider;
            _pathProvider = pathProvider;
        }

        public virtual void Configure(IAppBuilder owinApp)
        {
            AppEnvironment appEnvironment = _appEnvironmentProvider.GetActiveAppEnvironment();

            string rootFolder = _pathProvider.GetCurrentStaticFilesPath();

            PhysicalFileSystem fileSystem = new PhysicalFileSystem(rootFolder);

            FileServerOptions options = new FileServerOptions
            {
                EnableDirectoryBrowsing = appEnvironment.DebugMode,
                EnableDefaultFiles = false
            };

            options.DefaultFilesOptions.DefaultFileNames.Clear();

            options.FileSystem = fileSystem;

            string path = $@"/Files/V{appEnvironment.AppInfo.Version}";

            owinApp.Map(path, innerApp =>
            {
                if (appEnvironment.DebugMode == true)
                    innerApp.Use<OwinNoCacheResponseMiddleware>();
                else
                    innerApp.Use<OwinCacheResponseMiddleware>();
                innerApp.UseXContentTypeOptions();
                innerApp.UseXDownloadOptions();
                innerApp.UseFileServer(options);
            });
        }
    }
}