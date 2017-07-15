using System.Data.Common;
using System.Data.Entity;
using Bit.Data.Contracts;

namespace Bit.Data.EntityFramework.Implementations
{
    public class DefaultDbContext : DbContext
    {
        protected DefaultDbContext()
        {
        }

        public DefaultDbContext(string nameOrConnectionString)
            : base(nameOrConnectionString)
        {
            ApplyDefaultConfig();
        }

        public DefaultDbContext(DbConnection existingConnection, bool contextOwnsConnection)
            : base(existingConnection, contextOwnsConnection)
        {
            ApplyDefaultConfig();
        }

        public DefaultDbContext(string connectionString, IDbConnectionProvider dbConnectionProvider)
            : base(dbConnectionProvider.GetDbConnection(connectionString, rollbackOnScopeStatusFailure: true), contextOwnsConnection: false)
        {
            ApplyDefaultConfig();
            Database.UseTransaction(dbConnectionProvider.GetDbTransaction(connectionString));
        }

        private void ApplyDefaultConfig()
        {
            Configuration.AutoDetectChangesEnabled = false;
            Configuration.EnsureTransactionsForFunctionsAndCommands = true;
            Configuration.LazyLoadingEnabled = false;
            Configuration.ProxyCreationEnabled = false;
            Configuration.UseDatabaseNullSemantics = false;
            Configuration.ValidateOnSaveEnabled = true;
        }
    }
}