using System;
using Bit.Data.EntityFrameworkCore.Contracts;
using Microsoft.EntityFrameworkCore;

namespace Bit.Data.EntityFrameworkCore.Implementations
{
    public class DbContextBase : DbContext
    {
        protected DbContextBase()
        {
        }

        protected DbContextBase(DbContextOptions options)
            : base(options)
        {
            ApplyDefaultConfig();
        }

        protected DbContextBase(string connectionString, IDbContextObjectsProvider dbContextCreationOptionsProvider)
            : this(dbContextCreationOptionsProvider.GetDbContextOptions(connectionString).Options)
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

        private void ApplyDefaultConfig()
        {
            ChangeTracker.AutoDetectChangesEnabled = false;
            ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
        }

        private void UseTransactionForRelationalDatabases(DbContextObjects dbContextObjects)
        {
            // Although Database is in EntityFramework.Core.dll, but it has a dependency on EntityFramework.Relational.dll
            // We're going to call this method, only in case of relation database usage.
            // So this property has been used in separated method call stack.
            Database.UseTransaction(dbContextObjects.Transaction);
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);

            base.OnConfiguring(optionsBuilder);
        }
    }
}