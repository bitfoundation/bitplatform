using Bit.Core.Contracts;
using Bit.Core.Implementations;
using Bit.Owin;
using Bit.Owin.Contracts;
using Bit.Owin.Implementations;
using Bit.OwinCore.Contracts;
using Bit.OwinCore.Implementations;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Owin;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Bit.OwinCore
{
    public class AspNetCoreAppStartup
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IHostingEnvironment _hostingEnvironment;
        private readonly IPathProvider _pathProvider;

        public AspNetCoreAppStartup(IServiceProvider serviceProvider)
        {
            if (serviceProvider == null)
                throw new ArgumentNullException(nameof(serviceProvider));

            _serviceProvider = serviceProvider;
            _hostingEnvironment = _serviceProvider.GetService<IHostingEnvironment>();
            DefaultPathProvider.Current = _pathProvider = new AspNetCorePathProvider(_hostingEnvironment);
        }

        public virtual void InitServices(IServiceCollection services)
        {
            services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
            services.AddSingleton<OwinAppStartup, OwinAppStartup>();

            DefaultDependencyManager.Current.Init();

            foreach (IAppModule appModule in DefaultAppModulesProvider.Current.GetAppModules())
            {
                if (appModule is IAspNetCoreAppModule aspNetCoreAppModule)
                    aspNetCoreAppModule.ConfigureDependencies(_serviceProvider, services, DefaultDependencyManager.Current);

                else if (appModule is IOwinAppModule owinAppModule)
                    owinAppModule.ConfigureDependencies(DefaultDependencyManager.Current);
            }

            DefaultDependencyManager.Current.RegisterUsing((depManager) =>
            {
                HttpContext context = depManager.Resolve<IHttpContextAccessor>().HttpContext;
                return (IOwinContext)context.Items["OwinContext"];
            });
        }

        public void Configure(IApplicationBuilder aspNetCoreApp, OwinAppStartup owinAppStartup, IEnumerable<IAspNetCoreMiddlewareConfiguration> aspNetCoreMiddlewares)
        {
            if (string.IsNullOrEmpty(_hostingEnvironment.WebRootPath))
                _hostingEnvironment.WebRootPath = _pathProvider.GetCurrentStaticFilesPath();

            if (Directory.Exists(_hostingEnvironment.WebRootPath) && (_hostingEnvironment.WebRootFileProvider == null || _hostingEnvironment.WebRootFileProvider is NullFileProvider))
                _hostingEnvironment.WebRootFileProvider = new PhysicalFileProvider(_hostingEnvironment.WebRootPath);

            ConfigureBitAspNetCoreApp(aspNetCoreApp, owinAppStartup, aspNetCoreMiddlewares);
        }

        public virtual void ConfigureBitAspNetCoreApp(IApplicationBuilder aspNetCoreApp, OwinAppStartup owinAppStartup, IEnumerable<IAspNetCoreMiddlewareConfiguration> aspNetCoreMiddlewares)
        {
            aspNetCoreMiddlewares
                .ToList()
                .ForEach(aspNetCoreMiddleware => aspNetCoreMiddleware.Configure(aspNetCoreApp));

            aspNetCoreApp.UseOwinApp(owinAppStartup.Configuration);
        }
    }
}
