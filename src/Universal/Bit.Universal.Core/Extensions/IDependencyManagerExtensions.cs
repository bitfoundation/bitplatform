using Autofac;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace Bit.Core.Contracts
{
    public static class IDependencyManagerExtensions
    {
        public static IServiceCollection GetServiceCollection(this IDependencyManager dependencyManager)
        {
            if (dependencyManager == null)
                throw new ArgumentNullException(nameof(dependencyManager));

            return ((IServiceCollectionAccessor)dependencyManager).ServiceCollection;
        }

        public static ContainerBuilder GetContainerBuilder(this IDependencyManager dependencyManager)
        {
            if (dependencyManager == null)
                throw new ArgumentNullException(nameof(dependencyManager));

            return ((IAutofacDependencyManager)dependencyManager).GetContainerBuidler();
        }
    }
}
