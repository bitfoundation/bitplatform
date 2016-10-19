using Foundation.Api;
using Foundation.Api.Implementations;
using Foundation.AspNetCore.Contracts;
using Foundation.AspNetCore.Extensions;
using Foundation.AspNetCore.Implementations;
using Foundation.Core.Contracts;
using Foundation.Core.Contracts.Project;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Owin;
using System.Collections.Generic;
using System.Linq;

namespace Foundation.AspNetCore
{
    public class AspNetCoreAppStartup
    {
        private readonly IHostingEnvironment _hostingEnvironment;

        public AspNetCoreAppStartup(IHostingEnvironment hostingEnvironment)
        {
            _hostingEnvironment = hostingEnvironment;
        }

        public virtual void InitServices(IServiceCollection services)
        {
            services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
            services.AddSingleton<OwinAppStartup, OwinAppStartup>();

            DefaultPathProvider.Current = new AspNetCorePathProvider(_hostingEnvironment);

            DefaultDependencyManager.Current.Init();

            foreach (IDependenciesManager projectDependenciesManager in DefaultDependenciesManagerProvider.Current.GetDependenciesManagers())
            {
                if (projectDependenciesManager is IAspNetCoreDependenciesManager)
                    ((IAspNetCoreDependenciesManager)projectDependenciesManager).ConfigureServices(services, DefaultDependencyManager.Current);

                projectDependenciesManager.ConfigureDependencies(DefaultDependencyManager.Current);
            }

            DefaultDependencyManager.Current.RegisterUsing(dr =>
            {
                HttpContext context = dr.Resolve<IHttpContextAccessor>().HttpContext;
                return (IOwinContext)context.Items["OwinContext"];
            }, lifeCycle: DepepdencyLifeCycle.InstancePerLifetimeScope);
        }

        public virtual void Configure(IApplicationBuilder aspNetCoreApp, OwinAppStartup owinAppStartup, IEnumerable<IAspNetCoreMiddlewareConfiguration> aspNetCoreMiddlewares)
        {
            aspNetCoreMiddlewares
                .Where(m => m.GetRegisterKind() == RegisterKind.BeforeOwinPiepline)
                .ToList()
                .ForEach(aspNetCoreMiddleware => aspNetCoreMiddleware.Configure(aspNetCoreApp));

            aspNetCoreApp.UseOwinApp(owinApp =>
            {
                owinAppStartup.Configuration(owinApp);
            });

            aspNetCoreMiddlewares
                .Where(m => m.GetRegisterKind() == RegisterKind.AfterOwinPipeline)
                .ToList()
                .ForEach(aspNetCoreMiddleware => aspNetCoreMiddleware.Configure(aspNetCoreApp));
        }
    }
}
