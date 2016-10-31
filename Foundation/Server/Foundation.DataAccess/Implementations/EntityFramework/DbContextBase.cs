using System;
using Foundation.DataAccess.Contracts;
using Microsoft.EntityFrameworkCore;
using Foundation.DataAccess.Contracts.EntityFramework;

namespace Foundation.DataAccess.Implementations.EntityFramework
{
    public class DbContextBase : DbContext
    {
        protected DbContextBase()
        {
        }

        protected DbContextBase(DbContextOptions options)
            : base(options)
        {
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
                UseTransactionForRelatinalDatabases(dbContextObjects);
        }

        private void UseTransactionForRelatinalDatabases(DbContextObjects dbContextObjects)
        {
            // Although Database in in EntityFramework.Core.dll, but it has a dependency on EntityFramework.Relational.dll
            // We're going to call this method, only in case of relation database usage.
            // So this property has been used in separated method call stack.
            Database.UseTransaction(dbContextObjects.Transaction);
        }
    }
}