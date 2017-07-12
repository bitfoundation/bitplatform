using Bit.Core.Contracts;
using Bit.Core.Models;
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

            foreach (IDependenciesManager projectDependenciesManager in DefaultDependenciesManagerProvider.Current.GetDependenciesManagers())
            {
                if (projectDependenciesManager is IAspNetCoreDependenciesManager)
                    ((IAspNetCoreDependenciesManager)projectDependenciesManager).ConfigureDependencies(_serviceProvider, services, DefaultDependencyManager.Current);

                else if (projectDependenciesManager is IOwinDependenciesManager)
                    ((IOwinDependenciesManager)projectDependenciesManager).ConfigureDependencies(DefaultDependencyManager.Current);
            }

            DefaultDependencyManager.Current.RegisterUsing(() =>
            {
                HttpContext context = DefaultDependencyManager.Current.Resolve<IHttpContextAccessor>().HttpContext;
                return (IOwinContext)context.Items["OwinContext"];
            }, lifeCycle: DependencyLifeCycle.InstancePerLifetimeScope);
        }

        public void Configure(IApplicationBuilder aspNetCoreApp, OwinAppStartup owinAppStartup, IEnumerable<IAspNetCoreMiddlewareConfiguration> aspNetCoreMiddlewares)
        {
            AppEnvironment activeAppEnvironment = DefaultAppEnvironmentProvider.Current.GetActiveAppEnvironment();

            if (string.IsNullOrEmpty(_hostingEnvironment.WebRootPath))
                _hostingEnvironment.WebRootPath = _pathProvider.GetCurrentStaticFilesPath();
            if (_hostingEnvironment.WebRootFileProvider == null || _hostingEnvironment.WebRootFileProvider is NullFileProvider)
                _hostingEnvironment.WebRootFileProvider = new PhysicalFileProvider(_hostingEnvironment.WebRootPath);

            string hostVirtualPath = activeAppEnvironment.GetHostVirtualPath();
            string hostVirtualPathAsAspNeteCorePrefix = hostVirtualPath.Substring(0, hostVirtualPath.Length - 1);

            if (!string.IsNullOrEmpty(hostVirtualPathAsAspNeteCorePrefix))
            {
                aspNetCoreApp.Map(hostVirtualPathAsAspNeteCorePrefix, bitAspNetCoreApp =>
                {
                    ConfigureBitAspNetCoreApp(bitAspNetCoreApp, owinAppStartup, aspNetCoreMiddlewares);
                });
            }
            else
            {
                ConfigureBitAspNetCoreApp(aspNetCoreApp, owinAppStartup, aspNetCoreMiddlewares);
            }
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
