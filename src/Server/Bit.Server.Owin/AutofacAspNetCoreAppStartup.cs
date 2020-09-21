using Autofac;
using Autofac.Extensions.DependencyInjection;
using Bit.Core.Contracts;
using Bit.Owin.Implementations;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace Bit.Owin
{
    /// <summary>
    /// Startup class for your asp.net core based apps which configures autofac by default
    /// </summary>
    public class AutofacAspNetCoreAppStartup : AspNetCoreAppStartup
    {
        public AutofacAspNetCoreAppStartup(IServiceProvider serviceProvider)
            : base(serviceProvider)
        {

        }

        /// <summary>
        /// ASP.NET Core's default ConfigureServices. We recommend you to use <see cref="IAppModule.ConfigureDependencies(IServiceCollection, Core.Contracts.IDependencyManager)"/> method which provides you <see cref="IServiceProvider"/>, <see cref="Core.Contracts.IDependencyManager"/> and <see cref="IServiceCollection"/> altogether
        /// </summary>
        public virtual IServiceProvider ConfigureServices(IServiceCollection services)
        {
            if (services == null)
                throw new ArgumentNullException(nameof(services));

            InitServices(services);

            DefaultDependencyManager.Current.Populate(services);

            DefaultDependencyManager.Current.BuildContainer();

            ILifetimeScope container = ((IAutofacDependencyManager)DefaultDependencyManager.Current).GetContainer();

            return new AutofacServiceProvider(container);
        }
    }
}
