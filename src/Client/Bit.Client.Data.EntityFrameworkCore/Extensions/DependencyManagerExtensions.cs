using Bit.Core.Contracts;
using Bit.Data.EntityFrameworkCore.Implementations;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace Autofac
{
    public static class DependencyManagerExtensions
    {
        public static IDependencyManager RegisterDbContext<TDbContext>(this IDependencyManager dependencyManager, Action<IServiceProvider, DbContextOptionsBuilder>? optionsAction = null)
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
