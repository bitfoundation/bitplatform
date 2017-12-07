using System;
using Bit.Data.Contracts;
using Bit.Data.EntityFrameworkCore.Contracts;
using Bit.Data.EntityFrameworkCore.Implementations;
using Bit.Data.Implementations;
using System.Reflection;

namespace Bit.Core.Contracts
{
    public static class IDependencyManagerExtensions
    {
        public static IDependencyManager RegisterEfCoreDbContext<TDbContext, TDbContextObjectsProvider>(this IDependencyManager dependencyManager)
            where TDbContext : EfCoreDbContextBase
            where TDbContextObjectsProvider : class, IDbContextObjectsProvider
        {
            if (dependencyManager == null)
                throw new ArgumentNullException(nameof(dependencyManager));

            dependencyManager.Register<IDbContextObjectsProvider, TDbContextObjectsProvider>(overwriteExciting: false);
            dependencyManager.Register(new[] { typeof(EfCoreDbContextBase).GetTypeInfo(), typeof(TDbContext).GetTypeInfo() }, typeof(TDbContext).GetTypeInfo(), overwriteExciting: false);
            dependencyManager.Register<IDataProviderSpecificMethodsProvider, EfCoreDataProviderSpecificMethodsProvider>(lifeCycle: DependencyLifeCycle.SingleInstance, overwriteExciting: false);
            dependencyManager.Register<EfCoreDataProviderSpecificMethodsProvider, EfCoreDataProviderSpecificMethodsProvider>(lifeCycle: DependencyLifeCycle.SingleInstance, overwriteExciting: false);
            dependencyManager.Register<IUnitOfWork, DefaultUnitOfWork>(overwriteExciting: false);

            return dependencyManager;
        }
    }
}
