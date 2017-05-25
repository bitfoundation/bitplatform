using System;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using Bit.Owin.Contracts;
using Bit.Owin.Implementations;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace Bit.OwinCore
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
