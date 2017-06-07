using System.Data.Common;
using System.Data.Entity;
using Bit.Data.Contracts;

namespace Bit.Data.EntityFramework.Implementations
{
    public class DefaultDbContext : DbContext
    {
        private readonly IDbConnectionProvider _dbConnectionProvider;

        protected DefaultDbContext()
        {
        }

        public DefaultDbContext(DbConnection existingConnection, bool contextOwnsConnection)
            : base(existingConnection, contextOwnsConnection)
        {
            
        }

        public DefaultDbContext(string connectionString, IDbConnectionProvider dbConnectionProvider)
            : base(dbConnectionProvider.GetDbConnection(connectionString, rollbackOnScopeStatusFailure: true), contextOwnsConnection: false)
        {
            _dbConnectionProvider = dbConnectionProvider;
            Database.UseTransaction(_dbConnectionProvider.GetDbTransaction(connectionString));
            Configuration.AutoDetectChangesEnabled = false;
            Configuration.EnsureTransactionsForFunctionsAndCommands = true;
            Configuration.LazyLoadingEnabled = false;
            Configuration.ProxyCreationEnabled = false;
            Configuration.UseDatabaseNullSemantics = false;
            Configuration.ValidateOnSaveEnabled = true;
        }
    }
}