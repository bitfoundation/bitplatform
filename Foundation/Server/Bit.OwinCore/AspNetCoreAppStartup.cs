using System.Collections.Generic;
using System.Linq;
using Bit.Core.Contracts;
using Bit.Core.Contracts.Project;
using Bit.Owin;
using Bit.Owin.Implementations;
using Bit.OwinCore.Contracts;
using Bit.OwinCore.Implementations;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Owin;

namespace Bit.OwinCore
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

            DefaultDependencyManager.Current.RegisterUsing(() =>
            {
                HttpContext context = DefaultDependencyManager.Current.Resolve<IHttpContextAccessor>().HttpContext;
                return (IOwinContext)context.Items["OwinContext"];
            }, lifeCycle: DependencyLifeCycle.InstancePerLifetimeScope);
        }

        public virtual void Configure(IApplicationBuilder aspNetCoreApp, OwinAppStartup owinAppStartup, IEnumerable<IAspNetCoreMiddlewareConfiguration> aspNetCoreMiddlewares)
        {
            aspNetCoreMiddlewares
                .Where(m => m.GetRegisterKind() == RegisterKind.BeforeOwinPiepline)
                .ToList()
                .ForEach(aspNetCoreMiddleware => aspNetCoreMiddleware.Configure(aspNetCoreApp));

            aspNetCoreApp.UseOwinApp(owinAppStartup.Configuration);

            aspNetCoreMiddlewares
                .Where(m => m.GetRegisterKind() == RegisterKind.AfterOwinPipeline)
                .ToList()
                .ForEach(aspNetCoreMiddleware => aspNetCoreMiddleware.Configure(aspNetCoreApp));
        }
    }
}
