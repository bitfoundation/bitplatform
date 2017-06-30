using Autofac;
using Autofac.Extensions.DependencyInjection;
using Bit.Owin.Contracts;
using Bit.Owin.Implementations;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace Bit.OwinCore
{
    public class AutofacAspNetCoreAppStartup : AspNetCoreAppStartup
    {
        public AutofacAspNetCoreAppStartup(IServiceProvider serviceProvider)
            : base(serviceProvider)
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
