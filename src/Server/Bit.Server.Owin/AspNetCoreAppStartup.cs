using Bit.Core.Contracts;
using Bit.Owin.Contracts;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Bit.Owin
{
    public class AspNetCoreAppStartup
    {
        public virtual void ConfigureServices(IServiceCollection services)
        {

        }

        public virtual void ConfigureMiddlewares(IApplicationBuilder aspNetCoreApp)
        {

        }

        public void Configure(IApplicationBuilder aspNetCoreApp, OwinAppStartup owinAppStartup, IEnumerable<IAspNetCoreMiddlewareConfiguration> aspNetCoreMiddlewares, IServiceProvider serviceProvider, IPathProvider pathProvider)
        {
            IWebHostEnvironment webHostEnvironment = serviceProvider.GetService<IWebHostEnvironment>();

            if (webHostEnvironment != null)
            {
                if (string.IsNullOrEmpty(webHostEnvironment.WebRootPath))
                    webHostEnvironment.WebRootPath = pathProvider.GetStaticFilesFolderPath();

                if (Directory.Exists(webHostEnvironment.WebRootPath) && (webHostEnvironment.WebRootFileProvider == null || webHostEnvironment.WebRootFileProvider is NullFileProvider))
                    webHostEnvironment.WebRootFileProvider = new PhysicalFileProvider(webHostEnvironment.WebRootPath);
            }

            ConfigureBitAspNetCoreApp(aspNetCoreApp, owinAppStartup, aspNetCoreMiddlewares);
        }

        public void ConfigureBitAspNetCoreApp(IApplicationBuilder aspNetCoreApp, OwinAppStartup owinAppStartup, IEnumerable<IAspNetCoreMiddlewareConfiguration> aspNetCoreMiddlewares)
        {
            if (owinAppStartup == null)
                throw new ArgumentNullException(nameof(owinAppStartup));

            aspNetCoreMiddlewares
                .Where(m => m.MiddlewarePosition == MiddlewarePosition.BeforeOwinMiddlewares)
                .ToList()
                .ForEach(aspNetCoreMiddleware => aspNetCoreMiddleware.Configure(aspNetCoreApp));

            ConfigureMiddlewares(aspNetCoreApp);

            aspNetCoreApp.UseOwinApp(owinAppStartup.Configuration);

            aspNetCoreMiddlewares
                .Where(m => m.MiddlewarePosition == MiddlewarePosition.AfterOwinMiddlewares)
                .ToList()
                .ForEach(aspNetCoreMiddleware => aspNetCoreMiddleware.Configure(aspNetCoreApp));
        }
    }
}
