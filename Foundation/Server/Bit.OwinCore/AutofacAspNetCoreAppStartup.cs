using Autofac;
using Autofac.Extensions.DependencyInjection;
using Foundation.Api.Contracts;
using Foundation.Api.Implementations;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace Foundation.AspNetCore
{
    public class AutofacAspNetCoreAppStartup : AspNetCoreAppStartup
    {
        public AutofacAspNetCoreAppStartup(IHostingEnvironment hostingEnvironment)
            : base(hostingEnvironment)
        {

        }

        public virtual IServiceProvider ConfigureServices(IServiceCollection services)
        {
            InitServices(services);

            ContainerBuilder builder = ((IAutofacDependencyManager)DefaultDependencyManager.Current).GetContainerBuidler();

            builder.Populate(services);

            DefaultDependencyManager.Current.BuildContainer();

            ILifetimeScope container = ((IAutofacDependencyManager)DefaultDependencyManager.Current).GetContainer();

            return new AutofacServiceProvider(container);
        }
    }
}
