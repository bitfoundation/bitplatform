using Bit.Data.EntityFrameworkCore.Contracts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using System;

namespace Bit.Data.EntityFrameworkCore.Implementations
{
    public class EfCoreDbContextBase : DbContext
    {
        protected EfCoreDbContextBase()
        {
        }

        public EfCoreDbContextBase(DbContextOptions options)
            : base(options)
        {
            ApplyDefaultConfig();
        }

        public EfCoreDbContextBase(string connectionString, IDbContextObjectsProvider dbContextCreationOptionsProvider)
            : base(dbContextCreationOptionsProvider.GetDbContextOptions(connectionString).Options)
        {
            if (dbContextCreationOptionsProvider == null)
                throw new ArgumentNullException(nameof(dbContextCreationOptionsProvider));

            if (connectionString == null)
                throw new ArgumentNullException(nameof(connectionString));

            DbContextObjects dbContextObjects = dbContextCreationOptionsProvider.GetDbContextOptions(connectionString);

            if (dbContextObjects.Transaction != null /* We're going to use a relational database */)
                UseTransactionForRelationalDatabases(dbContextObjects);

            ApplyDefaultConfig();
        }

        protected virtual void ApplyDefaultConfig()
        {
            ChangeTracker.AutoDetectChangesEnabled = false;
        }

        protected virtual void UseTransactionForRelationalDatabases(DbContextObjects dbContextObjects)
        {
            // Although Database is in EntityFramework.Core.dll, but it has a dependency on EntityFramework.Relational.dll
            // We're going to call this method, only in case of relation database usage.
            // So this property has been used in separated method call stack.
            Database.UseTransaction(dbContextObjects.Transaction);
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking)
                .ConfigureWarnings(warnings => warnings.Throw(RelationalEventId.QueryClientEvaluationWarning));

            base.OnConfiguring(optionsBuilder);
        }
    }
}
