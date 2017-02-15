using Bit.Data.EntityFramework.Implementations;
using Foundation.DataAccess.Contracts;
using System;

namespace Foundation.Core.Contracts
{
    public static class IDependencyManangerExtensions
    {
        public static IDependencyManager RegisterEfDbContext<TDbContext>(this IDependencyManager dependencyManager)
            where TDbContext : class
        {
            if (dependencyManager == null)
                throw new ArgumentNullException(nameof(dependencyManager));

            dependencyManager.Register<TDbContext, TDbContext>();
            dependencyManager.Register<IDataProviderSpecificMethodsProvider, EfDataProviderSpecificMethodsProvider>(overwriteExciting: false, lifeCycle: DependencyLifeCycle.SingleInstance);

            return dependencyManager;
        }
    }
}
