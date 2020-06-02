using Autofac;
using Autofac.Extensions.DependencyInjection;
using Bit.Core.Contracts;
using Bit.Core.Implementations;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace Bit.Client.Web.Blazor.Implementation
{
    public class DefaultServiceProviderFactory : IServiceProviderFactory<ContainerBuilder>
    {
        private readonly Action<IDependencyManager, IServiceCollection, ContainerBuilder> _configureAction;
        private readonly AutofacServiceProviderFactory _inner;

        private IServiceCollection? _services;
        private IContainer? _container;

        public DefaultServiceProviderFactory(Action<IDependencyManager, IServiceCollection, ContainerBuilder> configureAction)
        {
            _configureAction = configureAction;
            _inner = new AutofacServiceProviderFactory(containerBuilder =>
            {
                if (_services == null)
                    throw new InvalidOperationException("services is null");

                if (_container == null)
                    throw new InvalidOperationException("container is null");

                containerBuilder.Properties["services"] = _services;

                AutofacDependencyManager dependencyManager = new AutofacDependencyManager();
                dependencyManager.UseContainerBuilder(containerBuilder);
                ((IServiceCollectionAccessor)dependencyManager).ServiceCollection = _services;

                containerBuilder.Properties["dependencyManager"] = dependencyManager;

                containerBuilder.Register(c => _container).SingleInstance();

                _configureAction?.Invoke(dependencyManager, _services, containerBuilder);
            });
        }

        public virtual ContainerBuilder CreateBuilder(IServiceCollection services)
        {
            _services = services;
            ContainerBuilder containerBuilder = _inner.CreateBuilder(services);
            return containerBuilder;
        }

        public virtual IServiceProvider CreateServiceProvider(ContainerBuilder containerBuilder)
        {
            if (containerBuilder == null)
                throw new ArgumentNullException(nameof(containerBuilder));

            containerBuilder.Populate(_services);

            _container = containerBuilder.Build();

            return new AutofacServiceProvider(_container);
        }
    }
}
