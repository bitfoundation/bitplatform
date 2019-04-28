using Bit.Core.Contracts;
using Bit.Core.Implementations;
using Bit.Owin;
using Bit.Owin.Implementations;
using Bit.OwinCore.Contracts;
using Bit.OwinCore.Implementations;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
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
        public IHostingEnvironment HostingEnvironment { get; }

        private readonly IPathProvider _pathProvider;

        public AspNetCoreAppStartup(IServiceProvider serviceProvider)
        {
            if (serviceProvider == null)
                throw new ArgumentNullException(nameof(serviceProvider));

            HostingEnvironment = serviceProvider.GetRequiredService<IHostingEnvironment>();

            DefaultPathProvider.Current = _pathProvider = new AspNetCorePathProvider(HostingEnvironment);
        }

        public virtual void InitServices(IServiceCollection services)
        {
            services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
            services.AddSingleton<OwinAppStartup, OwinAppStartup>();

            DefaultDependencyManager.Current.Init();

            if (DefaultDependencyManager.Current is IServiceCollectionAccessor dependencyManagerIServiceCollectionInterop)
                dependencyManagerIServiceCollectionInterop.ServiceCollection = services;

            foreach (IAppModule appModule in DefaultAppModulesProvider.Current.GetAppModules())
            {
                appModule.ConfigureDependencies(services, DefaultDependencyManager.Current);
            }

            HttpContext RegisterHttpContext(IDependencyResolver resolver)
            {
                HttpContext httpContext = resolver.Resolve<IHttpContextAccessor>().HttpContext;

                if (httpContext == null)
                    throw new InvalidOperationException("HttpContextIsNull");

                return httpContext;
            }

            DefaultDependencyManager.Current.RegisterUsing(RegisterHttpContext, overwriteExciting: false);

            IOwinContext RegisterOwinContext(IDependencyResolver resolver)
            {
                HttpContext context = resolver.Resolve<HttpContext>();

                if (context == null)
                    throw new InvalidOperationException("http context is null");

                if (!(context.Items["OwinContext"] is IOwinContext owinContext))
                    throw new InvalidOperationException("OwinContextIsNull");

                return owinContext;
            }

            DefaultDependencyManager.Current.RegisterUsing(RegisterOwinContext, overwriteExciting: false);
        }

        public void Configure(IApplicationBuilder aspNetCoreApp, OwinAppStartup owinAppStartup, IEnumerable<IAspNetCoreMiddlewareConfiguration> aspNetCoreMiddlewares)
        {
            if (string.IsNullOrEmpty(HostingEnvironment.WebRootPath))
                HostingEnvironment.WebRootPath = _pathProvider.GetStaticFilesFolderPath();

            if (Directory.Exists(HostingEnvironment.WebRootPath) && (HostingEnvironment.WebRootFileProvider == null || HostingEnvironment.WebRootFileProvider is NullFileProvider))
                HostingEnvironment.WebRootFileProvider = new PhysicalFileProvider(HostingEnvironment.WebRootPath);

            ConfigureBitAspNetCoreApp(aspNetCoreApp, owinAppStartup, aspNetCoreMiddlewares);
        }

        public virtual void ConfigureBitAspNetCoreApp(IApplicationBuilder aspNetCoreApp, OwinAppStartup owinAppStartup, IEnumerable<IAspNetCoreMiddlewareConfiguration> aspNetCoreMiddlewares)
        {
            aspNetCoreMiddlewares
                .Where(m => m.MiddlewarePosition == MiddlewarePosition.BeforeOwinMiddlewares)
                .ToList()
                .ForEach(aspNetCoreMiddleware => aspNetCoreMiddleware.Configure(aspNetCoreApp));

            aspNetCoreApp.UseOwinApp(owinAppStartup.Configuration);

            aspNetCoreMiddlewares
                .Where(m => m.MiddlewarePosition == MiddlewarePosition.AfterOwinMiddlewares)
                .ToList()
                .ForEach(aspNetCoreMiddleware => aspNetCoreMiddleware.Configure(aspNetCoreApp));
        }
    }
}
