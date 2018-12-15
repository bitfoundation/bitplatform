using Bit.Data.Contracts;
using Bit.Data.EntityFramework.Implementations;
using Bit.Data.Implementations;
using System;
using System.Reflection;

namespace Bit.Core.Contracts
{
    public static class IDependencyManagerExtensions
    {
        /// <summary>
        /// Configures EntityFramework. It registers <typeparamref name="TDbContext"/>
        /// | <see cref="IDataProviderSpecificMethodsProvider"/> by <see cref="EfDataProviderSpecificMethodsProvider"/>
        /// | <see cref="EfDataProviderSpecificMethodsProvider"/> itself
        /// | <see cref="IUnitOfWork"/> by <see cref="DefaultUnitOfWork"/>
        /// </summary>
        /// <typeparam name="TDbContext">Any class inherited from <see cref="EfDbContextBase"/></typeparam>
        public static IDependencyManager RegisterEfDbContext<TDbContext>(this IDependencyManager dependencyManager)
            where TDbContext : EfDbContextBase
        {
            if (dependencyManager == null)
                throw new ArgumentNullException(nameof(dependencyManager));

            dependencyManager.Register(new[] { typeof(EfDbContextBase).GetTypeInfo(), typeof(TDbContext).GetTypeInfo() }, typeof(TDbContext).GetTypeInfo(), overwriteExciting: false);
            dependencyManager.Register<IDataProviderSpecificMethodsProvider, EfDataProviderSpecificMethodsProvider>(lifeCycle: DependencyLifeCycle.SingleInstance, overwriteExciting: false);
            dependencyManager.Register<EfDataProviderSpecificMethodsProvider, EfDataProviderSpecificMethodsProvider>(lifeCycle: DependencyLifeCycle.SingleInstance, overwriteExciting: false);
            dependencyManager.Register<IUnitOfWork, DefaultUnitOfWork>(overwriteExciting: false);
            dependencyManager.RegisterMapperConfiguration<EntityFrameworkMapperConfiguration>();

            return dependencyManager;
        }
    }
}
