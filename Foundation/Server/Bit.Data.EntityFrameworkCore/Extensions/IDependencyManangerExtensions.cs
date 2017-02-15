using Foundation.DataAccess.Contracts;
using Foundation.DataAccess.Contracts.EntityFrameworkCore;
using Foundation.DataAccess.Implementations.EntityFrameworkCore;
using System;
using System.Reflection;

namespace Foundation.Core.Contracts
{
    public static class IDependencyManangerExtensions
    {
        public static IDependencyManager RegisterEfCoreDbContext<TDbContext, TDbContextObjectsProvider>(this IDependencyManager dependencyManager)
            where TDbContext : class
            where TDbContextObjectsProvider : class
        {
            if (dependencyManager == null)
                throw new ArgumentNullException(nameof(dependencyManager));

            dependencyManager.Register(typeof(IDbContextObjectsProvider).GetTypeInfo(), typeof(TDbContextObjectsProvider).GetTypeInfo());
            dependencyManager.Register<TDbContext, TDbContext>();
            dependencyManager.Register<IDataProviderSpecificMethodsProvider, EfCoreDataProviderSpecificMethodsProvider>(overwriteExciting: false, lifeCycle: DependencyLifeCycle.SingleInstance);
            dependencyManager.RegisterGeneric(typeof(IWhereByKeyBuilder<,>).GetTypeInfo(), typeof(EfCoreWhereByKeyBuilder<,>).GetTypeInfo(), lifeCycle: DependencyLifeCycle.SingleInstance);

            return dependencyManager;
        }
    }
}
