using Bit.Data.Contracts;
using Bit.Data.EntityFrameworkCore.Implementations;
using Bit.Data.Implementations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Data.Common;
using System.Linq;
using System.Reflection;

namespace Bit.Core.Contracts
{
    public static class IDependencyManagerExtensions
    {
        public static IDependencyManager RegisterEfCoreDbContext<TDbContext>(this IDependencyManager dependencyManager, Action<IServiceProvider, DbContextOptionsBuilder> optionsAction)
            where TDbContext : DbContext
        {
            if (dependencyManager == null)
                throw new ArgumentNullException(nameof(dependencyManager));

            if (optionsAction == null)
                throw new ArgumentNullException(nameof(optionsAction));

            if (typeof(TDbContext).GetConstructors().SelectMany(ctor => ctor.GetParameters().Select(p => p.ParameterType)).Any(t => t == typeof(DbContextOptions)))
                throw new InvalidOperationException($"Use DbContextOptions<{typeof(TDbContext).Name}> instead of DbContextOptions in constructor.");

            IServiceCollection services = ((IServiceCollectionAccessor)dependencyManager).ServiceCollection;

            services.AddDbContext<TDbContext>((sp, optionsBuilder) =>
            {
                optionsAction.Invoke(sp, optionsBuilder);

                if (!optionsBuilder.IsConfigured)
                    throw new InvalidOperationException("optionsBuilder is not configured.");

                optionsBuilder.ConfigureWarnings(warnings => warnings.Throw(RelationalEventId.QueryClientEvaluationWarning));
            });

            services.Remove(services.ExtendedSingle($"Finding {typeof(TDbContext).Name}'s service descriptor", s => s.ServiceType == typeof(TDbContext)));

            dependencyManager.RegisterUsing(c =>
            {
                DbContextOptions options = c.Resolve<DbContextOptions<TDbContext>>();

                DbConnection dbConnectionFromDbContextOptionsBuilder = null;

                foreach (RelationalOptionsExtension ext in options.Extensions.OfType<RelationalOptionsExtension>())
                {
                    dbConnectionFromDbContextOptionsBuilder = dbConnectionFromDbContextOptionsBuilder ?? ext.Connection;
                }

                TDbContext dbContext = (TDbContext)ActivatorUtilities.CreateInstance(c, typeof(TDbContext).GetTypeInfo());

                if (dbConnectionFromDbContextOptionsBuilder != null) // It's not a relation db, for example UseInMemoryDatabase
                {
                    DbTransaction transaction = c.Resolve<IDbConnectionProvider>().GetDbTransaction(dbConnectionFromDbContextOptionsBuilder);

                    if (transaction != null) // When DbConnectionProvider has failed to open a connection to db due connection open timeout or any other reasons such as database does not exist, it won't provide any transaction too.
                    {
                        if (transaction.Connection != dbConnectionFromDbContextOptionsBuilder) // You're using a db connection other than db connection created by bit.
                            throw new InvalidOperationException("You've to configure optionsBuilder by calling (serviceProvider, optionsBuilder) => { optionsBuilder.UseSqlServer /* or UseSqlite, UseInMemoryDatabase, ... */ (serviceProvider.GetService<IDbConnectionProvider>().GetDbConnection(\"YOUR_CONNECTION_STRING_FROM_CONFIG_FOR_EXAMPLE\", rollbackOnScopeStatusFailure: true)) }.");
                        if (dbContext.Database.CurrentTransaction != null) // You're using a db transaction other than one provided by bit.
                            throw new InvalidOperationException("DbContext.Database.CurrentTransaction is not null");
                        dbContext.Database.UseTransaction(transaction);
                    }
                }

                return dbContext;
            }, servicesType: new[] { typeof(EfCoreDbContextBase).GetTypeInfo(), typeof(TDbContext).GetTypeInfo() }, overwriteExciting: false);

            dependencyManager.Register<IDataProviderSpecificMethodsProvider, EfCoreDataProviderSpecificMethodsProvider>(lifeCycle: DependencyLifeCycle.SingleInstance, overwriteExciting: false);
            dependencyManager.Register<EfCoreDataProviderSpecificMethodsProvider, EfCoreDataProviderSpecificMethodsProvider>(lifeCycle: DependencyLifeCycle.SingleInstance, overwriteExciting: false);
            dependencyManager.Register<IUnitOfWork, DefaultUnitOfWork>(overwriteExciting: false);

            return dependencyManager;
        }
    }
}
