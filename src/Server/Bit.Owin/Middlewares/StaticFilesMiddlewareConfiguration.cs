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
        public virtual IAppEnvironmentProvider AppEnvironmentProvider { get; set; }
        public virtual IPathProvider PathProvider { get; set; }

        public virtual void Configure(IAppBuilder owinApp)
        {
            AppEnvironment appEnvironment = AppEnvironmentProvider.GetActiveAppEnvironment();

            string rootFolder = PathProvider.GetCurrentStaticFilesPath();

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

            owinApp.Map("/Files", innerApp =>
            {
                innerApp.Use<OwinNoCacheResponseMiddleware>();
                innerApp.UseXContentTypeOptions();
                innerApp.UseXDownloadOptions();
                innerApp.UseFileServer(options);
            });
        }
    }
}