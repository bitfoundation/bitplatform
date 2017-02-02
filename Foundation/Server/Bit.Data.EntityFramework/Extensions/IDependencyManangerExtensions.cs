using Bit.Data.EntityFramework.Implementations;
using Foundation.DataAccess.Contracts;
using System;

namespace Foundation.Core.Contracts
{
    public static class IDependencyManangerExtensions
    {
        public static IDependencyManager RegisterEfDbContext<TDbContext, TDbContextObjectsProvider>(this IDependencyManager dependencyManager)
            where TDbContext : class
            where TDbContextObjectsProvider : class
        {
            if (dependencyManager == null)
                throw new ArgumentNullException(nameof(dependencyManager));

            dependencyManager.Register<TDbContext, TDbContext>();
            dependencyManager.Register<IAsyncQueryableExecuter, EfAsyncQueryableExecuter>(overwriteExciting: false, lifeCycle: DependencyLifeCycle.SingleInstance);

            return dependencyManager;
        }
    }
}
