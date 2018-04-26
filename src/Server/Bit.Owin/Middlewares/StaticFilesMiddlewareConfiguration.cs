using Bit.Core.Contracts;
using Bit.Core.Models;
using Bit.Owin.Contracts;
using Microsoft.Owin.FileSystems;
using Microsoft.Owin.StaticFiles;
using Owin;

namespace Bit.Owin.Middlewares
{
    public class StaticFilesMiddlewareConfiguration : IOwinMiddlewareConfiguration
    {
        public virtual AppEnvironment AppEnvironment { get; set; }
        public virtual IPathProvider PathProvider { get; set; }

        public virtual void Configure(IAppBuilder owinApp)
        {
            string rootFolder = PathProvider.GetStaticFilesFolderPath();

            PhysicalFileSystem fileSystem = new PhysicalFileSystem(rootFolder);

            FileServerOptions options = new FileServerOptions
            {
                EnableDirectoryBrowsing = AppEnvironment.DebugMode,
                EnableDefaultFiles = false
            };

            options.DefaultFilesOptions.DefaultFileNames.Clear();

            options.FileSystem = fileSystem;

            string path = $@"/Files/V{AppEnvironment.AppInfo.Version}";

            owinApp.Map(path, innerApp =>
            {
                if (AppEnvironment.DebugMode == true)
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