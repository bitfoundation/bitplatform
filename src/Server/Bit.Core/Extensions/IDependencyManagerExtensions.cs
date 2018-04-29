using System;

namespace Bit.Core.Contracts
{
    public static class IDependencyManagerExtensions
    {
        public static IDependencyManager RegisterAppEvents<TAppEvents>(this IDependencyManager dependencyManager)
            where TAppEvents : class, IAppEvents
        {
            if (dependencyManager == null)
                throw new ArgumentNullException(nameof(dependencyManager));

            dependencyManager.Register<IAppEvents, TAppEvents>(lifeCycle: DependencyLifeCycle.SingleInstance, overwriteExciting: false);

            return dependencyManager;
        }
    }
}
