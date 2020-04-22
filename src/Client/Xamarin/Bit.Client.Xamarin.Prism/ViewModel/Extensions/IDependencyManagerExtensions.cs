using Prism.Ioc;
using System;

namespace Bit.Core.Contracts
{
    public static class IDependencyManagerExtensions
    {
        public static IContainerRegistry GetContainerRegistry(this IDependencyManager dependencyManager)
        {
            if (dependencyManager == null)
                throw new ArgumentNullException(nameof(dependencyManager));

            return (IContainerRegistry)dependencyManager.GetContainerBuilder().Properties["containerRegistry"]!;
        }
    }
}
