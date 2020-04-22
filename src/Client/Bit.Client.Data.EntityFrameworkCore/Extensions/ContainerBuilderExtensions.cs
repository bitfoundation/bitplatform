using Bit.Core.Contracts;
using Bit.Data;
using Bit.Data.EntityFrameworkCore.Implementations;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace Autofac
{
    public static class ContainerBuilderExtensions
    {
        public static IDependencyManager RegisterDbContext<TDbContext>(this IDependencyManager dependencyManager, Action<DbContextOptionsBuilder>? optionsAction = null)
            where TDbContext : EfCoreDbContextBase
        {
            if (dependencyManager == null)
                throw new ArgumentNullException(nameof(dependencyManager));

            IServiceCollection services = ((IServiceCollectionAccessor)dependencyManager).ServiceCollection;

            services.AddEntityFrameworkSqlite();
            services.AddDbContext<TDbContext>(optionsAction, contextLifetime: ServiceLifetime.Transient, optionsLifetime: ServiceLifetime.Transient);

            dependencyManager.RegisterUsing(resolver => (EfCoreDbContextBase)resolver.Resolve<TDbContext>(), overwriteExisting: false, lifeCycle: DependencyLifeCycle.Transient);

            return dependencyManager;
        }
    }
}
