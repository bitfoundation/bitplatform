using System;
using Autofac;
using Bit.Core.Contracts;
using Bit.ViewModel.Implementations;
using Microsoft.Extensions.DependencyInjection;
using Prism.Ioc;

namespace Bit.CSharpClientSample.Test
{
    public class TestPlatformInitializer : BitPlatformInitializer
    {
        private readonly Action<IDependencyManager, IContainerRegistry, ContainerBuilder, IServiceCollection> _conainerConfiguration;

        public TestPlatformInitializer(Action<IDependencyManager, IContainerRegistry, ContainerBuilder, IServiceCollection> conainerConfiguration = null)
        {
            _conainerConfiguration = conainerConfiguration;
        }

        public override void RegisterTypes(IDependencyManager dependencyManager, IContainerRegistry containerRegistry, ContainerBuilder containerBuilder, IServiceCollection services)
        {
            _conainerConfiguration?.Invoke(dependencyManager, containerRegistry, containerBuilder, services);

            base.RegisterTypes(dependencyManager, containerRegistry, containerBuilder, services);
        }
    }
}
