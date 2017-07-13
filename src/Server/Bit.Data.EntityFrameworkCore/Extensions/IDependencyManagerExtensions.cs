using System;
using System.Reflection;
using Bit.Data.Contracts;
using Bit.Data.EntityFrameworkCore.Contracts;
using Bit.Data.EntityFrameworkCore.Implementations;
using Microsoft.EntityFrameworkCore;

namespace Bit.Core.Contracts
{
    public static class IDependencyManagerExtensions
    {
        public static IDependencyManager RegisterEfCoreDbContext<TDbContext, TDbContextObjectsProvider>(this IDependencyManager dependencyManager)
            where TDbContext : DbContext
            where TDbContextObjectsProvider : class, IDbContextObjectsProvider
        {
            if (dependencyManager == null)
                throw new ArgumentNullException(nameof(dependencyManager));

            dependencyManager.Register<IDbContextObjectsProvider, TDbContextObjectsProvider>(overwriteExciting: false);
            dependencyManager.Register<TDbContext, TDbContext>(overwriteExciting: false);
            dependencyManager.Register<IDataProviderSpecificMethodsProvider, EfCoreDataProviderSpecificMethodsProvider>(lifeCycle: DependencyLifeCycle.SingleInstance, overwriteExciting: false);
            dependencyManager.RegisterGeneric(typeof(IWhereByKeyBuilder<,>).GetTypeInfo(), typeof(EfCoreWhereByKeyBuilder<,>).GetTypeInfo(), lifeCycle: DependencyLifeCycle.SingleInstance);

            return dependencyManager;
        }
    }
}
