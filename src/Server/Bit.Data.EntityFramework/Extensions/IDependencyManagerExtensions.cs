using System;
using Bit.Data.Contracts;
using Bit.Data.EntityFramework.Implementations;
using System.Data.Entity;

namespace Bit.Core.Contracts
{
    public static class IDependencyManagerExtensions
    {
        public static IDependencyManager RegisterEfDbContext<TDbContext>(this IDependencyManager dependencyManager)
            where TDbContext : DbContext
        {
            if (dependencyManager == null)
                throw new ArgumentNullException(nameof(dependencyManager));

            dependencyManager.Register<TDbContext, TDbContext>();
            dependencyManager.Register<IDataProviderSpecificMethodsProvider, EfDataProviderSpecificMethodsProvider>(overwriteExciting: false, lifeCycle: DependencyLifeCycle.SingleInstance);

            return dependencyManager;
        }
    }
}
