using Bit.Core.Contracts;
using Bit.Core.Implementations;
using Bit.Owin.Implementations;
using Bit.Owin.Contracts;
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

namespace Bit.Owin
{
    public class AspNetCoreAppStartup
    {
        public IWebHostEnvironment WebHostEnvironment { get; } = default!;

        public IConfiguration Configuration { get; set; } = default!;

        private readonly IPathProvider _pathProvider = default!;

        public AspNetCoreAppStartup(IServiceProvider serviceProvider)
        {
            if (serviceProvider == null)
                throw new ArgumentNullException(nameof(serviceProvider));

            WebHostEnvironment = serviceProvider.GetRequiredService<IWebHostEnvironment>();

            Configuration = serviceProvider.GetRequiredService<IConfiguration>();

            AspNetCoreAppEnvironmentsProvider.Current.Configuration = Configuration;

            AspNetCoreAppEnvironmentsProvider.Current.WebHostEnvironment = WebHostEnvironment;

            DefaultPathProvider.Current = _pathProvider = new AspNetCorePathProvider(WebHostEnvironment);
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
                throw new InvalidOperationException($"Please inject {nameof(IHttpContextAccessor)} instead of {nameof(HttpContext)}. See https://docs.microsoft.com/en-us/aspnet/core/performance/performance-best-practices?view=aspnetcore-3.0#do-not-store-ihttpcontextaccessorhttpcontext-in-a-field");
            }

            DefaultDependencyManager.Current.RegisterUsing(RegisterHttpContext, overwriteExisting: false);

            IOwinContext RegisterOwinContext(IDependencyResolver resolver)
            {
                HttpContext context = resolver.Resolve<IHttpContextAccessor>().HttpContext;

                if (context == null)
                    throw new InvalidOperationException("http context is null");

                if (!(context.Items["OwinContext"] is IOwinContext owinContext))
                    throw new InvalidOperationException("OwinContextIsNull");

                return owinContext;
            }

            DefaultDependencyManager.Current.RegisterUsing(RegisterOwinContext, overwriteExisting: false);
        }

        public void Configure(IApplicationBuilder aspNetCoreApp, OwinAppStartup owinAppStartup, IEnumerable<IAspNetCoreMiddlewareConfiguration> aspNetCoreMiddlewares)
        {
            if (string.IsNullOrEmpty(WebHostEnvironment.WebRootPath))
                WebHostEnvironment.WebRootPath = _pathProvider.GetStaticFilesFolderPath();

            if (Directory.Exists(WebHostEnvironment.WebRootPath) && (WebHostEnvironment.WebRootFileProvider == null || WebHostEnvironment.WebRootFileProvider is NullFileProvider))
                WebHostEnvironment.WebRootFileProvider = new PhysicalFileProvider(WebHostEnvironment.WebRootPath);

            ConfigureBitAspNetCoreApp(aspNetCoreApp, owinAppStartup, aspNetCoreMiddlewares);
        }

        public virtual void ConfigureBitAspNetCoreApp(IApplicationBuilder aspNetCoreApp, OwinAppStartup owinAppStartup, IEnumerable<IAspNetCoreMiddlewareConfiguration> aspNetCoreMiddlewares)
        {
            if (owinAppStartup == null)
                throw new ArgumentNullException(nameof(owinAppStartup));

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
